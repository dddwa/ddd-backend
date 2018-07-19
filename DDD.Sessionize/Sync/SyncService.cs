using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DDD.Core.AzureStorage;
using DDD.Core.Domain;
using DDD.Core.Time;
using DDD.Sessionize.Sessionize;
using Microsoft.Extensions.Logging;

namespace DDD.Sessionize.Sync
{
    public static class SyncService
    {
        public static async Task Sync(ISessionizeApiClient apiClient, ITableStorageRepository<SessionEntity> sessionRepo, ITableStorageRepository<PresenterEntity> presenterRepo, ILogger log, IDateTimeProvider dateTimeProvider, string conferenceInstance)
        {
            var sessionizeData = await apiClient.GetAllData();

            log.LogInformation("Retrieved {sessionCount} sessions, {presenterCount} presenters from Sessionize API {sessionizeApiUrl}", sessionizeData.Sessions.Length, sessionizeData.Speakers.Length, apiClient.GetUrl());

            var sourceData = SessionizeAdapter.SessionizeAdapter.Convert(sessionizeData, dateTimeProvider);

            log.LogInformation("Sessionize data successfully adapted to DDD domain model: {sessionCount} sessions and {presenterCount} presenters", sourceData.Item1.Length, sourceData.Item2.Length);

            var destinationSessions = (await sessionRepo.GetAllAsync(conferenceInstance)).ToArray();
            var destinationPresenters = (await presenterRepo.GetAllAsync(conferenceInstance)).ToArray();

            log.LogInformation("Existing read model retrieved: {sessionCount} sessions and {presenterCount} presenters", destinationSessions.Length, destinationPresenters.Length);

            await PerformSync(conferenceInstance, sessionRepo, presenterRepo, sourceData.Item1, sourceData.Item2, destinationSessions, destinationPresenters, log, dateTimeProvider);
        }

        private static async Task PerformSync(string conferenceInstance,
            ITableStorageRepository<SessionEntity> sessionRepo,
            ITableStorageRepository<PresenterEntity> presenterRepo,
            Session[] sourceSessions,
            Presenter[] sourcePresenters,
            IList<SessionEntity> destinationSessions,
            IList<PresenterEntity> destinationPresenters,
            ILogger log,
            IDateTimeProvider dateTimeProvider)
        {
            // Diff presenters
            var newPresenters = sourcePresenters.Except(destinationPresenters.Select(x => x.GetPresenter()), new PresenterSync()).ToArray();
            var deletedPresenters = destinationPresenters.Select(x => x.GetPresenter()).Except(sourcePresenters, new PresenterSync()).ToArray();
            var existingPresenters = destinationPresenters.Join(sourcePresenters,
                dest => dest.ExternalId,
                src => src.ExternalId,
                (dest, src) => new { dest, src }).ToArray();
            var editedPresenters = existingPresenters.Where(x => !x.dest.GetPresenter().DataEquals(x.src)).ToArray();

            // Sync presenters
            if (newPresenters.Any())
                log.LogInformation("Adding new presenters to read model: {newPresenterIds}", (object) newPresenters.Select(x => x.Id).ToArray());
            await Task.WhenAll(newPresenters.Select(p => presenterRepo.CreateAsync(new PresenterEntity(p, conferenceInstance))));
            if (deletedPresenters.Any())
            {
                log.LogInformation("Deleting presenters from read model: {deletedPresenterIds}", (object)deletedPresenters.Select(x => x.Id).ToArray());
                await Task.WhenAll(deletedPresenters.Select(p => presenterRepo.DeleteAsync(conferenceInstance, p.Id.ToString())));
            }
            if (editedPresenters.Any())
                log.LogInformation("Updating presenters in read model: {editedPresenterIds}", (object) editedPresenters.Select(x => x.dest.Id).ToArray());
            await Task.WhenAll(editedPresenters.Select(x => presenterRepo.UpdateAsync(x.dest.Update(x.src, dateTimeProvider))));
            if (!newPresenters.Any() && !deletedPresenters.Any() && !editedPresenters.Any())
                log.LogInformation("Presenters up to date in read model");

            // Exclude cancelled sessions
            sourceSessions = sourceSessions.Where(s => !s.Title.Contains("[Cancelled]")).ToArray();

            // Update presenter ids on sessions
            sourceSessions.ToList().ForEach(s => s.PresenterIds = s.PresenterIds.Select(pId =>
                existingPresenters
                    .Where(x => x.src.Id == pId)
                    .Select(x => x.dest.Id)
                    .Cast<Guid?>()
                    .SingleOrDefault() ?? pId)
                .ToArray());

            // Diff speakers
            var newSessions = sourceSessions.Except(destinationSessions.Select(x => x.GetSession()), new SessionSync()).ToArray();
            var deletedSessions = destinationSessions.Select(x => x.GetSession()).Except(sourceSessions, new SessionSync()).ToArray();
            var existingSessions = destinationSessions.Join(sourceSessions,
                dest => dest.ExternalId,
                src => src.ExternalId,
                (dest, src) => new { dest, src }).ToArray();
            var editedSessions = existingSessions.Where(x => !x.dest.GetSession().DataEquals(x.src)).ToArray();

            // Sync speakers
            if (newSessions.Any())
                log.LogInformation("Adding new sessions to read model: {newSessionIds}", (object)newSessions.Select(x => x.Id).ToArray());
            await Task.WhenAll(newSessions.Select(s => sessionRepo.CreateAsync(new SessionEntity(s, conferenceInstance))));
            if (deletedSessions.Any())
            {
                log.LogInformation("Deleting sessions from read model: {deletedSessionIds}", (object)deletedSessions.Select(x => x.Id).ToArray());
                await Task.WhenAll(deletedSessions.Select(s => sessionRepo.DeleteAsync(conferenceInstance, s.Id.ToString())));
            }
            if (editedSessions.Any())
                log.LogInformation("Editing sessions in read model: {editedSessionIds}", (object)editedSessions.Select(x => x.dest.Id).ToArray());
            await Task.WhenAll(editedSessions.Select(x => sessionRepo.UpdateAsync(x.dest.Update(x.src, dateTimeProvider))));
            if (!newSessions.Any() && !deletedSessions.Any() && !editedSessions.Any())
                log.LogInformation("Sessions up to date in read model");
        }
    }
}
