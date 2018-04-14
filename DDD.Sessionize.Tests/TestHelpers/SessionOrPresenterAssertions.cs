using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DDD.Sessionize.Tests.TestHelpers
{
    static class SessionOrPresenterAssertions
    {
        public static string PrepareSessionsForApproval(SessionOrPresenter[] items)
        {
            var sessions = items.Select(x => x.Session).Where(x => x != null).OrderBy(x => x.ExternalId).ToArray();
            return JsonConvert.SerializeObject(sessions, Formatting.Indented, new StringEnumConverter());
        }

        public static string PreparePresentersForApproval(SessionOrPresenter[] items)
        {
            var presenters = items.Select(x => x.Presenter).Where(x => x != null).OrderBy(x => x.ExternalId).ToArray();
            return JsonConvert.SerializeObject(presenters, Formatting.Indented, new StringEnumConverter());
        }
    }
}
