using System.Reflection;
using Elements.Core;
using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;
using Renderite.Shared;

namespace InspectorScroll;

public class InspectorScroll : ResoniteMod
{
	public override string Name => "InspectorScroll";
	public override string Author => "art0007i";
	public override string Version => "2.0.2";
	public override string Link => "https://github.com/art0007i/InspectorScroll/";

	[AutoRegisterConfigKey]
	public static ModConfigurationKey<float> KEY_SPEED = new("scroll_speed", "How fast you scroll, default is 120.", () => 120);
	[AutoRegisterConfigKey]
	public static ModConfigurationKey<bool> KEY_INDEX_JOYSTICK = new("index_joystick", "Use the joystick on index controller.", () => false);
	[AutoRegisterConfigKey]
	public static ModConfigurationKey<bool> KEY_BLOCK_INPUT_WORLD = new("block_input_world", "Prevent moving when hovering over a scrollable element in world space.", () => false);
	[AutoRegisterConfigKey]
	public static ModConfigurationKey<bool> KEY_BLOCK_INPUT_USER = new("block_input_user", "Prevent moving when hovering over a scrollable element in userspace.", () => false);
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
			if (__instance.InputInterface.ScreenActive) return;
				
			if (__instance.Inputs.Axis.RegisterBlocks)
			{
				float2 val;
				if (config.GetValue(KEY_INDEX_JOYSTICK) && 
				    __instance.InputInterface.GetControllerNode(__instance.Side) is IndexController index)
				{
					val = index.Joystick.Value;
				}
				else
				{
					val = __instance.Inputs.Axis.Value.Value;
				}
				val *= new float2(-1, 1);
				
				IAxisActionReceiver? axisActionReceiver = __instance.Laser.CurrentTouchable as IAxisActionReceiver;
				axisActionReceiver?.ProcessAxis(__instance.Laser.TouchSource, val * config.GetValue(KEY_SPEED));
			}
		}
	}
	
	private static readonly FieldInfo InteractionHandlerInputs =
		AccessTools.Field(typeof(InteractionHandler), "_inputs");

	static bool ShouldAttemptInputBlock()
	{
		return config.GetValue(KEY_BLOCK_INPUT_WORLD) || config.GetValue(KEY_BLOCK_INPUT_USER);
	}
	
	static bool CanScroll(InteractionHandler? __instance)
	{
		if (__instance == null) return false;
		var axisActionReceiver = __instance.Laser.CurrentTouchable as IAxisActionReceiver;
		if (axisActionReceiver == null) return false;

		var block = ShouldAttemptInputBlock() &&
		            axisActionReceiver.ProcessAxis(__instance.Laser.TouchSource, float2.Zero);

		return block;
	}
	
	[HarmonyPatch(typeof(InteractionHandler))]
	[HarmonyPatch("BeforeInputUpdate")]
	[HarmonyAfter("U-xyla.XyMod", "owo.Nytra.NoTankControls")]
	class InputBlockPatch
	{
		static InteractionHandler? userSpaceHandlerLeft = null;
		static InteractionHandler? userSpaceHandlerRight = null;
		
		// some code from Nytra's [NoTankControls fork](https://github.com/Nytra/NoTankControls)
		private static void Postfix(InteractionHandler __instance)
		{
			if (!ShouldAttemptInputBlock()) return;
			
			if (__instance.World == Engine.Current.WorldManager.FocusedWorld)
			{
				var worldScroll = config.GetValue(KEY_BLOCK_INPUT_WORLD) && CanScroll(__instance);
				var userScroll = (__instance.Side == Chirality.Left && CanScroll(userSpaceHandlerLeft))
				                  || (__instance.Side == Chirality.Right && CanScroll(userSpaceHandlerRight));
				if (worldScroll || (config.GetValue(KEY_BLOCK_INPUT_USER) && userScroll))
				{
					__instance.Inputs.Axis.RegisterBlocks = true;
				}
			}
			else if (__instance.World == Userspace.UserspaceWorld)
			{
				if (__instance.Side == Chirality.Left)
				{
					if (userSpaceHandlerLeft.FilterWorldElement() == null)
					{
						userSpaceHandlerLeft = __instance;
					}
				}
				else
				{
					if (userSpaceHandlerRight.FilterWorldElement() == null)
					{
						userSpaceHandlerRight = __instance;
					}
				}
			}
		}
	}
}
