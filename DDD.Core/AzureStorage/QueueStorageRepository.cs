using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace DDD.Core.AzureStorage
{
    public interface IQueueStorageRepository<T> where T : class, new()
    {
        Task InitializeAsync();
        Task PushAsync(T item);
    }

    public class QueueStorageRepository<T> : IQueueStorageRepository<T> where T : class, new()
    {
        private readonly CloudQueue _queue;

        public QueueStorageRepository(CloudStorageAccount storageAccount, string queueName)
        {
            var client = storageAccount.CreateCloudQueueClient();
            _queue = client.GetQueueReference(queueName);
        }

        public async Task InitializeAsync()
        {
            await _queue.CreateIfNotExistsAsync();
        }

        public async Task PushAsync(T item)
        {
            await _queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(item)));
        }
    }
}
