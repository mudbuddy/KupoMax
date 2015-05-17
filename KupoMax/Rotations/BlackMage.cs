using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using Kupo;
using Kupo.Helpers;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Kupo.Rotations
{
    internal class BlackMage : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override ClassJobType[] Class
        {
            get { return new[] { ClassJobType.BlackMage }; }
        }

        public override float PullRange
        {
            get { return 20; }
        }

        [Behavior(BehaviorType.PreCombatBuffs)]
        public Composite CreateBasicPreCombatBuffs()
        {
            return SummonChocobo();
        }

        [Behavior(BehaviorType.Rest)]
        public Composite CreateBasicRest()
        {
            return DefaultRestBehavior(r => Core.Player.CurrentManaPercent);
        }


        private readonly string[] PullSpells = new[] { "Thunder II", "Thunder", "Blizzard" };
        private string _BestPullSpell;
        private uint _level;
        private string BestPullSpell
        {
            get
            {

                if (_level != Core.Player.ClassLevel)
                {
                    _level = Core.Player.ClassLevel;
                    _BestPullSpell = null;
                }

                if (string.IsNullOrEmpty(_BestPullSpell))
                {
                    foreach (var spell in PullSpells.Where(Actionmanager.HasSpell))
                    {
                        _BestPullSpell = spell;
                        break;
                    }

                }

                return _BestPullSpell;
            }
        }


        [Behavior(BehaviorType.Pull)]
        public Composite CreateBasicPull()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
    new Decorator(ctx => ctx != null, new PrioritySelector(
                    EnsureTarget,
            CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
            CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit"),
    Spell.PullApply(r => BestPullSpell)
    )));
        }


        [Behavior(BehaviorType.CombatBuffs)]
        public Composite CreateBasicCombatBuffs()
        {
            return new PrioritySelector(
                //Dem deepz
                Spell.Apply("Raging Strikes"),
                Spell.Apply("Swiftcast")
                );
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                Spell.Apply("Manaward", r => Core.Player.CurrentHealthPercent <= 80),
                Spell.Apply("Manawall", r => Core.Player.CurrentHealthPercent <= 80),
                Spell.Cast("Physick", r => Core.Player.CurrentHealthPercent <= 40, r => Core.Player)
                );
        }

        [Behavior(BehaviorType.Combat)]
        public Composite CreateBasicCombat()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null,
                    new PrioritySelector(
                        CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit"),
                //Need to check for insta procs first and foremost
                        Spell.Cast("Thunder III", r => Core.Player.HasAura("Thundercloud")),
                        Spell.Cast("Fire III", r => Core.Player.HasAura("Firestarter")),

                        //If we're low on mana we need to make sure we get it back
                        Spell.Cast("Blizzard III", r => Core.Player.CurrentMana < 638 && Core.Player.ClassLevel >= 38),
                        Spell.Cast("Blizzard", r => Core.Player.CurrentManaPercent <= 10 && Core.Player.ClassLevel < 38),
                        Spell.Cast("Convert", r => Core.Player.CurrentMana < 79 && Core.Player.ClassLevel >= 30),
                //79 Mana is how much Blizzard III is with AstralFire ... don't want to be stuck with no mana

                        Spell.Apply("Thunder II", r => Core.Player.ClassLevel >= 22 && !Core.Target.HasAura("Thunder")),
                        Spell.Apply("Thunder", r => Core.Player.ClassLevel < 22),
                        Spell.Cast("Fire III",
                            r =>
                                Core.Player.ClassLevel >= 34 && !Core.Player.HasAura("Astral Fire III") &&
                                Core.Player.CurrentMana > 638),
                //Bread and butter Fire spam
                        Spell.Cast("Fire")
                        )));
        }
    }
}