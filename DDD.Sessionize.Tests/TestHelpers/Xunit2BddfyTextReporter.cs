using System.Collections.Generic;
using System.Threading;
using TestStack.BDDfy;
using Xunit.Abstractions;

namespace DDD.Sessionize.Tests.TestHelpers
{
    public class Xunit2BddfyTextReporter : ThreadsafeBddfyTextReporter
    {
        public static readonly Xunit2BddfyTextReporter Instance = new Xunit2BddfyTextReporter();
        private static readonly AsyncLocal<ITestOutputHelper> Output = new AsyncLocal<ITestOutputHelper>();
        private static readonly List<object> Processed = new List<object>();

        private Xunit2BddfyTextReporter() { }

        public void RegisterOutput(ITestOutputHelper output)
        {
            Output.Value = output;
        }

        public override void Process(Story story)
        {
            // For some reason this gets called multiple times when tests are run in parallel
            if (Processed.Contains(story))
                return;

            Processed.Add(story);
            base.Process(story);
            if (Output.Value != null)
                Output.Value.WriteLine(_text.Value.ToString());
        }
    }
}
