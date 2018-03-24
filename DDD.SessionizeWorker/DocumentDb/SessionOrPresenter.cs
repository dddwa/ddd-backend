using DDD.Core.Domain;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace DDD.SessionizeWorker.DocumentDb
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

        public SessionOrPresenter Update(Session session)
        {
            session.Id = Session.Id;
            return new SessionOrPresenter(session);
        }

        public SessionOrPresenter Update(Presenter presenter)
        {
            presenter.Id = Presenter.Id;
            return new SessionOrPresenter(presenter);
        }
    }
}