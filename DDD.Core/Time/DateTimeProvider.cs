using System;

namespace DDD.Core.Time
{
    public interface IDateTimeProvider
    {
        DateTimeOffset Now();
    }

    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset Now()
        {
            return DateTimeOffset.UtcNow;
        }
    }
}
