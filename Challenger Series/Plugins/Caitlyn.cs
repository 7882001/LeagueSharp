﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Challenger_Series.Utils;
using LeagueSharp;
using LeagueSharp.SDK;
using LeagueSharp.SDK.Core.UI.IMenu;
using LeagueSharp.SDK.Core.UI.IMenu.Values;
using LeagueSharp.SDK.Core.Utils;
using SharpDX;
using Color = System.Drawing.Color;

namespace Challenger_Series.Plugins
{
    public class Caitlyn : CSPlugin
    {
        public Caitlyn()
        {
            Q = new Spell(SpellSlot.Q, 1240);
            W = new Spell(SpellSlot.W, 820);
            E = new Spell(SpellSlot.E, 800);
            R = new Spell(SpellSlot.R, 2000);

            Q.SetSkillshot(0.25f, 50f, 2000f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 80f, 1600f, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(3000f, 50f, 1000f, false, SkillshotType.SkillshotLine);
            InitMenu();
            Orbwalker.OnAction += OnAction;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
        }

        private void OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (sender.IsMe && args.Animation == "Spell3")
            {
                var target = TargetSelector.GetTarget(1000, DamageType.Physical);
                var pred = Q.GetPrediction(target);
                if (AlwaysQAfterE)
                {
                    if ((int)pred.Hitchance >= (int)HitChance.Medium && pred.UnitPosition.Distance(ObjectManager.Player.ServerPosition) < 1100)
                    Q.Cast(pred.UnitPosition);
                }
                else
                {
                    if ((int)pred.Hitchance > (int)HitChance.Medium && pred.UnitPosition.Distance(ObjectManager.Player.ServerPosition) < 1100)
                    Q.Cast(pred.UnitPosition);
                }
            }
        }

