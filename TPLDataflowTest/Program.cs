using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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

            var doubleSourceBlock = new TransformManyBlock<double, double>(d =>
            {
                MakeCpuHurt(1000 * 1000 * 100);
                LogMessage(LogLevel.Debug, $"TransformMany {d}");
                return new List<double> {d, d * 10, d * 100, d * 1000, d * 10000, d * 100000};
            });

            var transformDoubleToIntBlock = new TransformBlock<double, int>(d =>
            {
                MakeCpuHurt(1000 * 1000 * 300);
                LogMessage(LogLevel.Debug, $"Transform {d}");
                return Convert.ToInt32(d);
            }, new ExecutionDataflowBlockOptions { EnsureOrdered = true, MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded });

            var computeAndPrintActionBlock = new ActionBlock<int>(number =>
            {
                MakeCpuHurt(1000* 1000 * 10);
                LogMessage(LogLevel.Debug, $"Action {number}");
            }, new ExecutionDataflowBlockOptions {EnsureOrdered = true, MaxDegreeOfParallelism = 1});

            doubleSourceBlock.LinkTo(transformDoubleToIntBlock);
            transformDoubleToIntBlock.LinkTo(computeAndPrintActionBlock);
            for (double j = 0; j < 10; ++j)
            {

                doubleSourceBlock.Post(j);
                
            }

            var firstCompletion = doubleSourceBlock.Completion.ContinueWith(t => transformDoubleToIntBlock.Complete());
            var secondCompletion = transformDoubleToIntBlock.Completion.ContinueWith(t => computeAndPrintActionBlock.Complete());
            var lastCompletion =
                computeAndPrintActionBlock.Completion.ContinueWith(t => LogMessage(LogLevel.Info, "Pipeline completed"));
            doubleSourceBlock.Complete();
            // computeAndPrintActionBlock.Complete();
            //await firstCompletion;
            //await secondCompletion;
            await lastCompletion;
        }

        private static void InitLoggers()
        {
            Logger.Level = LogLevel.Trace;
            Logger.RegisterReceiver(new ColouredConsoleLogger());
        }

        private static void MakeCpuHurt(int cycles)
        {
            var random = new Random();
            for (var i = 0; i < cycles; ++i)
            {
                if (random.Next() == 982374892)
                    break;
            };
        }

        private static void LogMessage(LogLevel level, string message) => Logger.LogEntry("TPL TEST", level, message);
    }
}
