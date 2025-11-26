namespace CreditCardsSystem.Web.Client.Components
{
    public class CancellableComponent : ApplicationComponent, IDisposable
    {
        protected CancellationTokenSource? CancellationTokenSource = new();
        public IDisposable? register;
        protected CancellationToken CancellationToken => (CancellationTokenSource ??= new()).Token;

        public void Dispose()
        {
            Notification.Clear();
            if (CancellationTokenSource is not null)
            {
                CancellationTokenSource.Cancel();
                CancellationTokenSource.Dispose();
                CancellationTokenSource = null;
                Console.WriteLine("Cancelling Token");
            }

            register?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
