using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Clio.Utilities;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Kupo.Helpers;
using Newtonsoft.Json;
using TreeSharp;

namespace Kupo.Rotations
{

    internal class Ninja : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override ClassJobType[] Class
        {
            get { return new[] { ClassJobType.Ninja }; }
        }

        public override float PullRange
        {
            get { return 2.5f; }
        }

        [Behavior(BehaviorType.PreCombatBuffs)]
        public Composite CreateBasicPreCombatBuffs()
        {
            return new PrioritySelector(

                Spell.Apply("Kiss of the Wasp", r => (Core.Player.ClassLevel >= 36 || !Actionmanager.HasSpell("Kiss of the Viper")), r => Core.Player),
                Spell.Apply("Kiss of the Viper", r => Actionmanager.HasSpell("Kiss of the Viper") && Core.Player.ClassLevel < 36, r => Core.Player),
                SummonChocobo()
                );
        }

        [Behavior(BehaviorType.Rest)]
        public Composite CreateBasicRest()
        {
            return DefaultRestBehavior(r => Core.Player.CurrentTPPercent);
        }


        [Behavior(BehaviorType.CombatBuffs)]
        public Composite CreateBasicCombatBuffs()
        {
            return new PrioritySelector(
                Spell.Cast("Invigorate", r => Core.Player.CurrentTP < 550, r => Core.Player)


                );
        }

        [Behavior(BehaviorType.Pull)]
        public Composite CreateBasicPull()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null,
                    new PrioritySelector(
                        CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, ctx => Core.Player.CombatReach + PullRange + (ctx as GameObject).CombatReach, true, "Moving to unit"),

