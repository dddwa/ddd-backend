using System;
using DDD.Core.Domain;
using DDD.Core.Time;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace DDD.Core.AzureStorage
{
    public class PresenterEntity : TableEntity
    {
        public PresenterEntity() { }

        public PresenterEntity(Presenter presenter, string conferenceInstance)
        {
            PartitionKey = conferenceInstance;
            RowKey = presenter.Id.ToString();
            ExternalId = presenter.ExternalId;
            Name = presenter.Name;
            Presenter = JsonConvert.SerializeObject(presenter);
        }

        public Presenter GetPresenter()
        {
            return JsonConvert.DeserializeObject<Presenter>(Presenter);
        }

        public Guid Id => Guid.Parse(RowKey);

        public string ExternalId { get; set; }
        public string Name { get; set; }
        public string Presenter { get; set; }

        public PresenterEntity Update(Presenter newData, IDateTimeProvider dateTimeProvider)
        {
            if (newData.Id != Id)
                throw new ArgumentException($"Attempt to a presenter with a different one {Id} vs {newData.Id}.");

            var existing = GetPresenter();
            newData.UpdateFromExisting(existing, dateTimeProvider);

            ExternalId = newData.ExternalId;
            Name = newData.Name;
            Presenter = JsonConvert.SerializeObject(newData);

            return this;
        }
    }
}
