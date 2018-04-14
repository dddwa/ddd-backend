using System.Linq;
using System.Threading.Tasks;
using DDD.Core.DocumentDb;
using DDD.Sessionize.Sessionize;
using DDD.Sessionize.Sync;
using DDD.Sessionize.Tests.TestHelpers;
using Shouldly;
using Shouldly.Core;
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
            _sessionizeApiClient = SessionizeApiClientMock.Get(ApiMocks.EmptyReadModelScenario);
        }

        public async Task WhenPerformingSync()
        {
            await SyncService.Sync(_sessionizeApiClient, _documentDbRepository, _logger);
        }

        public async Task ThenTheReadModelIsPopulated()
        {
            _readModel = (await _documentDbRepository.GetAllItemsAsync()).ToArray();
            _normalisedIdConverter = SessionOrPresenterAssertions.NormaliseIds(_readModel);
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
            Approve(_normalisedIdConverter.Convert(_logger));
        }

        public EmptyReadModelScenario(ITestOutputHelper output)
        {
            _logger = new Xunit2Logger(output);
            Xunit2BddfyTextReporter.Instance.RegisterOutput(output);
        }

        private DocumentDbRepository<SessionOrPresenter> _documentDbRepository;
        private ISessionizeApiClient _sessionizeApiClient;
        private readonly Xunit2Logger _logger;
        private SessionOrPresenter[] _readModel;
        private IdConverter _normalisedIdConverter;
        private const string TestDatabaseId = "ConferenceTest";
        private const string TestCollectionId = "Sessions";
    }
}
