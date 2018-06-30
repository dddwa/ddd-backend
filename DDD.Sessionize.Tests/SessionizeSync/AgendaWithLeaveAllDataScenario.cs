using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DDD.Core.DocumentDb;
using DDD.Core.Domain;
using DDD.Sessionize.Sessionize;
using DDD.Sessionize.Sync;
using DDD.Sessionize.Tests.TestHelpers;
using Shouldly;
using Xunit.Abstractions;
using Scenario = DDD.Sessionize.Tests.TestHelpers.Scenario;

namespace DDD.Sessionize.Tests.SessionizeSync
{
    public class AgendaWithLeaveAllDataScenario : Scenario
    {
        public void GivenExistingSession()
        {
            var presenterId = Guid.NewGuid();

            _documentDbRepository = new DocumentDbRepositoryMock<SessionOrPresenter>();
            _documentDbRepository.CreateItemAsync(new SessionOrPresenter(new Session
            {
                Id = Guid.NewGuid(),
                Title = "Existing title",
                Abstract = "Existing abstract",
                CreatedDate = new DateTimeOffset(2010, 1, 1, 12, 0, 0, TimeSpan.Zero),
                ExternalId = "1",
                Format = "Existing format",
                InAgenda = false,
                Level = "Existing level",
                PresenterIds = new[] { presenterId },
                Tags = new[] {"existing tag 1", "existing tag 2"},
                DataFields = new Dictionary<string, string> {{"existing key", "existing value"}}
            }));

            _documentDbRepository.CreateItemAsync(new SessionOrPresenter(new Presenter
            {
                Id = presenterId,
                ExternalId = Guid.Parse("86ab1cc5-ddd4-4ed4-b919-dd0eb429bfa9").ToString(),
                CreatedDate = new DateTimeOffset(2010, 1, 1, 12, 0, 0, TimeSpan.Zero),
                Name = "Existing name",
                Bio = "Existing bio",
                WebsiteUrl = "Existing website",
                TwitterHandle = "Existing twitter",
                Tagline = "Existing tagline",
                ProfilePhotoUrl = "Existing profile"
            }));

            _documentDbRepository.CreateItemAsync(new SessionOrPresenter(new Session
            {
                Id = Guid.NewGuid(),
                Title = "Non-agenda session",
                Abstract = "Abstract",
                CreatedDate = new DateTimeOffset(2010, 1, 1, 12, 0, 0, TimeSpan.Zero),
                ExternalId = "2",
                Format = "Format",
                InAgenda = false,
                Level = "Level",
                PresenterIds = new[] { Guid.NewGuid() },
                Tags = new[] { "tag1", "tag2" },
                DataFields = new Dictionary<string, string> { { "key", "value" } }
            }));

            _documentDbRepository.CreateItemAsync(new SessionOrPresenter(new Presenter
            {
                Id = Guid.NewGuid(),
                ExternalId = "3",
                CreatedDate = new DateTimeOffset(2010, 1, 1, 12, 0, 0, TimeSpan.Zero),
                Name = "Non-agenda presenter",
                Bio = "Existing bio",
                WebsiteUrl = "Existing website",
                TwitterHandle = "Existing twitter",
                Tagline = "Existing tagline",
                ProfilePhotoUrl = "Existing profile"
            }));
        }

        public void AndGivenSessionizeHasPresentersAndSessions()
        {
            _sessionizeApiClient = SessionizeApiClientMock.Get(
                GetResource("AgendaWithLeaveAllDataScenarioMock.json"));
        }

        public async Task WhenPerformingSyncForAgenda()
        {
            await SyncService.Sync(_sessionizeApiClient, _documentDbRepository, _logger, _dateTimeProvider, deleteNonExistantData: false, inAgenda: true);
        }

        public async Task ThenTheReadModelIsPopulated()
        {
            _readModel = (await _documentDbRepository.GetAllItemsAsync()).ToArray();
            _readModel.ShouldNotBeEmpty();
        }

        public void AndTheReadModelHasTheCorrectPresenters()
        {
            Approve(SessionOrPresenterAssertions.PreparePresentersForApproval(_readModel), "json");
        }

        public void AndTheReadModelHasTheCorrectSessions()
        {
            Approve(SessionOrPresenterAssertions.PrepareSessionsForApproval(_readModel), "json");
        }

        public void AndTheLoggerOutputIsCorrect()
        {
            Approve(_logger.ToString());
        }
        
        public AgendaWithLeaveAllDataScenario(ITestOutputHelper output)
        {
            _logger = new Xunit2Logger(output);
            Xunit2BddfyTextReporter.Instance.RegisterOutput(output);
        }

        private readonly StaticDateTimeProvider _dateTimeProvider = new StaticDateTimeProvider(new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero));
        private IDocumentDbRepository<SessionOrPresenter> _documentDbRepository;
        private ISessionizeApiClient _sessionizeApiClient;
        private readonly Xunit2Logger _logger;
        private SessionOrPresenter[] _readModel;
    }
}
