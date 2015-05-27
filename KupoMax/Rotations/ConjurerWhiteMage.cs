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
using Kupo.Settings;

namespace Kupo.Rotations
{
    internal class ConjurerWhiteMage : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override ClassJobType[] Class
        {
            get { return new[] {ClassJobType.Conjurer, ClassJobType.WhiteMage,}; }
        }

        public override float PullRange
        {
            get { return WindowSettings.PullRange; }
        }

        public override void OnInitialize()
        {
            WindowSettings = new ConjurerWhiteMageSettings();
        }

        [Behavior(BehaviorType.Rest)]
        public Composite CreateBasicRest()
        {
            return DefaultRestBehavior(r => Core.Player.CurrentManaPercent);
        }

        [Behavior(BehaviorType.Pull)]
        public Composite CreateBasicPull()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null && WindowSettings.AllowMovement, 
                    new PrioritySelector(
                        CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit"),
                        Spell.PullCast("Stone")     )));
        }

        [Behavior(BehaviorType.PreCombatBuffs)]
        public Composite CreateBasicPreCombatBuffs()
        {
            return new PrioritySelector(
                SummonChocobo(),
                Spell.Apply("Protect", on => Core.Player)
            );
        }

        [Behavior(BehaviorType.Heal, GameContext.PvP)]
        public Composite CreateHealPvP()
        {
            return new PrioritySelector(ctx => HealTargeting.Instance.FirstUnit,
                new Decorator(ctx => ctx != null && WindowSettings.AllowMovement,
                    new PrioritySelector(
                        CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit"),
                        Spell.Cast("Cure", 
                            req => HealTargeting.Instance.FirstUnit.CurrentHealthPercent < ((ConjurerWhiteMageSettings)WindowSettings).CurePartyPercent, 
                            on => HealTargeting.Instance.FirstUnit)
                )), 
                new ActionAlwaysSucceed()       );
        }

        [Behavior(BehaviorType.PreCombatBuffs, GameContext.Instances)]
        public Composite CreateBasicPreCombatBuffsParty()
        {
            return new PrioritySelector(
                Spell.Apply("Protect", 
                    req => PartyManager.IsInParty && VisiblePartyMembers().Where(u => !u.HasAura("Protect")).Count() > 0, 
                    on => VisiblePartyMembers().Where(u => !u.HasAura("Protect")).First())
            );
        }

        [Behavior(BehaviorType.Heal, GameContext.Instances)]
        public Composite CreateHealParty()
        {
            return new PrioritySelector(
                new PrioritySelector(ctx => HealTargeting.Instance.FirstUnit,
                    Spell.Cast("Cure II", 
                        ctx => (ctx as Character).CurrentHealthPercent < ((ConjurerWhiteMageSettings)WindowSettings).Cure2HealPercent, 
                        on => HealTargeting.Instance.FirstUnit)),
                Spell.Apply("Medica", 
                    req => (LowPartyMembersNear(Core.Player.Location, 12f, ((ConjurerWhiteMageSettings)WindowSettings).MedicaPercent)).Count() >= ((ConjurerWhiteMageSettings)WindowSettings).MedicaCount, 
                    on => Core.Player),
                new PrioritySelector(ctx => HealTargeting.Instance.FirstUnit,
                    new Decorator(ctx => ctx != null && WindowSettings.AllowMovement,
                        new PrioritySelector(
                                CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                                CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit"))),
                    Spell.Cast("Cure II", 
                        ctx => Core.Player.HasAura("Freecure") 
                            && (ctx as Character).CurrentHealthPercent < ((ConjurerWhiteMageSettings)WindowSettings).CurePartyPercent, 
                        on => HealTargeting.Instance.FirstUnit),
                    Spell.Cast("Cure", 
                        ctx => (ctx as Character).CurrentHealthPercent < ((ConjurerWhiteMageSettings)WindowSettings).CurePartyPercent, 
                        on => HealTargeting.Instance.FirstUnit),
                    Spell.Apply("Regen",
                        ctx => (ctx as Character).CurrentHealthPercent < ((ConjurerWhiteMageSettings)WindowSettings).RegenPercent,
                        on => HealTargeting.Instance.FirstUnit),
                    Spell.Apply("Esuna", 
                        req => DebuffedPartyMembers().Count() > 0, 
                        on => DebuffedPartyMembers().First()),
                    new Throttle(8,Spell.Apply("Repose", 
                        req => Attackers(Core.Me,"Sleep").Count > 0 
                            && ((ConjurerWhiteMageSettings)WindowSettings).ReposeAttackers, 
                        on => Attackers(Core.Me,"Sleep").First())),
                    new Throttle(15,Spell.Cast("Raise", 
                        req => VisiblePartyMembers().Where(pm => pm.IsDead).Count() > 0, 
                        on => VisiblePartyMembers().Where(pm => pm.IsDead).First()))));
        }

        [Behavior(BehaviorType.CombatBuffs)]
        public Composite CreateBasicCombatBuffsParty()
        {
            return new PrioritySelector(
                //Spell.Apply("Stoneskin", req => VisiblePartyMembers().Where(c => !c.HasAura("Stoneskin")).Count() > 0, r => VisiblePartyMembers().Where(c => !c.HasAura("Stoneskin")).First()),
                //Spell.Apply("Stoneskin", req => !Core.Player.HasAura("Stoneskin"), on => Core.Player)
                //Spell.Apply("Presence of Mind", on => Core.Player)
            );
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                Spell.Apply("Cleric Stance", 
                    req => Core.Player.HasAura("Cleric Stance") 
                        && Core.Player.CurrentHealthPercent < ((ConjurerWhiteMageSettings)WindowSettings).CureSoloPercent,
                    on => Core.Player),
                Spell.Apply("Cure II", 
                    req => (Core.Player.HasAura("Freecure") 
                            && Core.Player.CurrentHealthPercent < ((ConjurerWhiteMageSettings)WindowSettings).CureSoloPercent)
                        || Core.Player.CurrentHealthPercent < ((ConjurerWhiteMageSettings)WindowSettings).Cure2HealPercent, 
                    on => Core.Player),
                Spell.Apply("Cure", 
                    req => Core.Player.CurrentHealthPercent < ((ConjurerWhiteMageSettings)WindowSettings).CureSoloPercent,
                    on => Core.Player),
                Spell.Apply("Regen",
                    req => Core.Player.CurrentHealthPercent < ((ConjurerWhiteMageSettings)WindowSettings).RegenPercent,
                    on => Core.Player),
                Spell.Apply("Esuna", 
                    req => HasDebuff(Core.Player), 
                    on => Core.Player)
            );
        }

        [Behavior(BehaviorType.Combat)]
        public Composite CreateBasicCombat()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null,
                    new PrioritySelector(
                        new Decorator(req => WindowSettings.AllowMovement,
                            CommonBehaviors.MoveToLos(ctx => ctx as GameObject)
                        ),
                        new Decorator(req => WindowSettings.AllowMovement,
                            CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit")
                        ),
                        Spell.Apply("Presence of Mind", on => Core.Player),
                        Spell.Cast("Cleric Stance", 
                            req => !Core.Player.HasAura("Cleric Stance") 
                                && ((ConjurerWhiteMageSettings)WindowSettings).UseClericStance, 
                            on => Core.Player),
                        Spell.Cast("Shroud of Saints", 
                            req => (Core.Player.MaxMana - Core.Player.CurrentMana > 1200) 
                                || (Core.Player.CurrentManaPercent < ((ConjurerWhiteMageSettings)WindowSettings).SaveManaPercent), 
                            on => Core.Player),
                        Spell.Apply("Aero II"),
                        Spell.Apply("Aero", req => Core.Player.ClassLevel < 46),
                        Spell.Apply("Thunder"),
                        Spell.Cast("Fluid Aura", 
                            req => ((ConjurerWhiteMageSettings)WindowSettings).UseFluidAura 
                                && Core.Target.Distance2D() <= 15f),
                        Spell.Cast("Stone", 
                            req => Core.Player.CurrentManaPercent >= ((ConjurerWhiteMageSettings)WindowSettings).SaveManaPercent 
                                && (TimeToDeathExtension.TimeToDeath((BattleCharacter)Core.Player.CurrentTarget,long.MinValue) >= 2) 
                                && Core.Player.ClassLevel < 22),
                        Spell.Cast("Stone II", 
                            req => Core.Player.CurrentManaPercent >= ((ConjurerWhiteMageSettings)WindowSettings).SaveManaPercent 
                                && (TimeToDeathExtension.TimeToDeath((BattleCharacter)Core.Player.CurrentTarget, long.MinValue) >= 2))
            )));
        }
    }
}