using Celeste;
using Celeste.Mod.Entities;
using MonoMod.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vitmod
{
    [CustomEntity("vitellary/canceltimecrystaltrigger")]
    public class CancelTimeCrystalTrigger : Trigger
    {
        public CancelTimeCrystalTrigger(EntityData data, Vector2 offset) : base(data, offset) { }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            TimeCrystal.stopTimer = 2f;
            TimeCrystal.stopStage = 2;
            VitModule.timeStopType = TimeCrystal.FreezeTypes.Timer; //hacky thing to get it to resume time normally
            VitModule.timeStopScaleTimer = TimeCrystal.timeScaleToSet;
        }
    }
}
