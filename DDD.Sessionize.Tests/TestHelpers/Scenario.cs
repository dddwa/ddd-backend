using System.Runtime.CompilerServices;
using Shouldly.Core;
using TestStack.BDDfy;
using TestStack.BDDfy.Configuration;
using Xunit;

namespace DDD.Sessionize.Tests.TestHelpers
{
    public abstract class Scenario
    {
        static Scenario()
        {
            Configurator.Processors.Add(() => Xunit2BddfyTextReporter.Instance);
        }

        protected void Approve(string textToApprove, string extension = "txt", [CallerMemberName] string testMethod = "")
        {
            textToApprove.ShouldMatchApproved(b => b
                .WithName(GetType().Name)
                .InFolder(GetType().Namespace.Replace("DDD.Sessionize.Tests.", "").Replace(".", "\\"))
                .WithFileExtension($".{extension}")
                .WithDescriminator($"_{testMethod}")
                .WithScrubber(_guidScrubber.Scrub)
            );
        }

        [Fact]
        public virtual void Run()
        {
            this.BDDfy(GetType().Name);
        }

        private readonly GuidScrubber _guidScrubber = new GuidScrubber();
    }
}
