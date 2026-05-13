using Random = UnityEngine.Random;

namespace FixedMistRNG;

[HarmonyPatch(typeof(MazeController))]
internal static class MazeControllerPatches
{
    private static Random.State lastState;

    [HarmonyPatch(nameof(MazeController.Activate))]
    [HarmonyPrefix]
    private static void Prefix_Activate(MazeController __instance)
    {
        SaveAndInitRngState(__instance);
    }

    public static void SaveAndInitRngState(MazeController mc)
    {
        if (!FixedMistClientAddon.IsConnected())
            return;

        lastState = Random.state;
        Random.InitState(FixedMistClientAddon.Instance!.GetSeed());
    }
    
    [HarmonyPatch(nameof(MazeController.Activate))]
    [HarmonyPostfix]
    public static void Postfix_Activate()
    {
        if (!FixedMistClientAddon.IsConnected())
            return;
            
        Random.state = lastState;
    }
}
