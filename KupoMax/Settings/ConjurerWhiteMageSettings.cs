using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using ff14bot.Helpers;

namespace Kupo.Settings
{
    internal class ConjurerWhiteMageSettings : KupoSettings
    {
        public ConjurerWhiteMageSettings(string filename = "ConjurerWhiteMage-KupoSettings") : base(filename) { }

        [Setting]
        [DefaultValue(88)]
        [Category("WHM/CNJ - Regen")]
        [DisplayName("Health % Below")]
        public int RegenPercent { get; set; }

        [Setting]
        [DefaultValue(44)]
        [Category("WHM/CNJ - Cure II")]
        [DisplayName("Health Below % (Party)")]
        public int Cure2HealPercent { get; set; }

        [Setting]
        [DefaultValue(77)]
        [Category("WHM/CNJ - Cure")]
        [DisplayName("Health Below % (Party)")]
        public int CurePartyPercent { get; set; }

        [Setting]
        [DefaultValue(55)]
        [Category("WHM/CNJ - Cure")]
        [DisplayName("Health Below % (Solo)")]
        public int CureSoloPercent { get; set; }

        [Setting]
        [DefaultValue(3)]
        [Category("WHM/CNJ - Medica")]
        [DisplayName("# Party Members")]
        public int MedicaCount { get; set; }

        [Setting]
        [DefaultValue(88)]
        [Category("WHM/CNJ - Medica")]
        [DisplayName("Health Below %")]
        public int MedicaPercent { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("WHM/CNJ - Misc")]
        [DisplayName("Cleric Stance")]
        public bool UseClericStance { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("WHM/CNJ - Misc")]
        [DisplayName("Fluid Aura")]
        public bool UseFluidAura { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("WHM/CNJ - Misc")]
        [DisplayName("Repose Attackers")]
        public bool ReposeAttackers { get; set; }

        [Setting]
        [DefaultValue(33)]
        [Category("WHM/CNJ - Misc")]
        [DisplayName("Save Mana %")]
        public int SaveManaPercent { get; set; }
    }
}