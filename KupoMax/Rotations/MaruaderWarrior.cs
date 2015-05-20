using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class MaruaderWarrior : KupoRoutine
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
            get { return new [] {ClassJobType.Marauder, ClassJobType.Warrior,}; }
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
                    Spell.PullCast("Heavy Swing")
            )));
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                // CROSS CLASS SKILLS
                Spell.Apply("Second Wind", r => Core.Player.CurrentHealthPercent <= 66, on => Core.Player),
                // MAIN CLASS SKILLS
                Spell.Apply("Foresight", r => Core.Player.CurrentHealthPercent <= 70,r=>Core.Player),
                Spell.Apply("Bloodbath", r => Core.Player.CurrentHealthPercent <= 50, r => Core.Player),
                Spell.Cast("Inner Beast", r => Core.Player.CurrentHealthPercent <= 60 && Core.Player.HasAura("Infuriated"), r => Core.Player),
                Spell.Cast("Convalescence", r => Core.Player.CurrentHealthPercent <= 50, r => Core.Player),
                Spell.Apply("Thrill of Battle", r => Core.Player.CurrentHealthPercent <= 30, r => Core.Player)
            );
        }


        [Behavior(BehaviorType.PreCombatBuffs,GameContext.PvP)]
        public Composite CreatePvPPreCombatBuff()
        {
            return new PrioritySelector(
                Spell.Apply("Defiance",  r => Core.Player)
            );
        }

        [Behavior(BehaviorType.Combat,GameContext.PvP)]
        public Composite CreatePvPCombat()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null,
                    new PrioritySelector(
                        CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, false, "Moving to unit"),
                        Spell.Apply("Storm's Eye", r => Actionmanager.LastSpell.Name == "Maim"),
                        Spell.Apply("Storm's Path", r => Actionmanager.LastSpell.Name == "Maim"),
                        Spell.Cast("Maim", r => Actionmanager.LastSpell.Name == "Heavy Swing"),
                        Spell.Cast("Heavy Swing", r => true)
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
                        Spell.Apply("Fracture"),

                        Spell.Apply("Defiance", r => Core.Player),
                        Spell.Cast("Storm's Eye", r => Actionmanager.LastSpell.Name == "Maim"),
                        Spell.Cast("Butcher's Block", r => Actionmanager.LastSpell.Name == "Skull Sunder"),
                        Spell.Cast("Maim", r => Actionmanager.LastSpell.Name == "Heavy Swing" && !Core.Player.HasAura("Maim")),
                        Spell.Cast("Skull Sunder", r => Actionmanager.LastSpell.Name == "Heavy Swing"),
                        Spell.Cast("Heavy Swing", r => true)
            )));
        }
    }
}