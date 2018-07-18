using System.Linq;
using DDD.Core.AzureStorage;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DDD.Sessionize.Tests.TestHelpers
{
    static class SessionOrPresenterAssertions
    {
        public static string PrepareSessionsForApproval(SessionEntity[] items)
        {
            var sessions = items.Select(x => x.GetSession()).Where(x => x != null).OrderBy(x => x.ExternalId).ToArray();
            return JsonConvert.SerializeObject(sessions, Formatting.Indented, new StringEnumConverter());
        }

        public static string PreparePresentersForApproval(PresenterEntity[] items)
        {
            var presenters = items.Select(x => x.GetPresenter()).Where(x => x != null).OrderBy(x => x.ExternalId).ToArray();
            return JsonConvert.SerializeObject(presenters, Formatting.Indented, new StringEnumConverter());
        }
    }
}
