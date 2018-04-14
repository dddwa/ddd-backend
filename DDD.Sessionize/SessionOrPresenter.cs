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

        public SessionOrPresenter Update(Session session, IDateTimeProvider dateTimeProvider)
        {
            session.Id = Session.Id;
            session.CreatedDate = Session.CreatedDate;
            session.ModifiedDate = dateTimeProvider.Now();
            return new SessionOrPresenter(session);
        }

        public SessionOrPresenter Update(Presenter presenter, IDateTimeProvider dateTimeProvider)
        {
            presenter.Id = Presenter.Id;
            presenter.CreatedDate = Presenter.CreatedDate;
            presenter.ModifiedDate = dateTimeProvider.Now();
            return new SessionOrPresenter(presenter);
        }

        public Guid GetId()
        {
            return Session?.Id ?? Presenter.Id;
        }
    }
}