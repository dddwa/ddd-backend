using System;
using System.Collections.Generic;
using System.Linq;
using DDD.Core.Time;

namespace DDD.Core.Domain
{
    public class Session
    {
        public Guid Id { get; set; }
        public string ExternalId { get; set; }
        public string Title { get; set; }
        public string Abstract { get; set; }
        public Guid[] PresenterIds { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? ModifiedDate { get; set; }
        public string Format { get; set; }
        public string Level { get; set; }
        public string[] Tags { get; set; }
        public Dictionary<string, string> DataFields { get; set; }

        public bool DataEquals(Session p)
        {
            return Title == p.Title
                   && Abstract == p.Abstract
                   && string.Join(",", PresenterIds.OrderBy(x => x)) == string.Join(",", p.PresenterIds.OrderBy(x => x))
                   && Format == p.Format
                   && Level == p.Level
                   && string.Join("|", Tags.OrderBy(x => x)) == string.Join("|", p.Tags.OrderBy(x => x))
                   && DataFields.Count == p.DataFields.Count && !DataFields.Except(p.DataFields).Any();
        }

        public void UpdateFromExisting(Session existingSession, IDateTimeProvider dateTimeProvider)
        {

            Id = existingSession.Id;
            CreatedDate = existingSession.CreatedDate;
            ModifiedDate = dateTimeProvider.Now();

            Title = Title ?? existingSession.Title;
            Abstract = Abstract ?? existingSession.Abstract;
            Format = Format ?? existingSession.Format;
            Level = Level ?? existingSession.Level;
            Tags = Tags != null && Tags.Length > 0 ? Tags : existingSession.Tags;
            PresenterIds = PresenterIds != null && PresenterIds.Length > 0 ? PresenterIds : existingSession.PresenterIds;
            existingSession.DataFields.Keys.Except(DataFields.Keys).ToList().ForEach(key =>
                DataFields[key] = existingSession.DataFields[key]);
        }
    }
}