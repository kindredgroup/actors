using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Actors.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance;
            BenchmarkRunner.Run<FireAndForgetBenchmark>(config, args);
            BenchmarkRunner.Run<AckDeliveryBenchmark>(config, args);
            BenchmarkRunner.Run<EchoBenchmark>(config, args);

            // Use this to select benchmarks from the console:
            // var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }
}