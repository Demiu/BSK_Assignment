using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using Lib.Defines;
using Lib.Messages;

namespace Lib.Crypto;

public class SecurityAgent {
    // Secure flow for requesting side
    // 
    // ┌─────────┐ send SecureRequest     ┌─────────┐  recv SecureAccept
    // │Insercure├────────────────────────►Requested├───────────────────────┐
    // └─▲──▲────┘                        └────┬────┘                       │
    //   │  │      recv SecureReject           │                            │
    //   │  └──────────────────────────────────┘                         ┌──▼──┐
    //   │                                                               │     │
    //   │ (whitelist ON AND NOT key in whitelist) OR invalid signature  └─┬─┬─┘
    //   │                          send SecureReject                      │ │
    //   └─────────────────────────────────────────────────────────────────┘ │
    //                                                                       │
    //             whitelist OFF OR (whitelist ON AND key in whitelist)      │
    // ┌───────┐                   send SecureFinalize                       │
    // │Secured◄─────────────────────────────────────────────────────────────┘
    // └───────┘
    // Secure flow for requested side
    // 
    // ┌─────────┐              recv SecureRequest                       ┌─────┐
    // │Insercure├───────────────────────────────────────────────────────►     │
    // └──▲───▲──┘                                                       └─┬─┬─┘
    //    │   │                       whitelist ON AND NOT key in whitelist│ │
    //    │   │                                           send SecureReject│ │
    //    │   └────────────────────────────────────────────────────────────┘ │
    //    │                                                                  │
    //    │              whitelist OFF OR (whitelist ON AND key in whitelist)│
    //    │                                                 send SecureAccept│
    //    │                                                                  │
    //    │      recv SecureReject      ┌────────────┐                       │
    //    └─────────────────────────────┤SelfAccepted◄───────────────────────┘
    //                                  └─────┬──────┘
    // ┌───────┐    recv SecureFinalize       │
    // │Secured◄──────────────────────────────┘
    // └───────┘
    public enum State {
        Insecure, // Starting state
        Requested,
        SelfAccepted,
        Secured,
    }

    public volatile EncryptionMode preferredMode;
    volatile State state; // Write via mutex
    AsymmetricContainer keyStore;
    volatile byte[]? aesKey; // Write via mutex
    SemaphoreSlim mutex;
    volatile bool whitelistEnabled;

    public SecurityAgent(AsymmetricContainer keyStore) {
        this.state = State.Insecure;
        this.preferredMode = EncryptionMode.CBC;
        this.keyStore = keyStore;
        this.aesKey = null;
        this.mutex = new SemaphoreSlim(1);
        this.whitelistEnabled = false;
    }

    public bool IsSecured => state == State.Secured;

    public bool CanStartSecuring() => state == State.Insecure;
    public bool CanFinishSecuring() => state == State.Requested;
    public bool CanAcceptSecuring() => state == State.Insecure;
    public bool CanFinalizeSecuring() => state == State.SelfAccepted;
    public byte[] GetPubRsaKey() => keyStore.GetOwnPubKey();

    // TODO rework start/finish/accept securing with returning a message maybe?

    // To be used on requesting side
    // Returns a Message to send if securing has started
    public async Task<Message?> StartSecuring(CancellationToken cancellationToken) {
        return await RunLocked(() => StartSecuringInternal(), cancellationToken);
    }
    // Returns a Message to send in response (either a SecureFinalize or SecureReject)
    public async Task<Message> FinishSecuring(SecureAccept msg, CancellationToken cancellationToken) {
        return await RunLocked(() => FinishSecuringInternal(msg), cancellationToken);    
    }

    // To be used on requested side
    // Returns a Message to send in response (either a SecureAccept or SecureReject)
    public async Task<Message> AcceptSecuring(SecureRequest msg, CancellationToken cancellationToken) {
        return await RunLocked(() => AcceptSecuringInternal(msg), cancellationToken);
    }
    public async Task<bool> FinalizeSecuring(SecureFinalize msg, CancellationToken cancellationToken) {
        return await RunLocked(() => FinalizeSecuringInternal(msg), cancellationToken);
    }

    public async Task CancelSecuring(CancellationToken cancellationToken) {
        await RunLocked(() => { this.state = State.Insecure; }, cancellationToken);
    }

    // Requires Secured state
    public byte[]? GetAesKey() {
        return aesKey;
    }

    public Message? TrySecureMessage(Message toSecure) {
        if (state != State.Secured) {
            return null;
        }
        return preferredMode switch {
            EncryptionMode.CBC => new SecuredMessageCBC(toSecure, aesKey!),
            EncryptionMode.ECB => new SecuredMessageECB(toSecure, aesKey!),
            _ => throw new UnexpectedEnumValueException<EncryptionMode,byte>((byte)preferredMode),
        };
    }

    // Requires lock
    protected Message? StartSecuringInternal() {
        if (CanStartSecuring()) {
            state = State.Requested;
            return new SecureRequest(GetPubRsaKey());
        }
        return null;
    }

    // Requires lock
    protected Message FinishSecuringInternal(SecureAccept msg) {
        if (!CanFinishSecuring()) {
            return SecureReject.WrongState;
        }
        if (!msg.CheckSignature()) {
            return SecureReject.InvalidSignature;
        }

        aesKey = keyStore.Decrypt(msg.encryptedKey);
        state = State.Secured;
        return new SecureFinalize();
    }

    // Requires lock
    protected Message AcceptSecuringInternal(SecureRequest msg) {
        if (!CanAcceptSecuring()) {
            return SecureReject.WrongState;
        }
        if (whitelistEnabled && !keyStore.PubKeyKnown(msg.publicKey)) {
            return SecureReject.NotInWhitelist;
        }

        var otherRsa = RSA.Create(Defines.Constants.RSA_KEY_SIZE);
        otherRsa.ImportRSAPublicKey(msg.publicKey, out var len);
        Debug.Assert(len == msg.publicKey.Length);
        
        using (var aes = Aes.Create()) {
            aesKey = aes.Key;
        }

        state = State.SelfAccepted;
        return new SecureAccept(msg.publicKey, keyStore.OwnPair, aesKey);
    }

    // Requires lock
    protected bool FinalizeSecuringInternal(SecureFinalize msg) {
        if (!CanFinalizeSecuring()) {
            return false;
        }
        state = State.Secured;
        return true;
    }

    protected async Task RunLocked(Action toRun, CancellationToken cancellationToken) {
        await mutex.WaitAsync(cancellationToken);
        try {
            toRun();
        } finally {
            mutex.Release();
        }
    }

    protected async Task<T> RunLocked<T>(Func<T> toRun, CancellationToken cancellationToken) {
        await mutex.WaitAsync(cancellationToken);
        try {
            return toRun();
        } finally {
            mutex.Release();
        }
    }
}