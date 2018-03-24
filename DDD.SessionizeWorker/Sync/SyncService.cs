using System;
using System.Linq;
using System.Threading.Tasks;
using DDD.Core.Domain;
using DDD.SessionizeWorker.DocumentDb;
using DDD.SessionizeWorker.Sessionize;

namespace DDD.SessionizeWorker.Sync
{
    public static class SyncService
    {
        public static async Task Sync(ISessionizeApiClient apiClient, string sessionizeApiKey, DocumentDbRepository<SessionOrPresenter> repo)
        {
            var sessionizeData = await apiClient.GetAllData(sessionizeApiKey);
            var adapter = new SessionizeAdapter.SessionizeAdapter();
            var sourceData = adapter.Convert(sessionizeData);

            var destinationData = (await repo.GetAllItemsAsync()).ToArray();

            await PerformSync(repo, sourceData.Item1, sourceData.Item2, destinationData);
        }

        public static async Task PerformSync(DocumentDbRepository<SessionOrPresenter> repo, Session[] sourceSessions, Presenter[] sourcePresenters, SessionOrPresenter[] destinationData)
        {
            var destinationPresenters = destinationData.Where(x => x.Presenter != null).ToArray();
            var destinationSessions = destinationData.Where(x => x.Session != null).ToArray();

            // Diff presenters
            var newPresenters = sourcePresenters.Except(destinationPresenters.Select(x => x.Presenter), new PresenterSync()).ToArray();
            var deletedPresenters = destinationPresenters.Select(x => x.Presenter).Except(sourcePresenters, new PresenterSync()).ToArray();
            var existingPresenters = destinationPresenters.Join(sourcePresenters,
                dest => dest.Presenter.ExternalId,
                src => src.ExternalId,
                (dest, src) => new { dest, src }).ToArray();
            var editedPresenters = existingPresenters.Where(x => !x.dest.Presenter.DataEquals(x.src)).ToArray();

            // Sync presenters
            await Task.WhenAll(newPresenters.Select(p => repo.CreateItemAsync(new SessionOrPresenter(p))));
            await Task.WhenAll(deletedPresenters.Select(p => repo.DeleteItemAsync(p.Id.ToString())));
            await Task.WhenAll(editedPresenters.Select(x => repo.UpdateItemAsync(x.dest.Id, x.dest.Update(x.src))));

            // Update presenter ids on sessions
            sourceSessions.ToList().ForEach(s => s.PresenterIds = s.PresenterIds.Select(pId =>
                existingPresenters
                    .Where(x => x.src.Id == pId)
                    .Select(x => x.dest.Presenter.Id)
                    .Cast<Guid?>()
                    .SingleOrDefault() ?? pId)
                .ToArray());

            // Diff speakers
            var newSessions = sourceSessions.Except(destinationSessions.Select(x => x.Session), new SessionSync()).ToArray();
            var deletedSessions = destinationSessions.Select(x => x.Session).Except(sourceSessions, new SessionSync()).ToArray();
            var existingSessions = destinationSessions.Join(sourceSessions,
                dest => dest.Session.ExternalId,
                src => src.ExternalId,
                (dest, src) => new { dest, src }).ToArray();
            var editedSessions = existingSessions.Where(x => !x.dest.Session.DataEquals(x.src)).ToArray();

            // Sync speakers
            await Task.WhenAll(newSessions.Select(s => repo.CreateItemAsync(new SessionOrPresenter(s))));
            await Task.WhenAll(deletedSessions.Select(s => repo.DeleteItemAsync(s.Id.ToString())));
            await Task.WhenAll(editedSessions.Select(x => repo.UpdateItemAsync(x.dest.Id, x.dest.Update(x.src))));
        }
    }
}