        private void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender is Obj_AI_Hero && sender.IsEnemy && !sender.IsZombie)
            {
            var sdata = SpellDatabase.GetByName(args.SData.Name);
            if (sdata != null && sdata.SpellTags != null)
            {
            if (UseEAntiGapclose)
            {
                if (sdata.SpellTags.Any(tag => tag == SpellTags.Blink || tag == SpellTags.Dash) && args.Target.IsMe && args.End.Distance(ObjectManager.Player.ServerPosition) < 400)
                {
                    E.Cast(sender.ServerPosition);
                }
            }
            if (UseWInterrupt)
            {
                if (sdata.SpellTags.Any(tag => tag == SpellTags.Interruptable))
                {
                    //W.Cast(sender.ServerPosition);
                }
            }
            }
            }
        }

        private void OnDraw(EventArgs args)
        {
            var drawRange = DrawRange.Value;
            if (drawRange>0)
            {
                Drawing.DrawCircle(ObjectManager.Player.Position, drawRange, Color.Gold);
            }
            if (Orbwalker.ActiveMode == OrbwalkingMode.Combo)
            {
                if (UseQCombo && Q.IsReady() && ObjectManager.Player.CountEnemyHeroesInRange(715) == 0 && ObjectManager.Player.CountEnemyHeroesInRange(1100) > 0)
                {
                    Q.CastIfWillHit(TargetSelector.GetTarget(1100, DamageType.Physical), 2);
                }
                if (UseRCombo && R.IsReady() && ObjectManager.Player.CountEnemyHeroesInRange(900) == 0)
                {
                    foreach(var rTarget in base.ValidTargets.Where(e=>SquishyTargets.Contains(e.CharData.BaseSkinName) && R.GetDamage(e) > 0.1*e.MaxHealth))
                    {
                        var pred = R.GetPrediction(rTarget);
                        if (!pred.CollisionObjects.Any(obj => obj is Obj_AI_Hero))
                        {
                            R.CastOnUnit(rTarget);
                        }
                    }
                }
            }
            if (Orbwalker.ActiveMode != OrbwalkingMode.None && Orbwalker.ActiveMode != OrbwalkingMode.Combo && ObjectManager.Player.CountEnemyHeroesInRange(715) == 0)
            {
                var qHarassMode = QHarassMode.SelectedValue;
                if (qHarassMode != "DISABLED")
                {
                    var qTarget = TargetSelector.GetTarget(1100, DamageType.Physical);
                    if (qTarget != null)
                    {
                        var pred = Q.GetPrediction(qTarget);
                        if ((int)pred.Hitchance > (int)HitChance.Medium)
                        {
                            if (qHarassMode == "ALLOWMINIONS")
                            {
                                Q.Cast(pred.UnitPosition);
                            }
                            else if (pred.CollisionObjects.Count == 0)
                            {
                                Q.Cast(pred.UnitPosition);
                            }
                        }
                    }
                }
            }
        }

        private void OnAction(object sender, OrbwalkingActionArgs orbwalkingActionArgs)
        {
            if (orbwalkingActionArgs.Type == OrbwalkingType.BeforeAttack)
            {
                if (orbwalkingActionArgs.Target is Obj_AI_Minion && HasPassive && FocusOnHeadShotting)
                {
                    var target = orbwalkingActionArgs.Target as Obj_AI_Minion;
                    if (!target.CharData.BaseSkinName.Contains("MinionSiege") && target.Health > 60)
                    {
                        orbwalkingActionArgs.Process = false;
                        Orbwalker.ForceTarget = TargetSelector.GetTarget(715, DamageType.Physical);;
                    }
                }
                if (E.IsReady())
                {
                    
            if (!OnlyUseEOnMelees)
            {
            var eTarget = TargetSelector.GetTarget(UseEOnEnemiesCloserThanSlider.Value, DamageType.Physical);
            if (eTarget != null)
            {
                var pred = E.GetPrediction(eTarget);
                if (pred.CollisionObjects.Count == 0 && (int)pred.Hitchance > (int)HitChance.Medium)
                {
                    orbwalkingActionArgs.Process = false;
                    E.Cast(eTarget);
                }
            }
            }
            else
            {
                var eTarget = base.ValidTargets.FirstOrDefault(e=>e.IsMelee && e.Distance(ObjectManager.Player) < UseEOnEnemiesCloserThanSlider.Value && !e.IsZombie);
                var pred = E.GetPrediction(eTarget);
                if (pred.CollisionObjects.Count == 0 && (int)pred.Hitchance > (int)HitChance.Medium)
                {
                    orbwalkingActionArgs.Process = false;
                    E.Cast(eTarget);
                }
            }
                }
            }
            if (orbwalkingActionArgs.Type == OrbwalkingType.AfterAttack)
            {
                Orbwalker.ForceTarget = null;
            }
        }

        private Menu ComboMenu;
        private MenuBool UseQCombo;
        private MenuBool UseECombo;
        private MenuBool UseRCombo;
        private MenuBool AlwaysQAfterE;
        private MenuBool FocusOnHeadShotting;
        private MenuList<string> QHarassMode;
        private MenuBool UseWInterrupt;
        private Menu AutoWConfig;
        private MenuSlider UseEOnEnemiesCloserThanSlider;
        private MenuBool OnlyUseEOnMelees;
        private MenuBool UseEAntiGapclose;
        private MenuSlider DrawRange;
        
        public void InitMenu()
        {
            ComboMenu = MainMenu.Add(new Menu("caitcombomenu", "Combo Settings: "));
            UseQCombo = ComboMenu.Add(new MenuBool("caitqcombo", "Use Q", true));
            UseRCombo = ComboMenu.Add(new MenuBool("caitrcombo", "Use R", true));
            AutoWConfig = MainMenu.Add(new Menu("caitautow", "W Settings: "));
            UseWInterrupt = AutoWConfig.Add(new MenuBool("caitusewinterrupt", "Use W to Interrupt", true));
            new Utils.Logic.PositionSaver(AutoWConfig, W);
            FocusOnHeadShotting = MainMenu.Add(new MenuBool("caitfocusonheadshottingenemies", "Try to save Headshot for poking", true));
            AlwaysQAfterE = MainMenu.Add(new MenuBool("caitalwaysqaftere", "Always Q after E (EQ combo)", true));
            QHarassMode = MainMenu.Add(new MenuList<string>("caitqharassmode", "Q HARASS MODE", new[] {"FULLDAMAGE", "ALLOWMINIONS", "DISABLED"}));
            UseEAntiGapclose = MainMenu.Add(new MenuBool("caiteantigapclose", "Use E AntiGapclose", false));
            UseEOnEnemiesCloserThanSlider = MainMenu.Add(new MenuSlider("caitescape", "Use E on enemies closer than", 400, 100, 650));
            OnlyUseEOnMelees = MainMenu.Add(new MenuBool("caiteonlymelees", "Only use E on melees", false));
            DrawRange = MainMenu.Add(new MenuSlider("caitdrawrange", "Draw a circle with radius: ", 800, 0, 1240));
            MainMenu.Attach();
        }

        private bool HasPassive => ObjectManager.Player.HasBuff("caitlynheadshot");
        private string[] SquishyTargets = 
        {
                "Ahri", "Anivia", "Annie", "Ashe", "Azir", "Brand", "Caitlyn", "Cassiopeia", "Corki", "Draven",
                "Ezreal", "Graves", "Jinx", "Kalista", "Karma", "Karthus", "Katarina", "Kennen", "KogMaw", "Kindred",
                "Leblanc", "Lucian", "Lux", "Malzahar", "MasterYi", "MissFortune", "Orianna", "Quinn", "Sivir", "Syndra",
                "Talon", "Teemo", "Tristana", "TwistedFate", "Twitch", "Varus", "Vayne", "Veigar", "Velkoz", "Viktor",
                "Xerath", "Zed", "Ziggs", "Jhin", "Soraka"
        };
    }
}