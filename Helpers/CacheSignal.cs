using System.Threading;
using Microsoft.Extensions.Primitives;

namespace DashboardTeknikP1.Helpers
{
    public static class CacheSignal
    {
        private static CancellationTokenSource _cts = new CancellationTokenSource();

        public static CancellationTokenSource TokenSource => _cts;

        public static void Reset()
        {
            var oldCts = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
            try
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }
            catch
            {
                // Safe ignore
            }
        }
    }
}
