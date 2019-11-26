﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.SettingManagement.Mvc.UI.Localization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using Volo.Abp.Application.Services;
using Volo.Abp.Json;
using Volo.Abp.Settings;
using Volo.Abp.VirtualFileSystem;

namespace Abp.SettingManagement.Mvc.UI.SettingDefinitionGroup
{
    public class SettingDefinitionGroupAppService : ApplicationService, ISettingDefinitionGroupAppService
    {
        private readonly IStringLocalizer<AbpSettingManagementMvcUIResource> _localizer;
        private readonly IVirtualFileProvider _fileProvider;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ISettingDefinitionManager _settingDefinitionManager;

        public SettingDefinitionGroupAppService(IStringLocalizer<AbpSettingManagementMvcUIResource> localizer, IVirtualFileProvider fileProvider, IJsonSerializer jsonSerializer, ISettingDefinitionManager settingDefinitionManager)
        {
            _localizer = localizer;
            _fileProvider = fileProvider;
            _jsonSerializer = jsonSerializer;
            _settingDefinitionManager = settingDefinitionManager;
        }

        public IEnumerable<Dto.SettingDefinitionGroup> GroupSettingDefinitions()
        {
            // Merge all the setting properties in to one dictionary
            var settingProperties = GetMergedSettingProperties();

            // Set the properties of the setting definitions
            var settingDefinitions = SetSettingDefinitionProperties(settingProperties);

            // Group the setting definitions
            return settingDefinitions
                .GroupBy(sd => sd.Properties[AbpSettingManagementMvcUIConst.Group1].ToString())
                .Select(grp => new Dto.SettingDefinitionGroup
                {
                    GroupName = grp.Key,
                    GroupDisplayName = _localizer[grp.Key],
                    SettingDefinitions = grp
                })
                ;
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
                else
                {
                    // No define, we set the the default values of Group1 and Group2 
                    settingDefinition
                        .WithProperty(AbpSettingManagementMvcUIConst.Group1, AbpSettingManagementMvcUIConst.DefaultGroup)
                        .WithProperty(AbpSettingManagementMvcUIConst.Group2, AbpSettingManagementMvcUIConst.DefaultGroup);
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