using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DDD.SessionizeWorker.Sessionize
{
    public interface ISessionizeApiClient
    {
        Task<SessionizeResponse> GetAllData(string sessionizeApiId);
    }

    public class SessionizeApiClient : ISessionizeApiClient
    {
        private readonly HttpClient _httpClient;

        public SessionizeApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<SessionizeResponse> GetAllData(string sessionizeApiId)
        {
            var response = await _httpClient.GetAsync($"https://sessionize.com/api/v2/{sessionizeApiId}/view/all");
            response.EnsureSuccessStatusCode();
            var asJson = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SessionizeResponse>(asJson);
        }
    }
}
