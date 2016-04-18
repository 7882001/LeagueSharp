﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Challenger_Series.Utils;

namespace Challenger_Series.Plugins
{
    public class Lucian : CSPlugin
    {
        public Lucian()
        {
            Q = new Spell(SpellSlot.Q, 675);
            Q2 = new Spell(SpellSlot.Q, 1200);
            W = new Spell(SpellSlot.W, 1200f);
            E = new Spell(SpellSlot.E, 475f);
            R = new Spell(SpellSlot.R, 1400);

            Q.SetTargetted(0.25f, 1400f);
            Q2.SetSkillshot(0.5f, 50, float.MaxValue, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.30f, 80f, 1600f, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.2f, 110f, 2500, true, SkillshotType.SkillshotLine);
            InitMenu();
            Obj_AI_Hero.OnDoCast += OnDoCast;
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Events.OnGapCloser += EventsOnOnGapCloser;
            Events.OnInterruptableTarget += OnInterruptableTarget;
        }

        private void OnInterruptableTarget(object sender, Events.InterruptableTargetEventArgs args)
        {
            if (E.IsReady() && args.DangerLevel == DangerLevel.High && args.Sender.Distance(ObjectManager.Player) < 400)
            {
                E.Cast(ObjectManager.Player.Position.Extend(args.Sender.Position, -Misc.GiveRandomInt(300, 600)));
            }
        }

        private void EventsOnOnGapCloser(object sender, Events.GapCloserEventArgs args)
        {
            if (E.IsReady() && args.IsDirectedToPlayer && args.Sender.Distance(ObjectManager.Player) < 800)
            {
                E.Cast(ObjectManager.Player.Position.Extend(args.Sender.Position, -Misc.GiveRandomInt(300, 600)));
            }
        }

        public override void OnDraw(EventArgs args)
        {
            if (QKS && Q.IsReady())
            {
                var targets = ValidTargets.Where(x => x.IsValidTarget(Q.Range) && !x.IsZombie);
                foreach (var target in targets)
                {
                    if (target.Health < Q.GetDamage(target) &&
                        (!target.HasBuff("kindrednodeathbuff") && !target.HasBuff("Undying Rage") &&
                         !target.HasBuff("JudicatorIntervention")))
                    {
                        Q.Cast(target);
                    }
                }
            }
            var tg = TargetSelector.GetTarget(ObjectManager.Player.AttackRange, DamageType.Physical);
            if (HasPassive)
            {
                if (UsePassiveOnEnemy && tg.IsValidTarget())
                {
                    Orbwalker.ForceTarget = tg;
                    return;
                }
            }
            Orbwalker.ForceTarget = null;
            var q2tg = TargetSelector.GetTarget(Q2.Range);
            if (Q.IsReady() && tg.IsHPBarRendered)
            {
                if (Orbwalker.ActiveMode != OrbwalkingMode.None && Orbwalker.ActiveMode != OrbwalkingMode.Combo &&
                    UseQHarass) Q.Cast(tg);
                if (q2tg.Distance(ObjectManager.Player) > Q.Range)
                {
                    if (Orbwalker.ActiveMode != OrbwalkingMode.None && Orbwalker.ActiveMode != OrbwalkingMode.Combo)
                    {
                        if (UseQExtended &&
                            ObjectManager.Player.ManaPercent > QExManaPercent)
                        {
                            var minions =
                                GameObjects.EnemyMinions.Where(
                                    m => m.IsHPBarRendered && m.Distance(ObjectManager.Player) < Q.Range);
                            foreach (var minion in minions)
                            {
                                var QHit = new Utils.Geometry.Rectangle(ObjectManager.Player.Position,
                                    ObjectManager.Player.Position.Extend(minion.Position, Q2.Range), Q2.Width);
                                var QPred = Q2.GetPrediction(q2tg);
                                if (!QPred.UnitPosition.IsOutside(QHit) && QPred.Hitchance >= HitChance.High)
                                {
                                    Q.Cast(minion);
                                    return;
                                }
                            }
                        }
                    }
                }
                if (q2tg.Health < Q.GetDamage(q2tg) &&
                    (!q2tg.HasBuff("kindrednodeathbuff") && !q2tg.HasBuff("Undying Rage") &&
                     !q2tg.HasBuff("JudicatorIntervention")))
                {
                    var minions =
                        GameObjects.EnemyMinions.Where(
                            m => m.IsHPBarRendered && m.Distance(ObjectManager.Player) < Q.Range);
                    foreach (var minion in minions)
                    {
                        var QHit = new Utils.Geometry.Rectangle(ObjectManager.Player.Position,
                            ObjectManager.Player.Position.Extend(minion.Position, Q2.Range), Q2.Width);
                        var QPred = Q2.GetPrediction(q2tg);
                        if (QPred.UnitPosition.IsOutside(QHit) && QPred.Hitchance >= HitChance.High)
                        {
                            Q.Cast(minion);
                            return;
                        }
                    }
                }
            }
        }

