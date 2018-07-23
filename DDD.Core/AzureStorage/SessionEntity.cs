using System;
using DDD.Core.Domain;
using DDD.Core.Time;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace DDD.Core.AzureStorage
{
    public class SessionEntity : TableEntity
    {
        public SessionEntity() {}

        public SessionEntity(Session session, string conferenceInstance)
        {
            PartitionKey = conferenceInstance;
            RowKey = session.Id.ToString();
            ExternalId = session.ExternalId;
            Title = session.Title;
            Session = JsonConvert.SerializeObject(session);
        }

        public Session GetSession()
        {
            return JsonConvert.DeserializeObject<Session>(Session);
        }

        public Guid Id => Guid.Parse(RowKey);

        public string ExternalId { get; set; }
        public string Title { get; set; }
        public string Session { get; set; }

        public SessionEntity Update(Session newData, IDateTimeProvider dateTimeProvider)
        {
            if (newData.Id != Id)
                throw new ArgumentException($"Attempt to a session with a different one {Id} vs {newData.Id}.");

            var existing = GetSession();
            newData.UpdateFromExisting(existing, dateTimeProvider);

            ExternalId = newData.ExternalId;
            Title = newData.Title;
            Session = JsonConvert.SerializeObject(newData);

            return this;
        }
    }
}
