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
                .InFolder(GetType().Namespace.Replace("DDD.Sessionize.", "").Replace(".", "\\"))
                .WithFileExtension($".{extension}")
                .WithDescriminator($"_{testMethod}")
                //.WithScrubber(ReplaceGeneratedIds)
            );
        }

        /*string ReplaceGeneratedIds(string received)
        {
            return Regex.Replace(received, "[{(\"]?[0-9A-F]{8}[-]?([0-9A-F]{4}[-]?){3}[0-9A-F]{12}[\")}]?", g => $"\"{Guid.Empty:D}\"", RegexOptions.IgnoreCase);
        }*/

        [Fact]
        public void Test()
        {
            this.BDDfy(GetType().Name);
        }
    }
}