        private void OnDoCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.SData.Name == "LucianPassiveShot" || args.SData.Name.Contains("LucianBasicAttack"))
            {
                if (!HasPassive)
                {
                    var target = TargetSelector.GetTarget(ObjectManager.Player.AttackRange, DamageType.Physical);
                    if (Orbwalker.ActiveMode == OrbwalkingMode.Combo &&
                        target.Distance(ObjectManager.Player) < ObjectManager.Player.AttackRange)
                    {
                        if (E.IsReady())
                        {
                            switch (UseEMode.SelectedValue)
                            {
                                case "Side":
                                    E.Cast(
                                        Deviation(ObjectManager.Player.Position.ToVector2(), target.Position.ToVector2(),
                                            65).ToVector3());
                                    break;
                                case "Cursor":
                                    E.Cast(ObjectManager.Player.Position.Extend(Game.CursorPos,
                                        Misc.GiveRandomInt(50, 100)));
                                    break;
                                case "Enemy":
                                    E.Cast(ObjectManager.Player.Position.Extend(target.Position,
                                        Misc.GiveRandomInt(50, 100)));
                                    break;
                            }
                        }
                        if (UseQCombo && Q.IsReady())
                        {
                            Q.Cast(target);
                            return;
                        }
                        if (UseWCombo && W.IsReady())
                        {
                            W.Cast(target);
                            return;
                        }
                    }
                    if (args.Target is Obj_AI_Minion)
                    {
                        var tg = args.Target as Obj_AI_Minion;
                        if (QJg && Q.IsReady())
                        {
                            Q.Cast(tg);
                            return;
                        }
                        if (WJg && W.IsReady())
                        {
                            W.Cast(tg);
                            return;
                        }
                        if (EJg && E.IsReady())
                        {

                            E.Cast(
                                Deviation(ObjectManager.Player.Position.ToVector2(), tg.Position.ToVector2(),
                                    60).ToVector3());
                            return;
                        }
                    }
                }
            }
        }

        private Menu ComboMenu;
        private MenuBool UseQCombo;
        private MenuBool UseWCombo;
        private MenuList<string> UseEMode;
        private MenuBool ForceR;
        private Menu HarassMenu;
        private MenuBool UseQExtended;
        private MenuSlider QExManaPercent;
        private MenuBool UseQHarass;
        private MenuBool UsePassiveOnEnemy;
        private Menu JungleMenu;
        private MenuBool QJg;
        private MenuBool WJg;
        private MenuBool EJg;
        private MenuBool QKS;

        public void InitMenu()
        {
            ComboMenu = MainMenu.Add(new Menu("Luciancombomenu", "Combo Settings: "));
            UseQCombo = ComboMenu.Add(new MenuBool("Lucianqcombo", "Use Q", true));
            UseWCombo = ComboMenu.Add(new MenuBool("Lucianwcombo", "Use W", true));
            UseEMode =
                ComboMenu.Add(new MenuList<string>("Lucianecombo", "E Mode", new[] {"Side", "Cursor", "Enemy", "Never"}));
            ForceR = ComboMenu.Add(new MenuBool("Lucianrcombo", "Auto R", true));
            HarassMenu = MainMenu.Add(new Menu("Lucianharassmenu", "Harass Settings: "));
            UseQExtended = HarassMenu.Add(new MenuBool("Lucianqextended", "Use Extended Q", true));
            QExManaPercent =
                HarassMenu.Add(new MenuSlider("Lucianqexmanapercent", "Only use extended Q if mana > %", 75, 0, 100));
            UseQHarass = HarassMenu.Add(new MenuBool("Lucianqharass", "Use Q Harass", true));
            UsePassiveOnEnemy = HarassMenu.Add(new MenuBool("Lucianpassivefocus", "Use Passive On Champions", true));
            JungleMenu = MainMenu.Add(new Menu("Lucianjunglemenu", "Jungle Settings: "));
            QJg = JungleMenu.Add(new MenuBool("Lucianqjungle", "Use Q", true));
            WJg = JungleMenu.Add(new MenuBool("Lucianwjungle", "Use W", true));
            EJg = JungleMenu.Add(new MenuBool("Lucianejungle", "Use E", true));
            QKS = new MenuBool("Lucianqks", "Use Q for KS", true);
            MainMenu.Attach();
        }

        public static Vector2 Deviation(Vector2 point1, Vector2 point2, double angle)
        {
            angle *= Math.PI/180.0;
            Vector2 temp = Vector2.Subtract(point2, point1);
            Vector2 result = new Vector2(0);
            result.X = (float) (temp.X*Math.Cos(angle) - temp.Y*Math.Sin(angle))/4;
            result.Y = (float) (temp.X*Math.Sin(angle) + temp.Y*Math.Cos(angle))/4;
            result = Vector2.Add(result, point1);
            return result;
        }

        public bool HasPassive => ObjectManager.Player.HasBuff("LucianPassiveBuff");
    }
}