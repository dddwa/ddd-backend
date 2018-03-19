using System.Net.Http;
using System.Threading.Tasks;
using DDD.SessionizeWorker.Sessionize;

namespace DDD.SessionizeWorker
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            using (var httpClient = new HttpClient())
            {
                var apiClient = new SessionizeApiClient(httpClient);
                var sessionizeData = await apiClient.GetAllData("mdhxdmti");


            }
        }
    }
}
