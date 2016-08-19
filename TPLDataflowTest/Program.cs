using System.Threading.Tasks;
using ITCC.Logging.Core;
using ITCC.Logging.Windows.Loggers;

namespace TPLDataflowTest
{
    internal class Program
    {
        private static void Main(string[] args) => MainAsync().GetAwaiter().GetResult();

        private static async Task MainAsync()
        {
            InitLoggers();
            LogMessage(LogLevel.Info, "Started");

            await Task.Delay(1);
        }

        private static void InitLoggers()
        {
            Logger.Level = LogLevel.Trace;
            Logger.RegisterReceiver(new ColouredConsoleLogger());
        }

        private static void LogMessage(LogLevel level, string message) => Logger.LogEntry("TPL TEST", level, message);
    }
}
