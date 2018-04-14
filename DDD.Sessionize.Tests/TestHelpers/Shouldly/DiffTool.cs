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
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Shouldly.Core
{
    public class DiffTool
    {
        public delegate string ArgumentGenerator(string received, string approved, bool approvedExists);

        public DiffTool(string name, string path, ArgumentGenerator argGenerator)
        {
            Name = name;
            _path = path == null ? null : (Path.IsPathRooted(path) && File.Exists(path) ? path : Discover(path));
            _argGenerator = argGenerator;
        }

        public string Name { get; }

        public bool Exists()
        {
            return _path != null;
        }

        public void Open(string receivedPath, string approvedPath, bool approvedExists)
        {
            Process.Start(_path, _argGenerator(receivedPath, approvedPath, approvedExists));
        }

        private static string Discover(string path)
        {
            var exeName = Path.GetFileName(path);
            var fullPathFromPathEnv = GetFullPath(exeName);
            if (!string.IsNullOrEmpty(fullPathFromPathEnv))
                return fullPathFromPathEnv;

            return new[]
                {
                    Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
                    Environment.GetEnvironmentVariable("ProgramFiles"),
                    Environment.GetEnvironmentVariable("ProgramW6432")
                }
                .Where(p => p != null)
                .Select(pf => Path.Combine(pf, path))
                .FirstOrDefault(File.Exists);
        }

        private static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            return Environment.GetEnvironmentVariable("PATH")
                .Split(';')
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(path => path.Trim('"'))
                .Select(path => TryCombine(fileName, path))
                .FirstOrDefault(File.Exists);
        }

        private static string TryCombine(string fileName, string path)
        {
            try
            {
                return Path.Combine(path, fileName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private readonly ArgumentGenerator _argGenerator;
        private readonly string _path;
    }
}
