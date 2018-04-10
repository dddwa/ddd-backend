using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DDD.Sessionize.Sessionize
{
    public interface ISessionizeApiClient
    {
        Task<SessionizeResponse> GetAllData();
        string GetUrl();
    }

    public class SessionizeApiClient : ISessionizeApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _sessionizeApiId;

        public SessionizeApiClient(HttpClient httpClient, string sessionizeApiId)
        {
            _httpClient = httpClient;
            _sessionizeApiId = sessionizeApiId;
        }

        public async Task<SessionizeResponse> GetAllData()
        {
            var response = await _httpClient.GetAsync(GetUrl());
            response.EnsureSuccessStatusCode();
            var asJson = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SessionizeResponse>(asJson);
        }

        public string GetUrl()
        {
            return $"https://sessionize.com/api/v2/{_sessionizeApiId}/view/all";
        }
    }
}
