using System;
using DDD.Core.Time;

namespace DDD.Sessionize.Tests.TestHelpers
{
    public class StaticDateTimeProvider : IDateTimeProvider
    {
        private readonly DateTimeOffset _now;

        public StaticDateTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }
        public DateTimeOffset Now()
        {
            return _now;
        }
    }
}
