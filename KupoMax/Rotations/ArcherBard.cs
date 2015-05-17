using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Kupo.Helpers;
using Kupo.Settings;
using System.Linq;
using TreeSharp;

namespace Kupo.Rotations
{
    public class ArcherBard : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override float PullRange
        {
            get { return WindowSettings.PullRange; }
        }

        public override void OnInitialize()
        {
            WindowSettings = new ArcherBardSettings();
        }

        public override ClassJobType[] Class
        {
            get { return new ClassJobType[] {ClassJobType.Archer, ClassJobType.Bard,}; }
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
                new Decorator(ctx => ctx != null, new PrioritySelector(
                    new Decorator(req => !BotManager.Current.Name.Equals("Combat Assist"),
                        CommonBehaviors.MoveToLos(ctx => ctx as GameObject)
                    ),
                    new Decorator(req => !BotManager.Current.Name.Equals("Combat Assist"),
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit")
                    ),
                    Spell.PullCast("Heavy Shot")
            )));
        }

        [Behavior(BehaviorType.Combat)]
        public Composite CreateBasicCombat()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null && !Core.Player.IsCasting,
                    new PrioritySelector(
                        new Decorator(req => !BotManager.Current.Name.Equals("Combat Assist"),
                            CommonBehaviors.MoveToLos(ctx => ctx as GameObject)
                        ),
                        new Decorator(req => !BotManager.Current.Name.Equals("Combat Assist"),
                            CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit")
                        ),
                        // CROSS CLASS PUGILIST SKILLS
                        Spell.Apply("Second Wind", req => (Core.Player.CurrentHealthPercent <= 70), on => Core.Player),
                        Spell.Apply("Featherfoot", req => (Core.Player.CurrentHealthPercent <= 40), on => Core.Player),
                        Spell.Apply("Internal Release", on => Core.Player),
                        Spell.Cast("Haymaker"),
                        // ARCHER SKILLS
                        Spell.Cast("Blunt Arrow", req => ((BattleCharacter)Core.Player.CurrentTarget).IsCasting),
                        Spell.Apply("Quelling Strikes", req => (PartyManager.VisibleMembers.Count() > 0), on => Core.Player),
                        Spell.Apply("Barrage", on => Core.Player),
                        Spell.Apply("Hawk's Eye", on => Core.Player),
                        Spell.Apply("Raging Strikes", on => Core.Player),
                        Spell.Cast("Straight Shot", req => !Core.Player.HasAura("Straight Shot", true, 3000)),
                        Spell.Cast("Misery's End", req => ((ArcherBardSettings)WindowSettings).ExecuteAnyTarget && WindowSettings.UseAOE && (KupoRoutine.Instance.ListEnemiesNearTarget(WindowSettings.AOERange).Where(u => u.CurrentHealthPercent < 20).Count() > 0), on => KupoRoutine.Instance.ListEnemiesNearTarget(WindowSettings.AOERange).Where(u => u.CurrentHealthPercent < 20).First()),
                        Spell.Cast("Misery's End"),
                        Spell.Cast("Bloodletter"),
                        Spell.Cast("Straight Shot", req => Core.Player.HasAura("Straighter Shot")),
                        Spell.Apply(name => "Venomous Bite", req =>  TimeToDeathExtension.TimeToDeath((BattleCharacter) Core.Player.CurrentTarget, -1) >= 6, msLeft: 3000),
                        Spell.Apply(name => "Windbite", req => TimeToDeathExtension.TimeToDeath((BattleCharacter) Core.Player.CurrentTarget, -1) >= 6, msLeft: 3000),
                        Spell.CastLocation(sp => "Flaming Arrow", req => WindowSettings.UseAOE, loc => Core.Player.CurrentTarget.Location),
                        Spell.Cast("Quick Nock", req => WindowSettings.UseAOE && EnemiesNearTarget(WindowSettings.AOERange) >= WindowSettings.AOECount && Core.Player.CurrentHealthPercent >= 25 && Core.Player.CurrentTP >= 220),
                        Spell.Apply(name => "Venomous Bite", req => ((ArcherBardSettings)WindowSettings).SpreadDOTS && WindowSettings.UseAOE && (ListEnemiesNearTarget(WindowSettings.AOERange).Where(u => !u.HasAura("Venemous Bite")).Count() > 0), on => ListEnemiesNearTarget(WindowSettings.AOERange).Where(u => !u.HasAura("Venemous Bite")).First(), msLeft: 3000),
                        Spell.Apply(name => "Windbite", req => ((ArcherBardSettings)WindowSettings).SpreadDOTS && WindowSettings.UseAOE && (ListEnemiesNearTarget(WindowSettings.AOERange).Where(u => !u.HasAura("Windbite")).Count() > 0), on => ListEnemiesNearTarget(WindowSettings.AOERange).Where(u => !u.HasAura("Windbite")).First(), msLeft: 3000),
                        Spell.Cast("Heavy Shot", req => (Core.Player.CurrentTP >= 140))
            )));
        }
    }
}