using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;

namespace InspectorScroll
{
    public class InspectorScroll : ResoniteMod
    {
        public override string Name => "InspectorScroll";
        public override string Author => "art0007i";
        public override string Version => "2.0.0";
        public override string Link => "https://github.com/art0007i/InspectorScroll/";

		[AutoRegisterConfigKey]
		public static ModConfigurationKey<float> KEY_SPEED = new ModConfigurationKey<float>("scroll_speed", "How fast you scroll, default is 120.", () => 120);
		public static ModConfiguration config;

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("me.art0007i.InspectorScroll");
            harmony.PatchAll();
			config = GetConfiguration();
        }

		[HarmonyPatch(typeof(InteractionHandler))]
		[HarmonyPatch("OnInputUpdate")]
		class InteractionHandler_OnInputUpdate_Patch
		{
			public static void Postfix(InteractionHandler __instance)
			{
				IAxisActionReceiver axisActionReceiver = __instance.Laser.CurrentTouchable as IAxisActionReceiver;
				if (axisActionReceiver != null)
				{
					if (((__instance.ActiveTool != null && __instance.ActiveTool.UsesSecondary) || __instance.InputInterface.GetControllerNode(__instance.Side).GetType() == typeof(IndexController)) && !__instance.InputInterface.ScreenActive)
					{
						var val = (AccessTools.Field(__instance.GetType(), "_inputs").GetValue(__instance) as InteractionHandlerInputs).Axis.Value.Value;
						val *= new Elements.Core.float2(-1, 1);
						axisActionReceiver.ProcessAxis(__instance.Laser.TouchSource, val * config.GetValue(KEY_SPEED));
					}
				}
			}
		}
	}
}