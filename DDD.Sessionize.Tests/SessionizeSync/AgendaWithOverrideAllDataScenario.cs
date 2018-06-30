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
    public class AgendaWithOverrideAllDataScenario : Scenario
    {
        public void GivenExistingSession()
        {
            _documentDbRepository = new DocumentDbRepositoryMock<SessionOrPresenter>();
            _documentDbRepository.CreateItemAsync(new SessionOrPresenter(new Session
            {
                Id = Guid.NewGuid(),
                Title = "Title",
                Abstract = "Abstract",
                CreatedDate = new DateTimeOffset(2010, 1, 1, 12, 0, 0, TimeSpan.Zero),
                ExternalId = "1",
                Format = "Format",
                InAgenda = false,
                Level = "Level",
                PresenterIds = new[] {Guid.NewGuid()},
                Tags = new[] {"tag1", "tag2"},
                DataFields = new Dictionary<string, string> {{"key", "value"}}
            }));
        }

        public void AndGivenSessionizeHasPresentersAndSessions()
        {
            _sessionizeApiClient = SessionizeApiClientMock.Get(
                GetResource("AgendaWithOverrideAllDataScenarioMock.json"));
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
        
        public AgendaWithOverrideAllDataScenario(ITestOutputHelper output)
        {
            _logger = new Xunit2Logger(output);
            Xunit2BddfyTextReporter.Instance.RegisterOutput(output);
        }

        private StaticDateTimeProvider _dateTimeProvider = new StaticDateTimeProvider(new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero));
        private IDocumentDbRepository<SessionOrPresenter> _documentDbRepository;
        private ISessionizeApiClient _sessionizeApiClient;
        private readonly Xunit2Logger _logger;
        private SessionOrPresenter[] _readModel;
        private const string TestDatabaseId = "AddDeleteAndUpdateScenario";
        private const string TestCollectionId = "Sessions";
    }
}
