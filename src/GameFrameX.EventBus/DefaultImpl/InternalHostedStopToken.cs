using System;
using System.Threading;

namespace GameFrameX.EventBus.DefaultImpl
{
    internal class InternalHostedStopToken : IHostedStopToken, IDisposable
    {
        public InternalHostedStopToken()
        {
            StopCancellationTokenSource = new CancellationTokenSource();
        }

        private CancellationTokenSource StopCancellationTokenSource { get; }

        public void Dispose()
        {
            StopCancellationTokenSource.Dispose();
        }

        public CancellationToken StopCancellationToken
        {
            get { return StopCancellationTokenSource.Token; }
        }

        internal void Cancel()
        {
            StopCancellationTokenSource.Cancel();
        }
    }
}