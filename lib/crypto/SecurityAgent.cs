using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;

namespace Lib.Crypto;

public class SecurityAgent {
    enum State {
        Insecure,
        SelfInitialized,
        BothInitialized,
        Secured,
    }
    State state; // Access via mutex
    RSA? rsa; // Access via mutex
    Aes? aes; // Access via mutex
    SemaphoreSlim mutex;

    public SecurityAgent() {
        state = State.Insecure;
        mutex = new SemaphoreSlim(1);
        rsa = null;
        aes = null;
    }

    public async Task<bool> StartSecuring(CancellationToken cancellationToken) {
        return await RunLocked(() => InitSelf(), cancellationToken);
    }

    public async Task<byte[]> GetPubKey(CancellationToken cancellationToken) {
        // Will throw NullReferenceException if InitSelf wasn't called
        return await RunLocked(() => rsa?.ExportRSAPublicKey(), cancellationToken);
    }

    public async Task<(byte[], byte[])?> AcceptSecuring(byte[] otherPubKey, CancellationToken cancellationToken) {
        return await RunLocked(() => InitAesWithRsaPubKey(otherPubKey), cancellationToken);
    }

    protected bool InitSelf() {
        if (state == State.Insecure) {
            rsa = RSA.Create(Defines.Constants.RSA_KEY_SIZE);
            state = State.SelfInitialized;
            return true;
        }
        return false;
    }

    // Returns (aes.key, aes.iv), both encrypted with the public key
    protected (byte[], byte[])? InitAesWithRsaPubKey(byte[] otherPubKey) {
        if (state != State.Insecure) {
            return null;
        }

        var otherRsa = RSA.Create(Defines.Constants.RSA_KEY_SIZE);
        otherRsa.ImportRSAPublicKey(otherPubKey, out var len);
        Debug.Assert(len == otherPubKey.Length);
        
        aes = Aes.Create();
        var encKey = otherRsa.Encrypt(aes.Key, Defines.Constants.RSA_PADDING_TYPE);
        var encIv = otherRsa.Encrypt(aes.IV, Defines.Constants.RSA_PADDING_TYPE);

        state = State.Secured;
        return (encKey, encIv);
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