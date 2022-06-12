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
    //   │                whitelist ON AND NOT key in whitelist          └─┬─┬─┘
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
    public enum PreferredMode : byte {
        ECB,
        CBC
    }

    public PreferredMode preferredMode;
    volatile State state; // Write via mutex
    AsymmetricContainer keyStore;
    volatile byte[]? aesKey; // Write via mutex
    SemaphoreSlim mutex;
    volatile bool whitelistEnabled;

    public SecurityAgent(AsymmetricContainer keyStore) {
        this.state = State.Insecure;
        this.preferredMode = PreferredMode.CBC;
        this.keyStore = keyStore;
        this.aesKey = null;
        this.mutex = new SemaphoreSlim(1);
        this.whitelistEnabled = false;
    }

    public bool IsSecured => state == State.Secured;

    public bool CanStartSecuring() => state == State.Insecure;
    public bool CanFinishSecuring() => state == State.Requested;
    public bool CanAcceptSecuring() => state == State.Insecure;
    public byte[] GetPubRsaKey() => keyStore.GetOwnPubKey();

    // TODO rework start/finish/accept securing with returning a message maybe?

    // To be used on requesting side
    // Returns a Message to send if securing has started
    public async Task<Message?> StartSecuring(CancellationToken cancellationToken) {
        return await RunLocked(() => StartSecuringInternal(), cancellationToken);
    }
    public async Task<bool> FinishSecuring(byte[] encryptedAesKey, CancellationToken cancellationToken) {
        return await RunLocked(() => FinishSecuringInternal(encryptedAesKey), cancellationToken);    
    }

    // To be used on requested side
    // Returns a Message to send in response (either a SecureAccept or SecureReject)
    public async Task<Message> AcceptSecuring(byte[] otherPubKey, CancellationToken cancellationToken) {
        return await RunLocked(() => AcceptSecuringInternal(otherPubKey), cancellationToken);
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
            PreferredMode.CBC => new SecuredMessageCBC(toSecure, aesKey!),
            PreferredMode.ECB => new SecuredMessageECB(toSecure, aesKey!),
            _ => throw new UnexpectedEnumValueException<PreferredMode,byte>((byte)preferredMode),
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
    protected bool FinishSecuringInternal(byte[] encryptedAes) {
        if (CanFinishSecuring()) {
            aesKey = keyStore.Decrypt(encryptedAes);
            state = State.Secured;
            return true;
        }
        return false;
    }

    // Requires lock
    protected Message AcceptSecuringInternal(byte[] otherPubKey) {
        if (!CanAcceptSecuring()) {
            return SecureReject.WrongState;
        }
        if (whitelistEnabled && !keyStore.PubKeyKnown(otherPubKey)) {
            return SecureReject.NotInWhitelist;
        }

        var otherRsa = RSA.Create(Defines.Constants.RSA_KEY_SIZE);
        otherRsa.ImportRSAPublicKey(otherPubKey, out var len);
        Debug.Assert(len == otherPubKey.Length);
        
        using (var aes = Aes.Create()) {
            aesKey = aes.Key;
        }

        state = State.Secured;
        return new SecureAccept(otherPubKey, aesKey);
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