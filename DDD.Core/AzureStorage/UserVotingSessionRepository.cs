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
        public static string CreatePartitionKey(string id)
        {
            return id.Substring(0, 1);
        }
        
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [JsonProperty(PropertyName = "_ttl")]
        public DateTimeOffset Expiry { get; set; } = DateTimeOffset.UtcNow.AddDays(1);
        public List<string> SessionIds { get; set; } = new List<string>();

        public string PartitionKey => Id.Substring(0, 1);

        public Tuple<string, string> Next()
        {
            if (SessionIds.Count > 2)
            {
                var one = SessionIds[0];
                var two = SessionIds[1];
                
                SessionIds.RemoveRange(0,2);
                Expiry = DateTimeOffset.UtcNow.AddDays(1);
                
                return new Tuple<string, string>(one, two);
            }
            else
            {
                throw new ApplicationException("Not enough vote ids in the set to get the next pair.");
            }
                
        }
    }
    
    public class UserVotingSessionRepository
    {
        private readonly CosmosClient _cosmosClient;
        private readonly string _databaseId;
        private readonly string _containerId;
        private readonly Random _random;

        public UserVotingSessionRepository(CosmosClient client, string databaseId, string containerId)
        {
            this._cosmosClient = client;
            this._databaseId = databaseId;
            this._containerId = containerId;
            this._random = new Random();
        }

        public async Task<Tuple<string, string>> GetSessionIds(Lazy<Task<List<Session>>> feed, string id = null)
        {
            // add a check for safety here, if it's null we will fall through the catch block and create a new one for
            // the user anyway
            id ??= Guid.NewGuid().ToString();
            
            var (_, container) = await Init();

            try
            {
                ItemResponse<UserVotingSession> userResponse =
                    await container.ReadItemAsync<UserVotingSession>(id, new PartitionKey(UserVotingSession.CreatePartitionKey(id)));

                UserVotingSession user = userResponse.Resource;
                
                if (user.SessionIds.Count <= 2)
                {
                    var sessions = await feed.Value;
                    user.SessionIds.AddRange(sessions.Select(x =>x.Id.ToString()).OrderBy(x => _random.Next()).ToList());
                }

                // take the next two items out of the user's session collection and then write back to the store
                var result = user.Next();

                await container.ReplaceItemAsync<UserVotingSession>(user, user.Id,
                    new PartitionKey(UserVotingSession.CreatePartitionKey(id)));

                return result;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var sessions = await feed.Value;

                var user = new UserVotingSession()
                {
                    SessionIds = sessions.Select(x => x.Id.ToString()).OrderBy(x => _random.Next()).ToList()
                };

                // take the first two before we create it so we dont waste time updating
                var result = user.Next();

                await container.CreateItemAsync<UserVotingSession>(user, new PartitionKey(UserVotingSession.CreatePartitionKey(id)));

                return result;
            }
        }

        private async Task<(Database, Container)> Init()
        {
            var databaseResponse = await this._cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseId);
            var database = databaseResponse.Database;
            
            var containerResponse = await database.CreateContainerIfNotExistsAsync(_containerId, "/PartitionKey");
            var container = containerResponse.Container;

            return (database, container);
        }
    }
}