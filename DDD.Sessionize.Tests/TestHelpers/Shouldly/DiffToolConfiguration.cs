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
using System.Collections.Generic;
using System.Linq;

namespace Shouldly.Core
{
    public class DiffToolConfiguration
    {
        public DiffToolConfiguration()
        {
            _diffTools = typeof(KnownDiffTools).GetFields().Select(f => (DiffTool)f.GetValue(KnownDiffTools)).ToList();
            _knownShouldNotLaunchDiffToolReasons = typeof(KnownDoNotLaunchStrategies).GetFields()
                .Select(f => (IShouldNotLaunchDiffTool)f.GetValue(KnownDoNotLaunchStrategies)).ToList();
        }

        public KnownDiffTools KnownDiffTools { get; } = KnownDiffTools.Instance;
        public KnownDoNotLaunchStrategies KnownDoNotLaunchStrategies { get; } = new KnownDoNotLaunchStrategies();

        public void RegisterDiffTool(DiffTool diffTool)
        {
            _diffTools.Add(diffTool);
        }

        public void SetDiffToolPriorities(params DiffTool[] diffTools)
        {
            var notRegistered = diffTools.Except(_diffTools).ToArray();
            if (notRegistered.Any())
            {
                var notRegisteredNames = string.Join(", ", notRegistered.Select(r => r.Name).ToArray());
                throw new InvalidOperationException($"The following diff tools are not registed: {notRegisteredNames}");
            }

            _diffToolPriority.Clear();
            _diffToolPriority.AddRange(diffTools);
        }

        public void AddDoNotLaunchStrategy(IShouldNotLaunchDiffTool shouldNotlaunchStrategy)
        {
            _knownShouldNotLaunchDiffToolReasons.Add(shouldNotlaunchStrategy);
        }

        public bool ShouldOpenDiffTool()
        {
            return !_knownShouldNotLaunchDiffToolReasons.Any(r => r.ShouldNotLaunch());
        }

        public DiffTool GetDiffTool()
        {
            var diffTool = _diffToolPriority.FirstOrDefault(d => d.Exists()) ??
                _diffTools.FirstOrDefault(d => d.Exists());

            if (diffTool == null)
                throw new ShouldAssertException(@"Cannot find a difftool to use, 'ShouldlyConfiguration.DiffTools.RegisterDiffTool()' to add your own");

            return diffTool;
        }

        private readonly List<DiffTool> _diffToolPriority = new List<DiffTool>();
        private readonly List<DiffTool> _diffTools;
        private readonly List<IShouldNotLaunchDiffTool> _knownShouldNotLaunchDiffToolReasons;
    }
}
