using System;
using System.Linq;
using System.Threading.Tasks;
using DDD.Core.DocumentDb;
using DDD.Sessionize.Sessionize;
using DDD.Sessionize.Sync;
using DDD.Sessionize.Tests.TestHelpers;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using Xunit.Abstractions;
using Scenario = DDD.Sessionize.Tests.TestHelpers.Scenario;

namespace DDD.Sessionize.Tests.SessionizeSync
{
    public class AddDeleteAndUpdateScenario : Scenario
    {
        public async Task GivenEmptyReadModel()
        {
            _documentDbRepository = await EmptyDocumentDb.InitializeAsync<SessionOrPresenter>(TestDatabaseId, TestCollectionId);
        }

        public void AndGivenSessionizeHasPresentersAndSessions()
        {
            _sessionizeApiClient = SessionizeApiClientMock.Get(_ApiMocks.EmptyReadModelScenarioMock);
        }

        public async Task WhenPerformingSync()
        {
            await SyncService.Sync(_sessionizeApiClient, _documentDbRepository, _logger, _dateTimeProvider);
        }

        public void AndGivenSessionizeHasNewUpdatedAndDeletedPresentersAndSessions()
        {
            _sessionizeApiClient = SessionizeApiClientMock.Get(_ApiMocks.AddDeleteAndUpdateScenarioMock);
        }

        public async Task WhenPerformingSubsequentSync()
        {
            _dateTimeProvider = new StaticDateTimeProvider(_dateTimeProvider.Now().AddDays(1));
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

        [Fact]
        public override void Run()
        {
            this.Given(x => x.GivenEmptyReadModel())
                .And(x => x.AndGivenSessionizeHasPresentersAndSessions())
                .When(x => x.WhenPerformingSync())
                .Given(x => x.AndGivenSessionizeHasNewUpdatedAndDeletedPresentersAndSessions())
                .When(x => x.WhenPerformingSubsequentSync())
                .Then(x => x.ThenTheReadModelIsPopulated())
                .And(x => x.AndTheReadModelHasTheCorrectPresenters())
                .And(x => x.AndTheReadModelHasTheCorrectSessions())
                .And(x => x.AndTheLoggerOutputIsCorrect())
                .BDDfy(GetType().Name);
        }

        public AddDeleteAndUpdateScenario(ITestOutputHelper output)
        {
            _logger = new Xunit2Logger(output);
            Xunit2BddfyTextReporter.Instance.RegisterOutput(output);
        }

        private StaticDateTimeProvider _dateTimeProvider = new StaticDateTimeProvider(new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero));
        private DocumentDbRepository<SessionOrPresenter> _documentDbRepository;
        private ISessionizeApiClient _sessionizeApiClient;
        private readonly Xunit2Logger _logger;
        private SessionOrPresenter[] _readModel;
        private const string TestDatabaseId = "AddDeleteAndUpdateScenario";
        private const string TestCollectionId = "Sessions";
    }
}
