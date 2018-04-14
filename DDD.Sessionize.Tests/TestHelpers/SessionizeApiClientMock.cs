using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DDD.Sessionize.Sessionize;

namespace DDD.Sessionize.Tests.TestHelpers
{
    public class SessionizeApiClientMock : HttpMessageHandler
    {
        public static ISessionizeApiClient Get(byte[] mockResponse)
        {
            return Get(Encoding.UTF8.GetString(mockResponse));
        }

        public static ISessionizeApiClient Get(string mockResponse)
        {
            return new SessionizeApiClient(
                new HttpClient(
                    new SessionizeApiClientMock(mockResponse)
                ),
                "{APIID}"
            );
        }

        private readonly string _sessionizeResponse;

        private SessionizeApiClientMock(string sessionizeResponse)
        {
            _sessionizeResponse = sessionizeResponse;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_sessionizeResponse, Encoding.UTF8, "application/json")
            });
        }
    }
}
