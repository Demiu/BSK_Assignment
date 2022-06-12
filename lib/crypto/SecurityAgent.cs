using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using Lib.Defines;
using Lib.Messages;

namespace Lib.Crypto;

public class SecurityAgent {
    public enum State {
        Insecure,
        SelfInitialized,
        BothInitialized,
        Secured,
    }
    public enum PreferredMode : byte {
        ECB,
        CBC
    }

    public PreferredMode preferredMode;
    State state; // Write via mutex
    AsymmetricContainer keyStore;
    byte[]? aesKey; // Write via mutex
    SemaphoreSlim mutex;

    public SecurityAgent(AsymmetricContainer keyStore) {
        this.state = State.Insecure;
        this.preferredMode = PreferredMode.CBC;
        this.keyStore = keyStore;
        this.aesKey = null;
        this.mutex = new SemaphoreSlim(1);
    }

    public bool IsSecured => state == State.Secured;

    public bool CanStartSecuring() => state == State.Insecure;
    public bool CanFinishSecuring() => state == State.SelfInitialized;
    public bool CanAcceptSecuring() => state == State.Insecure;
    public byte[] GetPubRsaKey() => keyStore.GetOwnPubKey();

    // TODO rework start/finish/accept securing with returning a message maybe?

    // To be used on requestor side
    public async Task<bool> StartSecuring(CancellationToken cancellationToken) {
        return await RunLocked(() => InitSelfRsa(), cancellationToken);
    }
    public async Task<bool> FinishSecuring(byte[] encryptedAesKey, CancellationToken cancellationToken) {
        return await RunLocked(() => InitAesRsaEncrypted(encryptedAesKey), cancellationToken);    
    }

    // To be used on requestee side
    public async Task<bool> AcceptSecuring(byte[] otherPubKey, CancellationToken cancellationToken) {
        return await RunLocked(() => InitAesWithRsaPubKey(otherPubKey), cancellationToken);
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
    protected bool InitSelfRsa() {
        if (CanStartSecuring()) {
            //rsa = RSA.Create(Defines.Constants.RSA_KEY_SIZE);
            state = State.SelfInitialized;
            return true;
        }
        return false;
    }

    // Requires lock
    protected bool InitAesRsaEncrypted(byte[] encryptedAes) {
        if (CanFinishSecuring()) {
            aesKey = keyStore.Decrypt(encryptedAes);
            state = State.Secured;
            return true;
        }
        return false;
    }

    // Requires lock
    protected bool InitAesWithRsaPubKey(byte[] otherPubKey) {
        if (!CanAcceptSecuring()) {
            return false;
        }

        var otherRsa = RSA.Create(Defines.Constants.RSA_KEY_SIZE);
        otherRsa.ImportRSAPublicKey(otherPubKey, out var len);
        Debug.Assert(len == otherPubKey.Length);
        
        using (var aes = Aes.Create()) {
            aesKey = aes.Key;
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