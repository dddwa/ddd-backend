using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DDD.Core.AzureStorage;
using DDD.Core.Domain;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace DDD.Core.EloVoting
{
    public class UserVotingSession
    {
        public static readonly long DefaultTtl = 24 * 60 * 60;
        
        public static string CreatePartitionKey(string id)
        {
            return id.Substring(0, 1);
        }
        
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [JsonProperty(PropertyName = "ttl")]
        public long Expiry { get; set; } = DefaultTtl;
        public List<string> SessionIds { get; set; } = new List<string>();

        private string _partitionKey;

        public string PartitionKey
        {
            // this is required for cosmos, given we're using GUID's as keys just use the first letter to partition
            // the data - should give us an even spread of (26+10) buckets pretty much randomly filled
            get => string.IsNullOrEmpty(_partitionKey) ? Id.Substring(0, 1) : _partitionKey;
            set => _partitionKey = value;
        }

        public Tuple<string, string> Next()
        {
            if (SessionIds.Count > 2)
            {
                var one = SessionIds[0];
                var two = SessionIds[1];
                
                SessionIds.RemoveRange(0,2);
                Expiry = DefaultTtl;
                
                return new Tuple<string, string>(one, two);
            }

            return null;
        }
    }

    public interface IUserVotingSessionRepository
    {
        Task<Tuple<string, string>> NextSessionPair(Lazy<Task<List<Session>>> feed, string id = null);
    }
    
    public class UserVotingSessionRepository : IUserVotingSessionRepository
    {
        public static readonly int SessionCountLowWatermark = 2;
        
        private readonly CosmosClient _cosmosClient;
        private readonly Random _random;

        private Database _database;
        private Container _container;

        public UserVotingSessionRepository(CosmosClient client)
        {
            this._cosmosClient = client;
            this._random = new Random();
        }

        public async Task<Tuple<string, string>> NextSessionPair(Lazy<Task<List<Session>>> feed, string id = null)
        {
            // add a check for safety here, if it's null we will fall through the catch block and create a new one for
            // the user with the provided id
            id ??= Guid.NewGuid().ToString();
            
            try
            {
                ItemResponse<UserVotingSession> userResponse = await _container.ReadItemAsync<UserVotingSession>(id, new PartitionKey(UserVotingSession.CreatePartitionKey(id)));
                UserVotingSession user = userResponse.Resource;

                if (user.SessionIds.Count <= SessionCountLowWatermark)
                {
                    var sessions = await feed.Value;
                    user.SessionIds.AddRange(sessions
                        .Select(x =>x.Id.ToString())
                        .OrderBy(x => _random.Next())
                        .ToList());
                }

                var result = user.Next();

                // update the entry in the cosmos store, we've removed two items (and potentially added in a whole shuffled set again)
                await _container.ReplaceItemAsync<UserVotingSession>(user, user.Id, new PartitionKey(user.PartitionKey));

                return result;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // the session has either expired, or we have a new session to work with, so insert a record with a new
                // sample
                var sessions = await feed.Value;

                var user = new UserVotingSession()
                {
                    Id = id,
                    PartitionKey = UserVotingSession.CreatePartitionKey(id),
                    SessionIds = sessions
                        .Select(x => x.Id.ToString())
                        .OrderBy(x => _random.Next())
                        .ToList(),
                };

                // take the first two before we create it so we dont waste time updating
                var result = user.Next();

                await _container.CreateItemAsync<UserVotingSession>(user, new PartitionKey(UserVotingSession.CreatePartitionKey(id)));

                return result;
            }
        }
        

        public async Task InitialiseAsync(string databaseId, string containerId)
        {
            var databaseResponse = await this._cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            this._database = databaseResponse.Database;
            
            ContainerProperties properties = new ContainerProperties()
            {
                Id = containerId,
                PartitionKeyPath = "/PartitionKey",
                // Expire all documents after 1 day by default
                DefaultTimeToLive = 24 * 60 * 60
            };

            var containerResponse = await this._database.CreateContainerIfNotExistsAsync(properties);
            this._container = containerResponse.Container;            
        }
    }
}