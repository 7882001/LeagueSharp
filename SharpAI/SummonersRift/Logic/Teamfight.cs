﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpAI.SummonersRift.Data;
using SharpAI.Utility;
using LeagueSharp;
using LeagueSharp.SDK;
using LeagueSharp.SDK.Enumerations;
using TreeSharp;
using Action = TreeSharp.Action;

namespace SharpAI.SummonersRift.Logic
{
    public static class Teamfight
    {
        static bool ShouldTakeAction()
        {
            return ObjectManager.Get<Obj_AI_Hero>().Any(h => !h.IsDead && h.IsAlly && !h.InFountain() && h.Position.CountAllyHeroesInRange(1000) >2);
        }

        static TreeSharp.Action TakeAction()
        {
            Logging.Log("SWITCHED MODE TO TEAMFIGHT");
            return new Action(a =>
            {
                Variables.Orbwalker.ForceOrbwalkingPoint = Positioning.GetTeamfightPosition();
                Variables.Orbwalker.Enabled = true;
                Variables.Orbwalker.ActiveMode = OrbwalkingMode.LaneClear;
            });
        }

        public static Composite BehaviorComposite => new Decorator(t => ShouldTakeAction(), TakeAction());
    }
}