using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace DDD.Sessionize.Tests.TestHelpers
{
    public class Xunit2Logger : ILogger, IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly StringWriter _localWriter = new StringWriter();

        public Xunit2Logger(ITestOutputHelper output)
        {
            _output = output;
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _output.WriteLine(state.ToString());
            _localWriter.WriteLine(state.ToString());
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose()
        {
        }

        public override string ToString()
        {
            return _localWriter.ToString();
        }
    }
}
