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
    SemaphoreSlim mutex;

    public SecurityAgent() {
        state = State.Insecure;
        mutex = new SemaphoreSlim(1);
        rsa = null;
    }

    public async Task<bool> StartSecuring(CancellationToken cancellationToken) {
        return await RunLocked(() => InitSelf(), cancellationToken);
    }

    public async Task<byte[]> GetPubKey(CancellationToken cancellationToken) {
        // Will throw NullReferenceException if InitSelf wasn't called
        return await RunLocked(() => rsa?.ExportRSAPublicKey(), cancellationToken);
    }

    protected bool InitSelf() {
        if (state == State.Insecure) {
            rsa = RSA.Create(Defines.Constants.RSA_KEY_SIZE);
            state = State.SelfInitialized;
            return true;
        }
        return false;
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