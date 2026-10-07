using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.NET.Common;
using BepInExResoniteShim;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using Renderite.Shared;

namespace InspectorScroll;

[ResonitePlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION, PluginMetadata.AUTHORS, PluginMetadata.REPOSITORY_URL)]
[BepInDependency(BepInExResoniteShim.PluginMetadata.GUID, BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BasePlugin
{
#nullable disable
    internal static new ManualLogSource Log;
    internal static ConfigEntry<bool> ScrollWhileWalking;
    internal static ConfigEntry<float> Speed;
    internal static ConfigEntry<bool> IndexJoystick;
    internal static ConfigEntry<bool> BlockInputWorld;
    internal static ConfigEntry<bool> BlockInputUser;
#nullable enable

    public override void Load()
    {
        Log = base.Log;
        
        ScrollWhileWalking = Config.Bind("General", "ScrollWhileWalking", true, "Always allows scrolling, even if it means you'd walk and scroll at the same time.");
        Speed = Config.Bind("General", "Speed", 120f, "How fast you scroll, default is 120.");
        IndexJoystick = Config.Bind("General", "IndexJoystick", false, "Use the joystick on index controller.");
        BlockInputWorld = Config.Bind("General", "BlockInputWorld", false, "Prevent moving when hovering over a scrollable element in world space.");
        BlockInputUser = Config.Bind("General", "BlockInputUser", false, "Prevent moving when hovering over a scrollable element in userspace.");
        
        HarmonyInstance.PatchAll();
    }
    
    [HarmonyPatch(typeof(InteractionHandler))]
	[HarmonyPatch("OnInputUpdate")]
	class InteractionHandler_OnInputUpdate_Patch
	{
		public static void Postfix(InteractionHandler __instance)
		{
			if (__instance.InputInterface.ScreenActive) return;

			var index = __instance.InputInterface.GetControllerNode(__instance.Side) as IndexController;
			if (ScrollWhileWalking.Value || (index != null && !IndexJoystick.Value) || __instance.Inputs.Axis.RegisterBlocks)
			{
				float2 val;
				if (index != null && IndexJoystick.Value)
				{
					val = index.Joystick.Value;
				}
				else
				{
					val = __instance.Inputs.Axis.Value.Value;
				}
				val *= new float2(-1, 1);
				
				IAxisActionReceiver? axisActionReceiver = __instance.Laser.CurrentTouchable as IAxisActionReceiver;
				axisActionReceiver?.ProcessAxis(__instance.Laser.TouchSource, val * Speed.Value);
			}
		}
	}
	
	static bool ShouldAttemptInputBlock()
	{
		return BlockInputWorld.Value || BlockInputUser.Value;
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
		static InteractionHandler? userSpaceHandlerLeft;
		static InteractionHandler? userSpaceHandlerRight;
		
		// some code from Nytra's [NoTankControls fork](https://github.com/Nytra/NoTankControls)
		private static void Postfix(InteractionHandler __instance, ref CommonActionsInputs ____laserHoldInputs)
		{
			if (!ShouldAttemptInputBlock()) return;
			
			if (__instance.World == Engine.Current.WorldManager.FocusedWorld)
			{
				var worldScroll = BlockInputWorld.Value && CanScroll(__instance);
				var userScroll = (__instance.Side == Chirality.Left && CanScroll(userSpaceHandlerLeft))
				                  || (__instance.Side == Chirality.Right && CanScroll(userSpaceHandlerRight));
				if (worldScroll || BlockInputUser.Value && userScroll)
				{
                                  var index = __instance.InputInterface.GetControllerNode(__instance.Side) as IndexController;
                                  if (index == null || IndexJoystick.Value)
                                        ____laserHoldInputs.Active = true;
				}
			}
			else if (__instance.World == Userspace.UserspaceWorld)
			{
				// Required for NoTankControls+InspectorScroll
				if(BlockInputUser.Value)
					__instance.Inputs.Axis.RegisterBlocks = true;
				
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
