using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DDD.Domain;
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
                var adapter = new SessionizeAdapter.SessionizeAdapter();
                var data = adapter.Convert(sessionizeData);
            }
        }
    }

    class CategoryItem
    {
        public CategoryType Type { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
    }

    enum CategoryType
    {
        SessionFormat,
        Tags,
        Level
    }
}
