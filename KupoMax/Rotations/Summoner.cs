﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using Kupo;
using Kupo.Helpers;
using Kupo.Settings;
using Pathfinding;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Kupo.Rotations
{
    internal class Summoner : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override void OnInitialize()
        {
            WindowSettings = new SummonerSettings();
        }


        private SummonerSettings settings
        {
            get { return WindowSettings as SummonerSettings; }
        }

        private string WhichPet
        {
            get
            {
                if (settings.PetKind == PetTypes.Caster)
                    return "Summon";

                if (settings.PetKind == PetTypes.Tank)
                    return "Summon II";

                if (settings.PetKind == PetTypes.Attacker)
                    return "Summon III";

                return "Summon";
            }
        }


        private string[] PullSpells = new[] {"Bio II", "Miasma", "Bio"};
        private string _BestPullSpell;

        private string BestPullSpell
        {
            get
            {
                if (string.IsNullOrEmpty(_BestPullSpell))
                {
                    foreach (var spell in PullSpells)
                    {
                        if (Actionmanager.HasSpell(spell))
                        {
                            _BestPullSpell = spell;
                            return spell;
                        }
                    }
                    _BestPullSpell = "Ruin";
                    return "Ruin";
                }
                else
                {
                    return _BestPullSpell;
                }
            }
        }


        public override float PullRange
        {
            get { return 20; }
        }

        public override ClassJobType[] Class
        {
            get { return new[] {ClassJobType.Summoner}; }
        }


        [Behavior(BehaviorType.PreCombatBuffs)]
        public Composite CreateBasicPreCombatBuffs()
        {
            return SummonChocobo();
        }

        [Behavior(BehaviorType.Rest)]
        public Composite CreateBasicRest()
        {
            return new PrioritySelector(
                new Decorator(r => Core.Me.SpellCastInfo != null && Core.Me.SpellCastInfo.Name == "Physick" && !Resting, new Action(r => Actionmanager.StopCasting())), 
                Spell.Apply("Aetherflow", r => Core.Player.CurrentManaPercent < 65 || (Resting && Core.Player.CurrentManaPercent < settings.RestEnergyDone), r => Core.Player), 
                Spell.Cast("Physick", r => Resting && Core.Player.CurrentHealthPercent <= settings.RestHealthDone, r => Core.Player), 
                DefaultRestBehavior(r => Core.Player.CurrentManaPercent));
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                Spell.Apply("Aetherflow", r => Core.Player.CurrentManaPercent < 65, r => Core.Player), 
                Spell.Cast("Physick", r => Core.Player.CurrentHealthPercent <= 40, r => Core.Player), 
                Spell.Apply("Sustain", r => Core.Player.Pet != null && Core.Player.Pet.CurrentHealthPercent <= settings.SustainPet, r => Core.Player.Pet),
                Spell.Cast("Physick", r => Core.Player.Pet != null && Core.Player.Pet.CurrentHealthPercent <= settings.HealPet, r => Core.Player.Pet)
                
                
                );
        }

        [Behavior(BehaviorType.Pull)]
        public Composite CreateBasicPull()
        {
             return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null,new PrioritySelector(
                EnsureTarget, //Stop double casting due to delay
                new Decorator(r => Core.Me.SpellCastInfo != null && Core.Me.SpellCastInfo.SpellData.Name.Equals(WhichPet) && Core.Player.Pet != null, new Action(r => Actionmanager.StopCasting())), 

                Spell.Cast(r => WhichPet, r => Core.Player.Pet == null && Actionmanager.HasSpell(WhichPet), r => Core.Player),


                 CommonBehaviors.MoveToLos(ctx => ctx as BattleCharacter),
                 CommonBehaviors.MoveAndStop(ctx => (ctx as BattleCharacter).Location, PullRange, true, "Moving to unit"),

                Spell.PullApply(r => BestPullSpell)
                )));
        }

        [Behavior(BehaviorType.Combat)]
        public Composite CreateBasicCombat()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter, 
                new Decorator(ctx => ctx != null, 
                    new PrioritySelector(
                        CommonBehaviors.MoveToLos(ctx => ctx as GameObject), 
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit"),
                        new Decorator(r => !Core.Player.IsCasting, CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit")), 
                        Spell.Cast(r => WhichPet, r => Core.Player.Pet == null && Actionmanager.HasSpell(WhichPet), r => Core.Player), 
                        new Decorator(r => Core.Me.SpellCastInfo != null && Core.Me.SpellCastInfo.SpellData.Name.Equals(WhichPet) && Core.Player.Pet != null, new Action(r => Actionmanager.StopCasting())), 
                        Spell.Apply("Bio II"), 
                        Spell.Apply("Miasma"), 
                        Spell.Apply("Bio"), 
                        Spell.Cast("Energy Drain", r => Core.Player.HasAura("Aetherflow") && Core.Player.CurrentManaPercent < WindowSettings.RestEnergyDone), 
                        Spell.Cast("Ruin", r => true))));
        }
    }
}