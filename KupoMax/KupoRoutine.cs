﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using ff14bot.Enums;
using ff14bot.Navigation;
using ff14bot.NeoProfiles;
using Kupo.Helpers;
using Kupo.Settings;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
using Kupo.Settings;
using TreeSharp;
using ff14bot.AClasses;
using Color = System.Drawing.Color;
using Action = TreeSharp.Action;

namespace Kupo
{
    public abstract partial class KupoRoutine : CombatRoutine
    {
        //public override virtual string Name { get { return "Kupo [" + GetType().Name + "]"; } }

        private Form _configForm;

        public override bool WantButton
        {
            get { return true; }
        }


        public static KupoSettings WindowSettings = KupoSettings.Instance;

        public override void OnButtonPress()
        {
            if (_configForm == null || _configForm.IsDisposed || _configForm.Disposing)
                _configForm = new SettingsForm();

            _configForm.ShowDialog();
        }

        #region CombatRoutine implementation

        public virtual void OnPulse()
        {
        }

        public static bool WantHealing = false;

        public override sealed void Pulse()
        {
            Extensions.DoubleCastPreventionDict.RemoveAll(t => DateTime.UtcNow > t);
            if (WantHealing)
                HealTargeting.Instance.Pulse();

            OnPulse();
        }


        protected Composite SummonChocobo()
        {
            return new PrioritySelector(
                new Decorator(r => WindowSettings.SummonChocobo && !Chocobo.Summoned && Chocobo.CanSummon,
                    new Action(r =>
                    {
                        if (MovementManager.IsMoving)
                            Navigator.PlayerMover.MoveStop();

                        Chocobo.Summon();
                        return RunStatus.Failure;
                    }))
                );
        }



        #region Hidden Overrides
        //Seal this class so that all our child classes will have this logic be called
        public sealed override void ShutDown()
        {
            Logger.Write("Shutingdown " + Name);
            //Clear the events
            GameEvents.OnMapChanged -= OnGameEventsOnOnMapChanged;
            TreeHooks.Instance.OnHooksCleared -= OnInstanceOnOnHooksCleared;
            OnGameContextChanged -= OnOnGameContextChanged;


            _lastContext = GameContext.None;

            CompositeBuilder._methods.Clear();
            OnShutdown();
        }

        //These two functions allow child classes to have specialized initalize and shutdown functions
        public virtual void OnInitialize()
        {
        }

        public virtual void OnShutdown()
        {
        }

        internal static KupoRoutine Instance;

        public override sealed void Initialize()
        {
            Logger.Write("Starting " + Name);
            Instance = this;
            SetupEnergyFunction();
            GameEvents.OnMapChanged += OnGameEventsOnOnMapChanged;

            TreeHooks.Instance.OnHooksCleared += OnInstanceOnOnHooksCleared;


            UpdateContext();

            // NOTE: Hook these events AFTER the context update.
            OnGameContextChanged += OnOnGameContextChanged;


            

            if (!RebuildBehaviors(true))
            {
                return;
            }

            Logger.WriteDiagnostic(Color.White, "Verified behaviors can be created!");
            Logger.Write("Initialization complete!");
            OnInitialize();
        }

        private void OnInstanceOnOnHooksCleared(object s, EventArgs e)
        {
            Logger.Write("Hooks cleared, re-creating behaviors");
            RebuildBehaviors(true);
        }

        private void OnGameEventsOnOnMapChanged(object s, EventArgs e)
        {
            UpdateContext();
        }

        private void OnOnGameContextChanged(object orig, GameContextEventArg ne)
        {
            Logger.Write("Context changed, re-creating behaviors");
            RebuildBehaviors();
        }



        #endregion

        #endregion

        #region Unit Wrappers

        protected BattleCharacter GetTargetMissingDebuff(GameObject near, string debuff, float distance = -1f)
        {
            return
                UnfriendlyUnits.FirstOrDefault(
                    u => u.Location.Distance3D(near.Location) <= distance && !u.HasAura(debuff, true));
        }

        public int EnemiesNearTarget(float range)
        {
            var target = Core.Player.CurrentTarget;
            if (target == null)
                return 0;
            var tarLoc = target.Location;
            return UnfriendlyUnits.Count(u => u.ObjectId != target.ObjectId && u.Location.Distance3D(tarLoc) <= range);
        }

        //protected IEnumerable<GameObject> UnfriendlyMeleeUnits { get { return UnfriendlyUnits.Where(u => Actionmanager.InSpellInRangeLOS()); } }

