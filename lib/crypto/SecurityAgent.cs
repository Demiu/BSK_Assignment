using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using Lib.Messages;

namespace Lib.Crypto;

public class SecurityAgent {
    public enum State {
        Insecure,
        SelfInitialized,
        BothInitialized,
        Secured,
    }

    State state; // Write via mutex
    RSA? rsa; // Access via mutex
    byte[]? aesKey; // Write via mutex
    SemaphoreSlim mutex;

    public SecurityAgent() {
        state = State.Insecure;
        mutex = new SemaphoreSlim(1);
        rsa = null;
        aesKey = null;
    }

    public bool CanStartSecuring() => state == State.Insecure;
    public bool CanFinishSecuring() => state == State.SelfInitialized;
    public bool CanAcceptSecuring() => state == State.Insecure;

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

    // Requires SelfInitialized state
    public byte[] GetPubRsaKey() {
        // Will throw NullReferenceException if InitSelfRsa wasn't called
        return rsa.ExportRSAPublicKey();
    }

    // Requires Secured state
    public byte[]? GetAesKey() {
        return aesKey;
    }

    public SecuredMessage? TrySecureMessage(Message toSecure) {
        if (state != State.Secured) {
            return null;
        }
        return new SecuredMessage(toSecure, aesKey!);
    }

    // Requires lock
    protected bool InitSelfRsa() {
        if (CanStartSecuring()) {
            rsa = RSA.Create(Defines.Constants.RSA_KEY_SIZE);
            state = State.SelfInitialized;
            return true;
        }
        return false;
    }

    // Requires lock
    protected bool InitAesRsaEncrypted(byte[] encryptedAes) {
        if (CanFinishSecuring()) {
            aesKey = rsa.Decrypt(encryptedAes, Defines.Constants.RSA_PADDING_TYPE);
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

    protected async Task<T> RunLocked<T>(Func<T> toRun, CancellationToken cancellationToken) {
        await mutex.WaitAsync(cancellationToken);
        try {
            return toRun();
        } finally {
            mutex.Release();
        }
    }
}