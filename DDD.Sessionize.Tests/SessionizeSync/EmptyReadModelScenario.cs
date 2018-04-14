using System;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using DDD.Core.DocumentDb;
using DDD.Sessionize.Sessionize;
using DDD.Sessionize.Sync;
using DDD.Sessionize.Tests.TestHelpers;
using Shouldly;
using Xunit.Abstractions;

namespace DDD.Sessionize.Tests.SessionizeSync
{
    public class EmptyReadModelScenario : Scenario
    {
        public async Task GivenEmptyReadModel()
        {
            _documentDbRepository = await EmptyDocumentDb.InitializeAsync<SessionOrPresenter>(TestDatabaseId, TestCollectionId);
        }
        
        public void AndGivenSessionizeHasPresentersAndSessions()
        {
            _sessionizeApiClient = SessionizeApiClientMock.Get(
                GetResource("EmptyReadModelScenarioMock.json"));
        }

        public async Task WhenPerformingSync()
        {
            await SyncService.Sync(_sessionizeApiClient, _documentDbRepository, _logger, _dateTimeProvider);
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

        public EmptyReadModelScenario(ITestOutputHelper output)
        {
            _logger = new Xunit2Logger(output);
            Xunit2BddfyTextReporter.Instance.RegisterOutput(output);
        }

        private readonly StaticDateTimeProvider _dateTimeProvider = new StaticDateTimeProvider(new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero));
        private DocumentDbRepository<SessionOrPresenter> _documentDbRepository;
        private ISessionizeApiClient _sessionizeApiClient;
        private readonly Xunit2Logger _logger;
        private SessionOrPresenter[] _readModel;
        private const string TestDatabaseId = "EmptyReadModelScenario";
        private const string TestCollectionId = "Sessions";
    }
}
