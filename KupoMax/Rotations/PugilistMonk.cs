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
    public class PugilistMonk : KupoRoutine
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
            get { return new ClassJobType[] {ClassJobType.Pugilist, ClassJobType.Monk,}; }
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
                new Decorator(ctx => ctx != null,new PrioritySelector(
                    CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                    CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, ctx => Core.Player.CombatReach + PullRange + (ctx as GameObject).CombatReach, true, "Moving to unit"),
                    Spell.PullCast(r=>"Snap Punch", r => Core.Player.HasAura("Coeurl Form")),
                    Spell.PullCast(r=>"True Strike", r => Core.Player.HasAura("Raptor Form")),
                    Spell.PullApply("Touch of Death"),
                    Spell.PullCast("Bootshine")
            )));
        }

        [Behavior(BehaviorType.Combat)]
        public Composite CreateBasicCombat()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null,
                    new PrioritySelector(
                    CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                    CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, ctx => Core.Player.CombatReach + PullRange + (ctx as GameObject).CombatReach, true, "Moving to unit"),
                    // CROSS CLASS ARCHER SKILLS
                    Spell.Apply("Hawk's Eye", on => Core.Player),
                    Spell.Apply("Raging Strikes", on => Core.Player),
                    Spell.Apply("Straight Shot", req => !Core.Player.HasAura("Straight Shot",true,3000)),
                    Spell.Apply(r => "Venomous Bite", msLeft:3000),
                    // PUGILIST SKILLS
                    Spell.Apply("Second Wind", req => (Core.Player.CurrentHealthPercent <= 70), on => Core.Player),
                    Spell.Apply("Featherfoot", req => (Core.Player.CurrentHealthPercent <= 40), on => Core.Player),
                    Spell.Apply("Internal Release", on => Core.Player),
                    Spell.Cast("Haymaker"),
                    Spell.Apply("Touch of Death"),
                    Spell.Cast("Snap Punch", r => Core.Player.HasAura("Coeurl Form")),
                    Spell.Cast("True Strike", r => Core.Player.HasAura("Raptor Form")),
                    Spell.Cast("Bootshine", r => true)
            )));
        }
    }
}