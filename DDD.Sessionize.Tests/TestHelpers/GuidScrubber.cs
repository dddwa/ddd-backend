using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DDD.Sessionize.Tests.TestHelpers
{
    public class GuidScrubber
    {
        private readonly Dictionary<string, Guid> _conversionMap = new Dictionary<string, Guid>();

        public string Scrub(string stringToScrub)
        {
            return Regex.Replace(stringToScrub, "([{(\"]?)([0-9A-F]{8}[-]?([0-9A-F]{4}[-]?){3}[0-9A-F]{12})([\")}]?)", ReplaceGuid, RegexOptions.IgnoreCase);
        }

        private string ReplaceGuid(Match match)
        {
            var guid = match.Groups[2].Value.ToLower();
            if (!_conversionMap.ContainsKey(guid))
                _conversionMap[guid] = ToGuid(_conversionMap.Keys.Count);
            return match.Groups[1].Value + _conversionMap[guid] + match.Groups[4].Value;
        }

        public static Guid ToGuid(int value)
        {
            var bytes = new byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes, 0);
            Array.Reverse(bytes, 0, bytes.Length);
            return new Guid(bytes);
        }
    }

}
