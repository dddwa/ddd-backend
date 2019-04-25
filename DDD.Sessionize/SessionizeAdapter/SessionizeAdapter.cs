using System;
using System.Linq;
using DDD.Core.Domain;
using DDD.Core.Time;
using DDD.Functions.Config;
using DDD.Sessionize.Sessionize;

namespace DDD.Sessionize.SessionizeAdapter
{
    public static class SessionizeAdapter
    {
        public static Tuple<Session[], Presenter[]> Convert(SessionizeResponse sessionizeData, IDateTimeProvider dateTimeProvider)
        {
            var categories = GetCategories(sessionizeData);
            var presenters = GetPresenters(sessionizeData, categories, dateTimeProvider);
            var sessions = GetSessions(sessionizeData, categories, presenters, dateTimeProvider);

            return Tuple.Create(sessions, presenters);
        }

        private static Session[] GetSessions(SessionizeResponse sessionizeData, CategoryItem[] categories, Presenter[] presenters, IDateTimeProvider dateTimeProvider)
        {
            return sessionizeData.Sessions.Select(s => new Session
            {
                Id = SessionIds2018.ExternalIdToSessionId.ContainsKey(s.Id) ? Guid.Parse(SessionIds2018.ExternalIdToSessionId[s.Id]) : Guid.NewGuid(),
                ExternalId = s.Id,
                Title = s.Title,
                Abstract = s.Description,
                CreatedDate = dateTimeProvider.Now(),
                Format = s.CategoryItemIds.Where(cId =>
                        categories.Any(c => c.Type == CategoryType.SessionFormat && c.Id == cId))
                    .Select(cId => categories.First(c => c.Id == cId))
                    .Select(c => c.Title)
                    .FirstOrDefault(),
                Level = s.CategoryItemIds
                    .Where(cId => categories.Any(c => c.Type == CategoryType.Level && c.Id == cId))
                    .Select(cId => categories.First(c => c.Id == cId))
                    .Select(c => c.Title)
                    .FirstOrDefault(),
                Tags = s.CategoryItemIds
                    .Where(cId => categories.Any(c => c.Type == CategoryType.Tags && c.Id == cId))
                    .Select(cId => categories.First(c => c.Id == cId))
                    .Select(c => c.Title)
                    .ToArray(),
                PresenterIds = s.SpeakerIds.Select(sId => presenters.Single(p => p.ExternalId == sId)).Select(p => p.Id).ToArray(),
                DataFields = s.QuestionAnswers.Select(qa => new {q = sessionizeData.Questions.Single(q => q.Id == qa.QuestionId).Question, a = qa.AnswerValue})
                    .Concat(s.CategoryItemIds
                        .Where(cId => categories.Any(c => c.Type == CategoryType.Other && c.Id == cId))
                        .Select(cId => categories.First(c => c.Id == cId))
                        .Select(c => new {q = c.TypeText, a = c.Title})
                    )
                    .ToDictionary(x => x.q, x => x.a),
            }).ToArray();
        }

        private static Presenter[] GetPresenters(SessionizeResponse sessionizeData, CategoryItem[] categories, IDateTimeProvider dateTimeProvider)
        {
            return sessionizeData.Speakers.Select(s => new Presenter
            {
                Id = Guid.NewGuid(),
                ExternalId = s.Id,
                Name = s.FullName,
                CreatedDate = dateTimeProvider.Now(),
                //Email = s.Email,
                Tagline = s.TagLine,
                Bio = s.Bio,
                ProfilePhotoUrl = s.ProfilePictureUrl,
                WebsiteUrl = s.Links.Where(l => l.LinkType == BlogType).Select(l => l.Url).FirstOrDefault()
                             ?? s.Links.Where(l => l.LinkType == LinkedInType).Select(l => l.Url).FirstOrDefault(),
                TwitterHandle = s.Links.Where(l => l.LinkType == TwitterType).Select(l => l.Url.Replace("https://twitter.com/", "")).FirstOrDefault(),
                DataFields = (s.QuestionAnswers ?? new SessionizeQuestionAnswer[]{}).Select(qa => new { q = sessionizeData.Questions.Single(q => q.Id == qa.QuestionId).Question, a = qa.AnswerValue })
                    .Concat((s.CategoryItemIds ?? new int[]{})
                        .Select(cId => categories.First(c => c.Id == cId))
                        .Select(c => new { q = c.TypeText, a = c.Title })
                    )
                    .ToDictionary(x => x.q, x => x.a),
            }).ToArray();
        }

        private static CategoryItem[] GetCategories(SessionizeResponse sessionizeData)
        {
            return sessionizeData.Categories.SelectMany(c =>
            {
                var category = c.Title == SessionFormatTitle
                    ? CategoryType.SessionFormat
                    : c.Title == LevelTitle
                        ? CategoryType.Level
                        : TagsTitles.Contains(c.Title)
                            ? CategoryType.Tags
                            : CategoryType.Other;

                return c.Items.Select(i => new CategoryItem
                {
                    Id = i.Id,
                    Title = i.Name,
                    Type = category,
                    TypeText = c.Title
                });
            }).ToArray();
        }

        public const string SessionFormatTitle = "Session format";
        public const string LevelTitle = "Level";
        public static readonly string[] TagsTitles = new[] {"Tags", "General Topic Category", "Primary Topic", "Secondary Topic"};

        public const string LinkedInType = "LinkedIn";
        public const string TwitterType = "Twitter";
        public const string BlogType = "Blog";

        class CategoryItem
        {
            public CategoryType Type { get; set; }
            public string TypeText { get; set; }
            public int Id { get; set; }
            public string Title { get; set; }
        }

        enum CategoryType
        {
            SessionFormat,
            Tags,
            Level,
            Other
        }
    }
}
