﻿using Volo.Abp.Modularity;
using Volo.Abp.Localization;
using Abp.SettingManagement.Mvc.UI.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Localization.Resources.AbpValidation;
using Volo.Abp.VirtualFileSystem;

namespace Abp.SettingManagement.Mvc.UI
{
    [DependsOn(
        typeof(AbpLocalizationModule)
    )]
    public class AbpSettingManagementMvcUIDomainSharedModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.AddEmbedded<AbpSettingManagementMvcUIDomainSharedModule>("Abp.SettingManagement.Mvc.UI");
            });

            Configure<AbpLocalizationOptions>(options =>
            {
                options.Resources
                    .Add<AbpSettingManagementMvcUIResource>("en")
                    .AddBaseTypes(typeof(AbpValidationResource))
                    .AddVirtualJson("/Localization/AbpSettingManagementMvcUI");
            });

            Configure<AbpExceptionLocalizationOptions>(options =>
            {
                options.MapCodeNamespace("AbpSettingManagementMvcUI", typeof(AbpSettingManagementMvcUIResource));
            });
        }
    }
}
