using System;
using DDD.Core.Domain;
using DDD.Core.Time;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace DDD.Sessionize
{
    public class SessionOrPresenter : Document
    {
        [JsonProperty("Presenter")]
        public Presenter Presenter { get; private set; }
        [JsonProperty("Session")]
        public Session Session { get; private set; }

        public SessionOrPresenter() { }

        public SessionOrPresenter(Session session)
        {
            Session = session;
            Id = session.Id.ToString();
        }

        public SessionOrPresenter(Presenter presenter)
        {
            Presenter = presenter;
            Id = presenter.Id.ToString();
        }

        public SessionOrPresenter Update(Session session, IDateTimeProvider dateTimeProvider, bool deleteNonExistantData)
        {
            session.UpdateFromExisting(Session, dateTimeProvider, deleteNonExistantData);
            return new SessionOrPresenter(session);
        }

        public SessionOrPresenter Update(Presenter presenter, IDateTimeProvider dateTimeProvider, bool deleteNonExistantData)
        {
            presenter.UpdateFromExisting(Presenter, dateTimeProvider, deleteNonExistantData);
            return new SessionOrPresenter(presenter);
        }

        public Guid GetId()
        {
            return Session?.Id ?? Presenter.Id;
        }
    }
}