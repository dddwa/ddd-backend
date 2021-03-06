﻿using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;

namespace DDD.Functions.Extensions
{
    public class SubmissionsConfig : Attribute
    {
        [AppSetting(Default = "SessionsConnectionString")]
        public string ConnectionString { get; set; }
        [AppSetting(Default = "SubmissionsTable")]
        public string SubmissionsTable { get; set; }
        [AppSetting(Default = "SubmittersTable")]
        public string SubmittersTable { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class BindSubmissionsConfigAttribute : SubmissionsConfig { }

    public class BindSubmissionsConfigExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<BindSubmissionsConfigAttribute>();

            rule.BindToInput(BuildItemFromAttr);
        }

        private SubmissionsConfig BuildItemFromAttr(BindSubmissionsConfigAttribute attr)
        {
            return attr;
        }
    }
}