        protected IEnumerable<BattleCharacter> UnfriendlyUnits
        {
            get
            {
                return
                    GameObjectManager.GetObjectsOfType<BattleCharacter>().Where(u => !u.IsDead && u.CanAttack);
            }
        }

        #endregion

        #region Spell Casting

        protected Composite EnsureTarget
        {
            get
            {
                return
                    new Decorator(
                        r =>
                            Core.Player.SpellCastInfo != null &&
                            Core.Player.SpellCastInfo.TargetId != GameObjectManager.EmptyGameObject,
                        new PrioritySelector(
                            r => GameObjectManager.GetObjectByObjectId(Core.Player.SpellCastInfo.TargetId),
                            new Decorator(r => (r as Character) != null && (r as Character).IsDead,
                                new PrioritySelector(
                                    new FailLogger(
                                        r =>
                                            string.Format("Stop casting at {0} because it is dead.",
                                                (r as GameObject).Name)),
                                    new Action(r => Actionmanager.StopCasting()))
                                )));
            }
        }



        #endregion

        #region KupoMax

        public IEnumerable<BattleCharacter> ListEnemiesNearTarget(float range)
        {
            var target = Core.Player.CurrentTarget;
            if (target == null)
                return new List<BattleCharacter>();
            var tarLoc = target.Location;
            return UnfriendlyUnits.Where(u => u.ObjectId != target.ObjectId && u.Location.Distance3D(tarLoc) <= range);
        }

        public IEnumerable<Character> LowPartyMembersNear(Clio.Utilities.Vector3 l, float dist, int pct)
        {
            //Logging.Write("{0} Low Party Members under {1}% within {2} yalm of {3}", members.Count, pct, dist, l);
            return VisiblePartyMembers().Where(c => c.CurrentHealthPercent < pct && c.Location.Distance3D(l) <= dist);
        }

        public IEnumerable<Character> DebuffedPartyMembers()
        {
            return VisiblePartyMembers().Where(pm => HasDebuff(pm));
        }

        private static String[] Debuffs = { 
            "Heavy","Blind","Bleed","Disease","Incapacitation","Leaden","Mute",
            "Pacification","Paralysis","Petrification","Pox","Silence","Sleep","Slow","Doom"};

        public bool HasDebuff(Character c)
        {
            foreach (String aura in Debuffs)
                if (c.HasAura(aura))
                    return true;
            return false;
        }

        public bool IsTank(Character c)
        {
            switch (c.CurrentJob)
            {
                case ClassJobType.Paladin:
                case ClassJobType.Marauder:
                case ClassJobType.Gladiator:
                case ClassJobType.Warrior:
                    return true;
                default:
                    return false;
            }
        }

        public List<Character> VisiblePartyMembers()
        {
            List<Character> members = new List<Character>();
            if (PartyManager.IsInParty)
            {
                foreach (PartyMember pm in PartyManager.AllMembers)
                {
                    if (pm.IsInObjectManager)
                    {
                        Character c = (Character)GameObjectManager.GetObjectByObjectId(pm.ObjectId);
                        members.Add(c);
                    }
                }
            }
            return members;
        }

        public List<BattleCharacter> Attackers(Character c,params String[] exAuras)
        {
            List<BattleCharacter> attackers = new List<BattleCharacter>();
            foreach (BattleCharacter b in GameObjectManager.Attackers)
            {
                bool add = false;
                if (b.CurrentTargetId == c.ObjectId)
                {
                    add = true;
                    foreach (String a in exAuras)
                    {
                        if (b.HasAura(a))
                            add = false;
                    }
                    if(add)
                        attackers.Add(b);
                }
            }
            return attackers;
        }

        public List<Character> UnaggrodEnemies(Clio.Utilities.Vector3 loc, float dist)
        {
            List<Character> enemies = new List<Character>();
            foreach (GameObject o in GameObjectManager.GameObjects)
            {
                if (o != null
                        && o is Character
                        && ((Character)o).CurrentTargetId != Core.Player.ObjectId 
                        && o.Location.Distance3D(loc) <= dist
                        && o.CanAttack
                        && o.IsValid
                        && o.IsTargetable)
                    enemies.Add((Character)o);
            }
            return enemies;
        }
        #endregion

        protected delegate T Selection<out T>(object context);
        protected delegate T Selectionz<out T>();
    }


}