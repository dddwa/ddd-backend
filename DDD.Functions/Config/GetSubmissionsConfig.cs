using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Config
{
    public class GetSubmissionsConfig : Attribute
    {
        [AppSetting(Default = "ConnectionStrings:Sessions")]
        public string ConnectionString { get; set; }
        [AppSetting(Default = "SessionsDataSourceCosmosDatabaseId")]
        public string CosmosDatabaseId { get; set; }
        [AppSetting(Default = "SessionsDataSourceCosmosCollectionId")]
        public string CosmosCollectionId { get; set; }
        [AppSetting(Default = "AnonymousSessions")]
        public string AnonymousSessionsAppSetting { get; set; }
        public bool AnonymousSessions => AnonymousSessionsAppSetting != "false";
        [AppSetting(Default = "SubmissionsAvailableFrom")]
        public string SubmissionsAvailableFrom { get; set; }
        public DateTimeOffset SubmissionsAvailableFromDate => SubmissionsAvailableFrom != null ? DateTimeOffset.Parse(SubmissionsAvailableFrom) : DateTimeOffset.MaxValue;
        [AppSetting(Default = "SubmissionsAvailableTo")]
        public string SubmissionsAvailableTo { get; set; }
        public DateTimeOffset SubmissionsAvailableToDate => SubmissionsAvailableTo != null ? DateTimeOffset.Parse(SubmissionsAvailableTo) : DateTimeOffset.MinValue;
        public DateTimeOffset Now => DateTimeOffset.UtcNow;
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindGetSubmissionsConfigAttribute : GetSubmissionsConfig { }

    public class BindGetSubmissionsConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindGetSubmissionsConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private GetSubmissionsConfig BuildItemFromAttr(BindGetSubmissionsConfigAttribute attr)
        {
            return attr;
        }
    }
}
