using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using Kupo.Helpers;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Kupo.Rotations
{
    public class GladiatorPaladin : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override float PullRange
        {
            get { return 2.5f; }
        }

        public override ClassJobType[] Class
        {
            get { return new ClassJobType[] {ClassJobType.Gladiator, ClassJobType.Paladin,}; }
        }

        [Behavior(BehaviorType.PreCombatBuffs)]
        public Composite CreateBasicPreCombatBuffs()
        {
            return SummonChocobo();
        }

        [Behavior(BehaviorType.Rest)]
        public Composite CreateBasicRest()
        {
            return DefaultRestBehavior(r => Core.Player.CurrentTPPercent);
        }

        [Behavior(BehaviorType.Pull)]
        public Composite CreateBasicPull()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null,
                    new PrioritySelector(
                        new Decorator(req => WindowSettings.AllowMovement,
                            new PrioritySelector(
                                CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                                CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, ctx => Core.Player.CombatReach + PullRange + (ctx as GameObject).CombatReach, true, "Moving to unit")
                        )),
                        Spell.PullCast("Fast Blade")
            )));
        }

        [Behavior(BehaviorType.Combat)]
        public Composite CreateBasicCombat()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null,
                    new PrioritySelector(
                        new Decorator(req => WindowSettings.AllowMovement,
                            new PrioritySelector(
                                CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                                CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, ctx => Core.Player.CombatReach + PullRange + (ctx as GameObject).CombatReach, true, "Moving to unit")
                        )),
                        Spell.Apply("Rampart", req => Core.Player.CurrentHealthPercent <= 33, on => Core.Player),
                        Spell.Apply("Convalescence", req => Core.Player.CurrentHealthPercent <= 66, on => Core.Player),
                        Spell.Apply("Flash", req => UnaggrodEnemies(Core.Player.Location,5f).Count() > 0, on => Core.Player),
                        Spell.Apply("Fight or Flight", on => Core.Player),
                        Spell.Cast("Riot Blade", r => Core.Player.CurrentManaPercent <= 50 && Actionmanager.LastSpell.Name == "Fast Blade"),
                        Spell.Cast("Savage Blade", r => Actionmanager.LastSpell.Name == "Fast Blade"),
                        Spell.Cast("Fast Blade")
                        // r => Actionmanager.LastSpellId == 0 || Actionmanager.LastSpell.Name == "Full Thrust" )
            )));
        }
    }
}