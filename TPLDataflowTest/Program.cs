using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Timers;
using ITCC.Logging.Core;
using ITCC.Logging.Windows.Loggers;

namespace TPLDataflowTest
{
    internal class Program
    {
        private const string BaseUri = @"https://api.vk.com/method/users.get?user_ids=PLACEHOLDER&name_case=Nom&v=5.53&fields=sex,bdate,city,country";
        private static void Main(string[] args) => MainAsync().GetAwaiter().GetResult();

        private static async Task MainAsync()
        {
            InitLoggers();
            LogMessage(LogLevel.Info, "Started");

            var doubleSourceBlock = new TransformManyBlock<double, double>(d =>
            {
                MakeCpuHurt(1000 * 1000 * 100);
                LogMessage(LogLevel.Debug, $"TransformMany {d}");
                var result = new List<double>();
                for (var i = 0; i < 100; ++i)
                {
                    result.Add(d);
                }
                return result;
            });

            var transformDoubleToIntBlock = new TransformBlock<double, int>(async d =>
            {
                LogMessage(LogLevel.Trace, $"Request started {d}");
                using (var client = new HttpClient())
                {
                    try
                    {
                        for (var i = 0; i < 5; ++i)
                        {
                            await
                                client.GetAsync(BaseUri.Replace("PLACEHOLDER",
                                    new Random().Next(10000, 100000).ToString()));
                        }
                    }
                    catch (Exception ex)
                    {
                        LogException(LogLevel.Warning, ex);
                    }

                }
                LogMessage(LogLevel.Trace, $"Request ended {d}");
                return Convert.ToInt32(d);
            }, new ExecutionDataflowBlockOptions { EnsureOrdered = true, MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded });

            var computeAndPrintActionBlock = new ActionBlock<int>(number =>
            {
                MakeCpuHurt(1000* 1000 * 2);
                LogMessage(LogLevel.Debug, $"Action {number}");
            }, new ExecutionDataflowBlockOptions {EnsureOrdered = true, MaxDegreeOfParallelism = 1});

            var timer = new Timer(2000);
            timer.Elapsed += (sender, args) =>
            {
                var firstQueueStr = $"\n{doubleSourceBlock.InputCount} {doubleSourceBlock.OutputCount}";
                var secondQueueStr = $"\n{transformDoubleToIntBlock.InputCount} {transformDoubleToIntBlock.OutputCount}";
                var thirdQueueStr = $"\n{computeAndPrintActionBlock.InputCount}";
                LogMessage(LogLevel.Info, $"QUEUES: {firstQueueStr}{secondQueueStr}{thirdQueueStr}");
            };
            timer.Start();

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
            timer.Stop();
        }

        private static void InitLoggers()
        {
            Logger.Level = LogLevel.Debug;
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
        private static void LogException(LogLevel level, Exception exception) => Logger.LogException("TPL TEST", level, exception);
    }
}
