﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.SettingManagement.Mvc.UI.Authorization;
using Abp.SettingManagement.Mvc.UI.Extensions;
using Abp.SettingManagement.Mvc.UI.Localization;
using Abp.SettingManagement.Mvc.UI.SettingManagementUI.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Json;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;
using Volo.Abp.VirtualFileSystem;

namespace Abp.SettingManagement.Mvc.UI.SettingManagementUI
{
    public class SettingManagementUIAppService : ApplicationService, ISettingManagementUIAppService
    {
        private readonly IStringLocalizer<AbpSettingManagementMvcUIResource> _localizer;
        private readonly IVirtualFileProvider _fileProvider;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ISettingDefinitionManager _settingDefinitionManager;
        private readonly ISettingManager _settingManager;

        public SettingManagementUIAppService(IStringLocalizer<AbpSettingManagementMvcUIResource> localizer, IVirtualFileProvider fileProvider, IJsonSerializer jsonSerializer, ISettingDefinitionManager settingDefinitionManager, ISettingManager settingManager)
        {
            _localizer = localizer;
            _fileProvider = fileProvider;
            _jsonSerializer = jsonSerializer;
            _settingDefinitionManager = settingDefinitionManager;
            _settingManager = settingManager;
        }

        public async Task<IEnumerable<SettingGroup>> GroupSettingDefinitions()
        {
            if (!(await AuthorizationService.IsGrantedAsync(AbpSettingManagementMvcUIPermissions.Global) ||
                await AuthorizationService.IsGrantedAsync(AbpSettingManagementMvcUIPermissions.Tenant) ||
                await AuthorizationService.IsGrantedAsync(AbpSettingManagementMvcUIPermissions.User)))
            {
                throw new AbpAuthorizationException("Authorization failed! No SettingManagementUI policy granted.");
            }

            // Merge all the setting properties in to one dictionary
            var settingProperties = GetMergedSettingProperties();

            // Set the properties of the setting definitions
            var settingDefinitions = SetSettingDefinitionProperties(settingProperties);

            // Group the setting definitions
            return settingDefinitions
                .GroupBy(sd => sd.Properties[AbpSettingManagementMvcUIConst.Group1].ToString())
                .Select(grp => new SettingGroup
                {
                    GroupName = grp.Key,
                    GroupDisplayName = _localizer[grp.Key],
                    SettingDefinitions = grp
                })
                ;
        }

        public async Task SetSettingValues(IDictionary<string, string> settingValues)
        {
            foreach (var kv in settingValues)
            {
                // The key of the settingValues is in camelCase, like "setting_Abp_Localization_DefaultLanguage",
                // change it to "Abp.Localization.DefaultLanguage" form
                string pascalCaseName = kv.Key.ToPascalCase();
                if (!pascalCaseName.StartsWith(AbpSettingManagementMvcUIConst.FormNamePrefix))
                {
                    continue;
                }

                string name = pascalCaseName.RemovePreFix(AbpSettingManagementMvcUIConst.FormNamePrefix).UnderscoreToDot();
                // TODO: support setting providers
                await _settingManager.SetGlobalAsync(name, kv.Value);
            }
        }

        private IDictionary<string, IDictionary<string, string>> GetMergedSettingProperties()
        {
            return _fileProvider
                .GetDirectoryContents(AbpSettingManagementMvcUIConst.SettingPropertiesFileFolder)
                .Select(content =>
                    _jsonSerializer.Deserialize<IDictionary<string, IDictionary<string, string>>>(content.ReadAsString()))
                .SelectMany(dict => dict)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private IEnumerable<SettingDefinition> SetSettingDefinitionProperties(IDictionary<string, IDictionary<string, string>> settingProperties)
        {
            var settingDefinitions = _settingDefinitionManager.GetAll();
            foreach (var settingDefinition in settingDefinitions)
            {
                if (settingProperties.ContainsKey(settingDefinition.Name))
                {
                    // This SettingDefinition is defined in the property file,
                    // set its property values from the dictionary
                    var properties = settingProperties[settingDefinition.Name];
                    foreach (var kv in properties)
                    {
                        settingDefinition.WithProperty(kv.Key, kv.Value);
                    }
                }

                // Default group1: Others
                if (!settingDefinition.Properties.ContainsKey(AbpSettingManagementMvcUIConst.Group1))
                {
                    settingDefinition.WithProperty(AbpSettingManagementMvcUIConst.Group1,
                        AbpSettingManagementMvcUIConst.DefaultGroup);
                }

                // Default group2: Others
                if (!settingDefinition.Properties.ContainsKey(AbpSettingManagementMvcUIConst.Group2))
                {
                    settingDefinition.WithProperty(AbpSettingManagementMvcUIConst.Group2,
                        AbpSettingManagementMvcUIConst.DefaultGroup);
                }

                // Default type: text
                if (!settingDefinition.Properties.ContainsKey(AbpSettingManagementMvcUIConst.Type))
                {
                    settingDefinition.WithProperty(AbpSettingManagementMvcUIConst.Type,
                        AbpSettingManagementMvcUIConst.DefaultType);
                }
            }

            return settingDefinitions;
        }
    }
}