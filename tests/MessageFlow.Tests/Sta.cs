using System.Runtime.ExceptionServices;

namespace MessageFlow.Tests;

/// <summary>
/// WPF measuring and the view-model need a single-threaded apartment; xUnit runs tests on
/// pool threads, so each such test body runs on its own STA thread.
/// </summary>
internal static class Sta
{
    public static void Run(Action body)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    public static void Run(Func<Task> body)
    {
        Run(() =>
        {
            // Pump the dispatcher so awaited continuations resume on this STA thread.
            var frame = new System.Windows.Threading.DispatcherFrame();
            Exception? failure = null;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(async () =>
            {
                try
                {
                    await body();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    frame.Continue = false;
                }
            });
            System.Windows.Threading.Dispatcher.PushFrame(frame);
            if (failure is not null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        });
    }
}
