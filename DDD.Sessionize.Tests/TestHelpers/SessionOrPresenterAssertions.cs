using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DDD.Sessionize.Tests.TestHelpers
{
    static class SessionOrPresenterAssertions
    {
        public static string PrepareSessionsForApproval(SessionOrPresenter[] items)
        {
            var sessions = items.Select(x => x.Session).Where(x => x != null).OrderBy(x => x.Id).ToArray();
            return JsonConvert.SerializeObject(sessions, Formatting.Indented, new StringEnumConverter());
        }

        public static string PreparePresentersForApproval(SessionOrPresenter[] items)
        {
            var presenters = items.Select(x => x.Presenter).Where(x => x != null).OrderBy(x => x.Id).ToArray();
            return JsonConvert.SerializeObject(presenters, Formatting.Indented, new StringEnumConverter());
        }

        public static IdConverter NormaliseIds(SessionOrPresenter[] items)
        {
            var orderedItems = items
                .OrderBy(x => x.Session != null)
                .ThenBy(x => x.Session != null ? x.Session.ExternalId : x.Presenter.ExternalId)
                .ToArray();
            var originalGuids = orderedItems.Select(x => x.GetId()).ToArray();
            var i = 0;
            foreach (var x in orderedItems)
            {
                if (items[i].Session != null)
                    items[i].Session.Id = ToGuid(i);
                if (items[i].Presenter != null)
                    items[i].Presenter.Id = ToGuid(i);
                i++;
            }
            var newGuids = orderedItems.Select(x => x.GetId()).ToArray();
            var idConverter = originalGuids.Zip(newGuids, (k, v) => new { k, v })
                .ToDictionary(x => x.k, x => x.v);

            foreach (var x in orderedItems.Where(x => x.Session != null))
            {
                x.Session.PresenterIds = x.Session.PresenterIds.Select(id => idConverter[id]).ToArray();
            }

            return new IdConverter { Converter = idConverter };
        }

        public static Guid ToGuid(int value)
        {
            var bytes = new byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes, 0);
            Array.Reverse(bytes, 0, bytes.Length);
            return new Guid(bytes);
        }
    }

    public class IdConverter
    {
        public Dictionary<Guid, Guid> Converter { get; set; }

        public string Convert(ILogger logger)
        {
            var logOutput = logger.ToString();
            foreach (var originalId in Converter.Keys)
            {
                logOutput = logOutput.Replace(originalId.ToString(), Converter[originalId].ToString());
            }

            return logOutput;
        }
    }
}
