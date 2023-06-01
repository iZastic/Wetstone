using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using ProjectM;
using Wetstone.API;

namespace Wetstone.Hooks;

/// <summary>
/// Hook responsible for handling calls to IRunOnInitialized.
/// </summary>
static class OnInitialize
{
#nullable disable
    private static Harmony _harmony;
#nullable enable

    public static bool HasInitialized { get; private set; } = false;

    public static void Initialize()
    {
        _harmony = Harmony.CreateAndPatchAll(VWorld.IsServer ? typeof(ServerDetours) : typeof(ClientDetours));
    }

    public static void Uninitialize()
    {
        _harmony.UnpatchSelf();
    }

    private static void InvokePlugins()
    {
        WetstonePlugin.Logger.LogInfo("Game has bootstrapped. Worlds and systems now exist.");

        if (HasInitialized) return;
        HasInitialized = true;

        foreach (var (name, info) in IL2CPPChainloader.Instance.Plugins)
        {
            if (info.Instance is IRunOnInitialized runOnInitialized)
            {
                runOnInitialized.OnGameInitialized();
            }
        }

        foreach (var plugin in Features.Reload._loadedPlugins)
        {
            if (plugin is IRunOnInitialized runOnInitialized)
            {
                runOnInitialized.OnGameInitialized();
            }
        }
    }

    // these are intentionally different classes, even if their bodies _currently_ are the same

    private static class ServerDetours
    {
        [HarmonyPatch(typeof(GameBootstrap), nameof(GameBootstrap.Start))]
        [HarmonyPostfix]
        public static void Initialize()
        {
            InvokePlugins();
        }
    }

    private static class ClientDetours
    {
        [HarmonyPatch(typeof(CustomWorldSpawning), nameof(CustomWorldSpawning.AddSystemsToWorld))]
        [HarmonyPostfix]
        public static void Initialize()
        {
            InvokePlugins();
        }
    }
}