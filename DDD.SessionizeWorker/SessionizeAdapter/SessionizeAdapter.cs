using System;
using System.Collections.Generic;
using System.Linq;
using DDD.Core.Domain;
using DDD.SessionizeWorker.Sessionize;

namespace DDD.SessionizeWorker.SessionizeAdapter
{
    public class SessionizeAdapter
    {
        public Tuple<Session[], Presenter[]> Convert(SessionizeResponse sessionizeData)
        {
            var categories = GetCategories(sessionizeData);
            var presenters = GetPresenters(sessionizeData);
            var mobilePhoneQuestion = sessionizeData.Questions.Single(q => q.Question == MobileNumberQuestion);
            var sessions = GetSessions(sessionizeData, categories, presenters, mobilePhoneQuestion);

            return Tuple.Create(sessions, presenters);
        }

        private static Session[] GetSessions(SessionizeResponse sessionizeData, CategoryItem[] categories, Presenter[] presenters, SessionizeQuestion mobilePhoneQuestion)
        {
            return sessionizeData.Sessions.Select(s => new Session
            {
                Id = Guid.NewGuid(),
                ExternalId = s.Id,
                Title = s.Title,
                Abstract = s.Description,
                CreatedDate = DateTimeOffset.UtcNow,
                Format = s.CategoryItemIds.Where(cId =>
                        categories.Any(c => c.Type == CategoryType.SessionFormat && c.Id == cId))
                    .Select(cId => categories.First(c => c.Id == cId))
                    .Select(c => SessionFormatMap[c.Title])
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
                MobilePhoneContact = s.QuestionAnswers.Where(qa => qa.QuestionId == mobilePhoneQuestion.Id).Select(qa => qa.AnswerValue).FirstOrDefault()
            }).ToArray();
        }

        private static Presenter[] GetPresenters(SessionizeResponse sessionizeData)
        {
            return sessionizeData.Speakers.Select(s => new Presenter
            {
                Id = Guid.NewGuid(),
                ExternalId = s.Id,
                Name = s.FullName,
                //Email = s.Email,
                Tagline = s.TagLine,
                Bio = s.Bio,
                ProfilePhotoUrl = s.ProfilePictureUrl,
                WebsiteUrl = s.Links.Where(l => l.LinkType == BlogType).Select(l => l.Url).FirstOrDefault()
                             ?? s.Links.Where(l => l.LinkType == LinkedInType).Select(l => l.Url).FirstOrDefault(),
                TwitterHandle = s.Links.Where(l => l.LinkType == TwitterType).Select(l => l.Url.Replace("https://twitter.com/", "")).FirstOrDefault()
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
                        : c.Title == TagsTitle
                            ? CategoryType.Tags
                            : default(CategoryType?);

                if (!category.HasValue)
                    return null;

                return c.Items.Select(i => new CategoryItem
                {
                    Id = i.Id,
                    Title = i.Name,
                    Type = category.Value
                });
            }).Where(x => x != null).ToArray();
        }

        public const string SessionFormatTitle = "Session format";

        public static readonly Dictionary<string, SessionFormat> SessionFormatMap = new Dictionary<string, SessionFormat>
        {
            {SessionFormatLightningTalkTitle, SessionFormat.LightningTalk},
            {SessionFormatFullTalkTitle, SessionFormat.FullTalk},
            {SessionFormatWorkshopTitle, SessionFormat.Workshop},
        };

        public const string SessionFormatLightningTalkTitle = "20 mins (15 mins talking)";
        public const string SessionFormatFullTalkTitle = "45 mins (40 mins talking)";
        public const string SessionFormatWorkshopTitle = "Workshop";
        public const string LevelTitle = "Level";
        public const string TagsTitle = "Tags";

        public const string LinkedInType = "LinkedIn";
        public const string TwitterType = "Twitter";
        public const string BlogType = "Blog";

        public const string MobileNumberQuestion = "Mobile number";

        class CategoryItem
        {
            public CategoryType Type { get; set; }
            public int Id { get; set; }
            public string Title { get; set; }
        }

        enum CategoryType
        {
            SessionFormat,
            Tags,
            Level
        }
    }
}
