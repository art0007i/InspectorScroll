using HarmonyLib;
using NeosModLoader;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using FrooxEngine;
using FrooxEngine.LogiX;

namespace InspectorScroll
{
    public class InspectorScroll : NeosMod
    {
        public override string Name => "InspectorScroll";
        public override string Author => "art0007i";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/art0007i/InspectorScroll/";
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("me.art0007i.InspectorScroll");
            harmony.PatchAll();

        }

		[HarmonyPatch(typeof(CommonTool))]
		[HarmonyPatch("OnInputUpdate")]
		class CommonTool_OnInputUpdate_Patch
		{
			public static void Postfix(CommonTool __instance)
			{
				IAxisActionReceiver axisActionReceiver = __instance.Laser.CurrentTouchable as IAxisActionReceiver;
				if (axisActionReceiver != null)
				{
					if (((__instance.ActiveToolTip != null && __instance.ActiveToolTip.UsesSecondary && !__instance.World.IsUserspace()) || __instance.InputInterface.GetControllerNode(__instance.Side).GetType() == typeof(IndexController)) && !__instance.InputInterface.ScreenActive)
					{
						var val = (AccessTools.Field(__instance.GetType(), "_inputs").GetValue(__instance) as CommonToolInputs).Axis.Value.Value;
						val *= new BaseX.float2(-1, 1);
						axisActionReceiver.ProcessAxis(__instance.Laser.TouchSource, val * 120);
					}
				}
			}
		}
	}
}