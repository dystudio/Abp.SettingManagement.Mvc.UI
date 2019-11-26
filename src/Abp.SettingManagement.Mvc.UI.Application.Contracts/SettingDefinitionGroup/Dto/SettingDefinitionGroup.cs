﻿using System.Collections.Generic;
using Volo.Abp.Settings;

namespace Abp.SettingManagement.Mvc.UI.SettingDefinitionGroup.Dto
{
    public class SettingDefinitionGroup
    {
        public string GroupName { get; set; }
        public string GroupDisplayName { get; set; }
        public IEnumerable<SettingDefinition> SettingDefinitions { get; set; }
    }
}