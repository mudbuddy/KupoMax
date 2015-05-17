using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using ff14bot.Helpers;

namespace Kupo.Settings
{
    internal class ArcherBardSettings : KupoSettings
    {
        public ArcherBardSettings(string filename = "ArcherBard-KupoSettings") : base(filename) { }

        [Setting]
        [DefaultValue(true)]
        public bool ExecuteAnyTarget { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool SpreadDOTS { get; set; }
    }
}