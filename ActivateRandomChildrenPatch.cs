using FixedMistRNG.SSMPAddon;
using HarmonyLib;
using UnityEngine;

namespace FixedMistRNG;

// Override which traps are enabled
[HarmonyPatch(typeof(ActivateRandomChildren), nameof(ActivateRandomChildren.OnEnable))]
internal static class ActivateRandomChildrenPatch
{
    [HarmonyPrefix]
    public static bool Prefix_OnEnable(ActivateRandomChildren __instance)
    {
        if (!FixedMistClientAddon.IsConnected() || !__instance.gameObject.scene.name.StartsWith("Dust_Maze"))
            return true;

        foreach (Transform transform in __instance.transform)
        {
            transform.gameObject.SetActive(false);
        }

        System.Random rng = new(FixedMistClientAddon.Instance!.GetSeed(__instance.gameObject));
        for (int i = rng.Next(__instance.amountMin, __instance.amountMax); i > 0f; i -= 1)
        {
            int index = rng.Next(0, __instance.transform.childCount);
            Transform child = __instance.transform.GetChild(index);
            child.gameObject.SetActive(true);
        }

        return false;
    }
}
