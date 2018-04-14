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
    public interface IShouldNotLaunchDiffTool
    {
        bool ShouldNotLaunch();
    }

    public class DoNotLaunchWhenEnvVariableIsPresent : IShouldNotLaunchDiffTool
    {
        readonly string _environmentalVariable;

        public DoNotLaunchWhenEnvVariableIsPresent(string environmentalVariable)
        {
            _environmentalVariable = environmentalVariable;
        }

        public bool ShouldNotLaunch()
        {
            return Environment.GetEnvironmentVariable(_environmentalVariable) != null;
        }
    }

    public class KnownDoNotLaunchStrategies
    {
        public readonly IShouldNotLaunchDiffTool VisualStudioTeamServices = new DoNotLaunchWhenEnvVariableIsPresent("VSTS");
    }

    public class KnownDiffTools
    {
        public readonly DiffTool BeyondCompare4 = new DiffTool("Beyond Compare 4", @"Beyond Compare 4\BCompare.exe", BeyondCompareArgs);
        
        public readonly DiffTool KDiff3 = new DiffTool("KDiff3", @"KDiff3\kdiff3.exe", KDiffArgs);

        public readonly DiffTool TortoiseGitMerge = new DiffTool("Tortoise Git Merge", @"TortoiseGit\bin\TortoiseGitMerge.exe", TortoiseGitMergeArgs);

        public readonly DiffTool CurrentVisualStudio = new CurrentlyRunningVisualStudioDiffTool();

        public static KnownDiffTools Instance { get; } = new KnownDiffTools();

        private static string BeyondCompareArgs(string received, string approved, bool approvedExists)
        {
            return approvedExists
                ? $"\"{received}\" \"{approved}\" /mergeoutput=\"{approved}\""
                : $"\"{received}\" /mergeoutput=\"{approved}\"";
        }

        private static string KDiffArgs(string received, string approved, bool approvedExists)
        {
            return approvedExists
                ? $"\"{received}\" \"{approved}\" -o \"{approved}\""
                : $"\"{received}\" -o \"{approved}\"";
        }

        private static string IntelliJArgs(string received, string approved, bool approvedExists)
        {
            return approvedExists
                ? $"merge \"{received}\" \"{approved}\" \"{approved}\""
                : $"merge \"{received}\" \"{received}\" \"{approved}\"";
        }

        static string TortoiseGitMergeArgs(string received, string approved, bool approvedExists)
        {
            if (!approvedExists)
                File.AppendAllText(approved, string.Empty);

            return $"\"{received}\" \"{approved}\"";
        }
    }
}