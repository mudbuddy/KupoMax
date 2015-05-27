﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using ff14bot.Helpers;

namespace Kupo.Settings
{
    internal enum PetTypes
    {
        Caster,
        Tank,
        Attacker
    }


    internal class ArcanistSettings : KupoSettings
    {
        public ArcanistSettings(string filename = "Arcanist-KupoSettings"): base(filename)
        {
        }

        [Setting]
        [DefaultValue(PetTypes.Caster)]
        [Category("ARC/SUM")]
        public PetTypes PetKind { get; set; }

        [Setting]
        [UpdateDefaultValue(60.0f,30.0f)]
        [DefaultValue(30.0f)]
        [Category("ARC/SUM - Pet Healing")]
        public float HealPet { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        [Category("ARC/SUM - Pet Healing")]
        public float SustainPet { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        [Category("ARC/SUM")]
        public float AetherflowPercent { get; set; }

        [Setting]
        [DefaultValue(66f)]
        [Category("ARC/SUM")]
        [DisplayName("DOT Min Duration %")]
        public float DOTMinTickDurationPercent { get; set; }

    }

    internal class SummonerSettings : ArcanistSettings
    {
        public SummonerSettings(): base("Summoner-KupoSettings")
        {
        }
    }

}