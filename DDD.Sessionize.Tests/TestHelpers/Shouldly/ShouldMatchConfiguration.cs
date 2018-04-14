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
    public class ShouldMatchConfigurationBuilder
    {
        public ShouldMatchConfigurationBuilder(ShouldMatchConfiguration initialConfig)
        {
            Config = new ShouldMatchConfiguration(initialConfig);
        }

        public ShouldMatchConfigurationBuilder WithStringCompareOptions(StringCompareShould stringCompareOptions)
        {
            return Configure(c => c.StringCompareOptions = stringCompareOptions);
        }

        public ShouldMatchConfigurationBuilder WithName(string name)
        {
            return Configure(c => c.Name = name);
        }

        public ShouldMatchConfigurationBuilder WithDescriminator(string fileDescriminator)
        {
            return Configure(c => c.FilenameDescriminator = fileDescriminator);
        }

        public ShouldMatchConfigurationBuilder WithFileExtension(string fileExtension)
        {
            return Configure(c => c.FileExtension = fileExtension.TrimStart('.'));
        }

        /// <summary>
        ///     Default is to ignore line endings
        /// </summary>
        public ShouldMatchConfigurationBuilder DoNotIgnoreLineEndings()
        {
            return Configure(c =>
            {
                if ((c.StringCompareOptions & StringCompareShould.IgnoreLineEndings) ==
                    StringCompareShould.IgnoreLineEndings)
                    c.StringCompareOptions &= ~StringCompareShould.IgnoreLineEndings;
            });
        }

        /// <summary>
        ///     Places the .approved and .received files into a subfolder
        /// </summary>
        public ShouldMatchConfigurationBuilder InFolder(params string[] parts)
        {
            var srcFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            var folder = Path.Combine(srcFolder, string.Join(Path.DirectorySeparatorChar.ToString(), parts));

            return Configure(c => c.ApprovalFileFolder = folder);
        }

        public ShouldMatchConfigurationBuilder WithScrubber(Func<string, string> scrubber)
        {
            return Configure(c =>
            {
                if (c.Scrubber == null)
                    c.Scrubber = scrubber;
                else
                {
                    var existing = c.Scrubber;
                    c.Scrubber = s => existing(scrubber(s));
                }
            });
        }

        public ShouldMatchConfigurationBuilder Configure(Action<ShouldMatchConfiguration> configure)
        {
            configure(Config);
            return this;
        }

        public ShouldMatchConfiguration Build()
        {
            return Config;
        }

        public readonly ShouldMatchConfiguration Config;
    }

    public class ShouldMatchConfiguration
    {
        public ShouldMatchConfiguration()
        {
        }

        public ShouldMatchConfiguration(ShouldMatchConfiguration initialConfig)
        {
            StringCompareOptions = initialConfig.StringCompareOptions;
            FilenameDescriminator = initialConfig.FilenameDescriminator;
            FileExtension = initialConfig.FileExtension;
            ApprovalFileFolder = initialConfig.ApprovalFileFolder;
            Scrubber = initialConfig.Scrubber;
        }

        public StringCompareShould StringCompareOptions { get; set; }
        public string FilenameDescriminator { get; set; }

        /// <summary>
        ///     File extension without the .
        /// </summary>
        public string FileExtension { get; set; }

        public string ApprovalFileFolder { get; set; }

        public string Name { get; set; }

        /// <summary>
        ///     Scrubbers allow you to alter the received document before comparing it to approved.
        ///     This is useful for replacing dates or dynamic data with fixed data
        /// </summary>
        public Func<string, string> Scrubber { get; set; }

        public string OutputFolder { get; set; }
    }
}
