//  Redistribution and use in source and binary forms, with or without modification,
//  are permitted provided that the following conditions are met:
//
//    * Redistributions of source code must retain the above copyright notice,
//      this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above copyright notice,
//      this list of conditions and the following disclaimer in the documentation
//      software without specific prior written permission.
//      and/or other materials provided with the distribution.
//      contributors may be used to endorse or promote products derived from this
//    * Neither the names of the copyright holders nor the names of
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//  ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//  WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//  DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
//  FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
//  DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
//  SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
//  CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
//  OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
//  THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//  https://github.com/shouldly/shouldly/blob/master/LICENSE.txt

using System;
using System.IO;

namespace Shouldly.Core
{
    public static class ShouldMatchApprovedTestExtensions
    {
        public static void ShouldMatchApproved(this string actual, Action<ShouldMatchConfigurationBuilder> configureOptions)
        {
            actual.ShouldMatchApproved(() => null, configureOptions);
        }

        public static void ShouldMatchApproved(this string actual, string customMessage, Action<ShouldMatchConfigurationBuilder> configureOptions)
        {
            actual.ShouldMatchApproved(() => customMessage, configureOptions);
        }

        public static void ShouldMatchApproved(this string actual, Func<string> customMessage, Action<ShouldMatchConfigurationBuilder> configureOptions)
        {
            var configurationBuilder = new ShouldMatchConfigurationBuilder(new ShouldMatchConfiguration
            {
                StringCompareOptions = StringCompareShould.IgnoreLineEndings,
                FileExtension = "txt"
            });

            configureOptions(configurationBuilder);

            var config = configurationBuilder.Build();

            if (config.Scrubber != null)
                actual = config.Scrubber(actual);

            var approvalFileFolder = config.ApprovalFileFolder;
            if (!Directory.Exists(approvalFileFolder))
                Directory.CreateDirectory(approvalFileFolder);

            var approvedFile = Path.Combine(approvalFileFolder, $"{config.Name}{config.FilenameDescriminator}.approved.{config.FileExtension}");
            var receivedFile = Path.Combine(approvalFileFolder, $"{config.Name}{config.FilenameDescriminator}.received.{config.FileExtension}");

            File.WriteAllText(receivedFile, actual);
            if (!File.Exists(approvedFile))
            {
                if (DiffTools.ShouldOpenDiffTool())
                    DiffTools.GetDiffTool().Open(receivedFile, approvedFile, false);

                throw new ShouldMatchApprovedException($@"Approval file {approvedFile} does not exist", receivedFile, approvedFile);
            }

            try
            {
                var approvedFileContents = File.ReadAllText(approvedFile);
                var receivedFileContents = File.ReadAllText(receivedFile);

                receivedFileContents.ShouldBe(approvedFileContents, config.StringCompareOptions);

                if (File.Exists(receivedFile))
                    File.Delete(receivedFile);
            }
            catch (Exception)
            {
                if (DiffTools.ShouldOpenDiffTool())
                    DiffTools.GetDiffTool().Open(receivedFile, approvedFile, true);

                throw;
            }
        }

        public static DiffToolConfiguration DiffTools { get; } = new DiffToolConfiguration();
    }
}
