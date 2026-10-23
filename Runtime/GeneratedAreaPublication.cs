using System;
using System.Threading;
using System.Threading.Tasks;

namespace Celeste.Mods.Randomizer.Runtime
{
    internal sealed class GeneratedAreaPublication : IDisposable
    {
        private readonly TaskCompletionSource<object> completion =
            new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly CancellationToken token;
        private readonly CancellationTokenRegistration cancellation;
        private Action commit;
        private int state;

        public GeneratedAreaPublication(Action commit, CancellationToken token)
        {
            this.commit = commit ?? throw new ArgumentNullException(nameof(commit));
            this.token = token;
            cancellation = token.Register(Cancel);
        }

        private void Cancel()
        {
            if (Interlocked.CompareExchange(ref state, 2, 0) != 0) return;
            Interlocked.Exchange(ref commit, null);
            completion.TrySetCanceled(token);
        }

        public void Commit()
        {
            // Cancellation wins only before the main-thread commit is accepted.
            if (Interlocked.CompareExchange(ref state, 1, 0) != 0) return;
            var action = Interlocked.Exchange(ref commit, null);
            try
            {
                action();
                completion.TrySetResult(null);
            }
            catch (Exception error)
            {
                completion.TrySetException(error);
            }
        }

        public void Wait() => completion.Task.GetAwaiter().GetResult();
        public void Dispose() => cancellation.Dispose();
    }
}