                        Spell.PullCast("Spinning Edge")

                        )));
        }


        private readonly SpellData Jin = DataManager.GetSpellData(2263);
        private readonly SpellData Chi = DataManager.GetSpellData(2261);
        private readonly SpellData Ten = DataManager.GetSpellData(2259);
        private readonly SpellData Ninjutsu = DataManager.GetSpellData(2260);

        private readonly SpellData Kassatsu = DataManager.GetSpellData(2264);

        private readonly SpellData Trick_Attack = DataManager.GetSpellData(2258);
        private readonly SpellData Sneak_Attack = DataManager.GetSpellData(2250);

        public static HashSet<uint> OverrideBackstabIds = new HashSet<uint>()
        {
            3240//Cloud of darkness
        };


        private const int HutonRecast = 20000;
        private async Task<bool> DoNinjutsu()
        {


            //Exit early if player was inputting something
            if (Core.Player.HasAura("Mudra"))
                return true;

            if (Actionmanager.CanCastOrQueue(Jin, null))
            {
                if (!Core.Player.HasAura("Huton", true, HutonRecast))
                {
                    await CastHuton();
                    return false;
                }

                var curTarget = Core.Target as BattleCharacter;
                if (curTarget == null)
                    return false;

                if (curTarget.TimeToDeath() <= 3)
                    return false;

                //Suiton
                var taCD = Trick_Attack.Cooldown;
                //We can start casting suiton before trick attack is ready cause its going to take some time
                if (taCD.TotalMilliseconds <= 1300)
                {
                    if (!await CastSuiton())
                        return false;

                    if (!await CastTrickAttack())
                        return false;

                    if (!(Kassatsu.Cooldown.TotalMilliseconds <= 0) || !Core.Player.HasTarget)
                        return false;

                    if (await Coroutine.Wait(5000, () => Actionmanager.DoAction(Kassatsu, null)))
                    {
                        await CastRaiton();
                    }


                    return false;

                }

                if (taCD.TotalSeconds >= 20)
                {
                    await CastRaiton();
                }


                return false;
            }



            if (Actionmanager.CanCastOrQueue(Chi, null))
            {
                await CastRaiton();
                return false;
            }

            if (Actionmanager.CanCastOrQueue(Ten, null))
            {
                await Coroutine.Wait(5000, () => Actionmanager.DoAction(Ten, null));
                await CastNinjutsu();
                return false;
            }




            return false;
        }

        private async Task CastHuton()
        {
            if (await Coroutine.Wait(5000, () => Actionmanager.DoAction(Jin, null)))
            {
                if (await Coroutine.Wait(5000, () => Actionmanager.DoAction(Chi, null)))
                {
                    if (await Coroutine.Wait(5000, () => Actionmanager.DoAction(Ten, null)))
                    {
                        await CastNinjutsu();
                    }
                }
            }
        }


        private async Task<bool> CastTrickAttack()
        {

            while (Core.Player.HasAura("Suiton"))
            {
                if (Core.Player.HasTarget)
                {
                    if (OverrideBackstabIds.Contains(Core.Target.NpcId) || Core.Target.IsBehind)
                    {
                        Actionmanager.DoAction(Trick_Attack, Core.Target);
                    }
                    else if (BotManager.Current.IsAutonomous)
                    {
                        Actionmanager.DoAction(Sneak_Attack, Core.Target);
                    }
                }
                if (!Core.Player.InCombat)
                    return false;

                await Coroutine.Yield();
            }

            if (!BotManager.Current.IsAutonomous)
            {
                return await Coroutine.Wait(2000, () => Core.Target != null && Core.Target.IsValid && Core.Target.HasAura("Vulnerability Up"));
            }

            return false;
        }


        private async Task<bool> CastRaiton()
        {
            if (!await Coroutine.Wait(5000, () => Actionmanager.DoAction(Ten, null))) return false;
            if (await Coroutine.Wait(5000, () => Actionmanager.DoAction(Chi, null)))
            {
                return await CastNinjutsu();
            }
            return false;
        }

        private async Task<bool> CastSuiton()
        {
            if (!await Coroutine.Wait(5000, () => Actionmanager.DoAction(Ten, null))) return false;
            if (!await Coroutine.Wait(5000, () => Actionmanager.DoAction(Chi, null))) return false;
            if (!await Coroutine.Wait(5000, () => Actionmanager.DoAction(Jin, null))) return false;


            if (await CastNinjutsu())
            {
                return await Coroutine.Wait(5000, () => Core.Player.HasAura("Suiton"));
            }

            return false;
        }


        private async Task<bool> CastNinjutsu()
        {
            if (await Coroutine.Wait(5000, () => Core.Player.HasAura("Mudra")))
            {
                bool possibly = false;
                while (Core.Player.HasAura("Mudra"))
                {
                    if (Core.Player.HasTarget)
                    {
                        if (Actionmanager.DoAction(Ninjutsu, Core.Target))
                        {
                            possibly = true;
                        }
                    }
                    if (!Core.Player.InCombat)
                        return false;

                    await Coroutine.Yield();
                }
                await Coroutine.Wait(5000, () => Ninjutsu.Cooldown.TotalSeconds > 10);
                return possibly;
            }
            return false;
        }


        private bool HasBleedingDebuff(BattleCharacter r)
        {

            return r.HasAura("Storm's Eye", false, 2000) || r.HasAura("Dancing Edge", false, 2000);

        }

        private bool ShouldMutilate(BattleCharacter r)
        {
            var name = Actionmanager.LastSpell.Name;
            if ((name == "Aeolian Edge" || name == "Shadow Fang" || name == "Dancing Edge"))
            {
                if (HasBleedingDebuff(r))
                {
                    if (r.HasAura("Shadow Fang", true, 2000))
                    {
                        return true;
                    }

                }
            }


            return false;

            //!(r as BattleCharacter).HasAura("Mutilation", true, 3500) && 

        }


        [Behavior(BehaviorType.Combat)]
        public Composite CreateBasicCombat()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null,
                    new PrioritySelector(
                        CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, ctx => Core.Player.CombatReach + PullRange + (ctx as GameObject).CombatReach, true, "Moving to unit"),


                        Spell.Apply("Kiss of the Wasp", r => (Core.Player.ClassLevel >= 36 || !Actionmanager.HasSpell("Kiss of the Viper")), r => Core.Player),
                        Spell.Apply("Kiss of the Viper", r => Actionmanager.HasSpell("Kiss of the Viper") && Core.Player.ClassLevel < 36, r => Core.Player),
                        
                        new ActionRunCoroutine(r => DoNinjutsu()),
                        Spell.Cast("Trick Attack", r => Core.Player.HasAura("Suiton") && (r as BattleCharacter).IsBehind),
                        Spell.Cast("Assassinate"),
                        Spell.Apply(r => "Shadow Fang", r => Actionmanager.LastSpell.Name == "Spinning Edge", msLeft: 2000),
                        Spell.Cast(r => "Dancing Edge", r => !HasBleedingDebuff((r as BattleCharacter)) && Actionmanager.LastSpell.Name == "Gust Slash"),
                        
                        Spell.Cast("Aeolian Edge", r => Actionmanager.LastSpell.Name == "Gust Slash"),
                        Spell.Cast("Gust Slash", r => Actionmanager.LastSpell.Name == "Spinning Edge"),
                        Spell.Cast(r => "Mutilate", r => ShouldMutilate((r as BattleCharacter))),
                        Spell.Cast("Mug"),
                        Spell.Cast("Spinning Edge")

                        )));
        }

    }

}
