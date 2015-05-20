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

        [Behavior(BehaviorType.Rest)]
        public Composite CreateBasicRest()
        {
            return DefaultRestBehavior(r => Core.Player.CurrentManaPercent);
        }

        [Behavior(BehaviorType.Pull)]
        public Composite CreateBasicPull()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null, new PrioritySelector(
                    new Decorator(req => !BotManager.Current.Name.Contains("Assist"),
                        CommonBehaviors.MoveToLos(ctx => ctx as GameObject)
                    ),
                    new Decorator(req => !BotManager.Current.Name.Contains("Assist"),
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit")
                    ),
                    Spell.PullCast("Stone")
            )));
        }

        [Behavior(BehaviorType.PreCombatBuffs)]
        public Composite CreateBasicPreCombatBuffs()
        {
            return new PrioritySelector(
                SummonChocobo(),
                Spell.Apply("Protect", r => true, r => Core.Player)
            );
        }

        [Behavior(BehaviorType.Heal, GameContext.PvP)]
        public Composite CreateHealPvP()
        {
            return new PrioritySelector(ctx => HealTargeting.Instance.FirstUnit,
                new Decorator(ctx => ctx != null,
                    new PrioritySelector(
                        new Decorator(req => !BotManager.Current.Name.Contains("Assist"),
                            CommonBehaviors.MoveToLos(ctx => ctx as GameObject)
                        ),
                        new Decorator(req => !BotManager.Current.Name.Contains("Assist"),
                            CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit")
                        ),
                        Spell.Cast("Cure", r => HealTargeting.Instance.FirstUnit.CurrentHealthPercent < 80, r => HealTargeting.Instance.FirstUnit)
                ))
                //As a pvp healer dont let combat logic run
                , new ActionAlwaysSucceed()
           );
        }

        [Behavior(BehaviorType.PreCombatBuffs, GameContext.Instances)]
        public Composite CreateBasicPreCombatBuffsParty()
        {
            return new PrioritySelector(
                Spell.Apply("Protect", r => true, r => Core.Player)
            );
        }

        [Behavior(BehaviorType.Heal, GameContext.Instances)]
        public Composite CreateHealParty()
        {
            return new PrioritySelector(
                Spell.Cast("Medica", req => (LowPartyMembersNear(Core.Player.Location, 12f, 75, true)).Count >= 3, on => Core.Player),
                new PrioritySelector(ctx => HealTargeting.Instance.FirstUnit,
                    new Decorator(ctx => ctx != null,
                        new PrioritySelector(
                            new Decorator(req => !BotManager.Current.Name.Contains("Assist"),
                                CommonBehaviors.MoveToLos(ctx => ctx as GameObject)
                            ),
                            new Decorator(req => !BotManager.Current.Name.Contains("Assist"),
                                CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit")
                            ),
                            Spell.Cast("Cure", ctx => (ctx as Character).CurrentHealthPercent < 80, r => HealTargeting.Instance.FirstUnit)
            ))));
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                //Check to see if we have cleric stance up or not -- If so, remove it for better heals
                Spell.Apply("Cleric Stance", r => Core.Player.HasAura("Cleric Stance") && Core.Player.CurrentHealthPercent <= 40, r => Core.Player),
                //If we have a free Cure II and we have Cure II use it!
                Spell.Cast("Cure II", r => Core.Player.HasAura("Freecure"), r => Core.Player),
                Spell.Cast("Cure", r => Core.Player.CurrentHealthPercent <= 40, r => Core.Player)
            );
        }

        [Behavior(BehaviorType.Combat)]
        public Composite CreateBasicCombat()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null,
                    new PrioritySelector(
                        new Decorator(req => !BotManager.Current.Name.Contains("Assist"),
                            CommonBehaviors.MoveToLos(ctx => ctx as GameObject)
                        ),
                        new Decorator(req => !BotManager.Current.Name.Contains("Assist"),
                            CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit")
                        ),
                        //Check to see if we have cleric stance up or not -- Gotta get them deepz
                        Spell.Cast("Cleric Stance", r => !Core.Player.HasAura("Cleric Stance") && !PartyManager.IsInParty, r => Core.Player),

                        //Check to see if we need to get mana back
                        Spell.Cast("Shroud of Saints", r => (Core.Player.MaxMana - Core.Player.CurrentMana > 1200), r => Core.Player),

                        //Get our DoTs up and going
                        //Get the Aero I/II dot up and going
                        Spell.Apply("Aero II"),
                        Spell.Apply("Aero", r => Core.Player.ClassLevel < 46),

                        //Get the thunder dot up and going
                        Spell.Apply("Thunder"),

                        //Use the push back if we can
                        Spell.Cast("Fluid Aura", r => !PartyManager.IsInParty && Core.Target.Distance2D() <= 15f),

                        //Bread and butter Stone I/II spam
                        Spell.Cast("Stone", r => Core.Player.ClassLevel < 22),
                        Spell.Cast("Stone II")
            )));
        }
    }
}