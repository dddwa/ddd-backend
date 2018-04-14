using System;
using System.Linq;
using System.Threading.Tasks;
using DDD.Core.DocumentDb;
using DDD.Core.Domain;
using DDD.Core.Time;
using DDD.Sessionize.Sessionize;
using Microsoft.Extensions.Logging;

namespace DDD.Sessionize.Sync
{
    public static class SyncService
    {
        public static async Task Sync(ISessionizeApiClient apiClient, DocumentDbRepository<SessionOrPresenter> repo, ILogger log, IDateTimeProvider dateTimeProvider)
        {
            var sessionizeData = await apiClient.GetAllData();

            log.LogInformation("Retrieved {sessionCount} sessions, {presenterCount} presenters from Sessionize API {sessionizeApiUrl}", sessionizeData.Sessions.Length, sessionizeData.Speakers.Length, apiClient.GetUrl());

            var adapter = new SessionizeAdapter.SessionizeAdapter();
            var sourceData = adapter.Convert(sessionizeData, dateTimeProvider);

            log.LogInformation("Sessionize data successfully adapted to DDD domain model: {sessionCount} sessions and {presenterCount} presenters", sourceData.Item1.Length, sourceData.Item2.Length);

            var destinationData = (await repo.GetAllItemsAsync()).ToArray();

            log.LogInformation("Existing read model retrieved: {sessionCount} sessions and {presenterCount} presenters", destinationData.Count(x => x.Session != null), destinationData.Count(x => x.Presenter != null));

            await PerformSync(repo, sourceData.Item1, sourceData.Item2, destinationData, log);
        }

        private static async Task PerformSync(DocumentDbRepository<SessionOrPresenter> repo, Session[] sourceSessions, Presenter[] sourcePresenters, SessionOrPresenter[] destinationData, ILogger log)
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
            if (newPresenters.Any())
                log.LogInformation("Adding new presenters to read model: {newPresenterIds}", (object) newPresenters.Select(x => x.Id).ToArray());
            await Task.WhenAll(newPresenters.Select(p => repo.CreateItemAsync(new SessionOrPresenter(p))));
            if (deletedPresenters.Any())
                log.LogInformation("Deleting presenters from read model: {deletedPresenterIds}", (object) deletedPresenters.Select(x => x.Id).ToArray());
            await Task.WhenAll(deletedPresenters.Select(p => repo.DeleteItemAsync(p.Id.ToString())));
            if (editedPresenters.Any())
                log.LogInformation("Updating presenters in read model: {editedPresenterIds}", (object) editedPresenters.Select(x => x.dest.Id).ToArray());
            await Task.WhenAll(editedPresenters.Select(x => repo.UpdateItemAsync(x.dest.Id, x.dest.Update(x.src))));
            if (!newPresenters.Any() && !deletedPresenters.Any() && !editedPresenters.Any())
                log.LogInformation("Presenters up to date in read model");

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
            if (newSessions.Any())
                log.LogInformation("Adding new sessions to read model: {newSessionIds}", (object)newSessions.Select(x => x.Id).ToArray());
            await Task.WhenAll(newSessions.Select(s => repo.CreateItemAsync(new SessionOrPresenter(s))));
            if (deletedSessions.Any())
                log.LogInformation("Deleting sessions from read model: {deletedSessionIds}", (object)deletedSessions.Select(x => x.Id).ToArray());
            await Task.WhenAll(deletedSessions.Select(s => repo.DeleteItemAsync(s.Id.ToString())));
            if (editedSessions.Any())
                log.LogInformation("Editing sessions in read model: {editedSessionIds}", (object)editedSessions.Select(x => x.dest.Id).ToArray());
            await Task.WhenAll(editedSessions.Select(x => repo.UpdateItemAsync(x.dest.Id, x.dest.Update(x.src))));
            if (!newSessions.Any() && !deletedSessions.Any() && !editedSessions.Any())
                log.LogInformation("Sessions up to date in read model");
        }
    }
}
