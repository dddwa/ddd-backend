using System;
using System.Linq;
using System.Threading.Tasks;
using DDD.Core.AzureStorage;
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
    public class UpdateWithRemovedDataScenario : Scenario
    {
        public void GivenEmptyReadModel()
        {
            _sessionRepository = new TableStorageRepositoryMock<SessionEntity>();
            _presenterRepository = new TableStorageRepositoryMock<PresenterEntity>();
        }

        public void AndGivenSessionizeHasPresentersAndSessions()
        {
            _sessionizeApiClient = SessionizeApiClientMock.Get(
                GetResource("EmptyReadModelScenarioMock.json"));
        }

        public async Task WhenPerformingSync()
        {
            await SyncService.Sync(_sessionizeApiClient, _sessionRepository, _presenterRepository,  _logger, _dateTimeProvider, "2018");
        }

        public void AndGivenSessionizeHasAnUpdateThatRemovesADataField()
        {
            _sessionizeApiClient = SessionizeApiClientMock.Get(
                GetResource("UpdateWithRemovedDataScenarioMock.json"));
        }

        public async Task WhenPerformingSubsequentSync()
        {
            _dateTimeProvider = new StaticDateTimeProvider(_dateTimeProvider.Now().AddDays(1));
            await SyncService.Sync(_sessionizeApiClient, _sessionRepository, _presenterRepository, _logger, _dateTimeProvider, "2018");
        }

        public async Task ThenTheReadModelIsPopulated()
        {
            _readModel = (await _sessionRepository.GetAllAsync("2018")).ToArray();
            _readModel.ShouldNotBeEmpty();
        }

        public async Task AndTheReadModelHasTheCorrectPresenters()
        {
            Approve(SessionOrPresenterAssertions.PreparePresentersForApproval((await _presenterRepository.GetAllAsync("2018")).ToArray()), "json");
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
                .Given(x => x.AndGivenSessionizeHasAnUpdateThatRemovesADataField())
                .When(x => x.WhenPerformingSubsequentSync())
                .Then(x => x.ThenTheReadModelIsPopulated())
                .And(x => x.AndTheReadModelHasTheCorrectPresenters())
                .And(x => x.AndTheReadModelHasTheCorrectSessions())
                .And(x => x.AndTheLoggerOutputIsCorrect())
                .BDDfy(GetType().Name);
        }

        public UpdateWithRemovedDataScenario(ITestOutputHelper output)
        {
            _logger = new Xunit2Logger(output);
            Xunit2BddfyTextReporter.Instance.RegisterOutput(output);
        }

        private StaticDateTimeProvider _dateTimeProvider = new StaticDateTimeProvider(new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero));
        private TableStorageRepositoryMock<SessionEntity> _sessionRepository;
        private TableStorageRepositoryMock<PresenterEntity> _presenterRepository;
        private ISessionizeApiClient _sessionizeApiClient;
        private readonly Xunit2Logger _logger;
        private SessionEntity[] _readModel;
    }
}
