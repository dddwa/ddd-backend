using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DDD.Domain
{
    public class Session
    {
        public Guid Id { get; set; }
        public string ExternalId { get; set; }
        public string Title { get; set; }
        public string Abstract { get; set; }
        public List<Presenter> Presenters { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public SessionFormat Format { get; set; }
        public string Level { get; set; }
        public string[] Tags { get; set; }
    }

    public enum SessionFormat
    {
        [Description("20 mins (15 mins talking)")]
        LightningTalk,
        [Description("45 mins (40 mins talking)")]
        FullTalk,
        [Description("Workshop")]
        Workshop
    }
}