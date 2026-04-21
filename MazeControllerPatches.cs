using System.Collections.Generic;
using System.Linq;
using FixedMistRNG.SSMPAddon;
using HarmonyLib;

namespace FixedMistRNG;

[HarmonyPatch(typeof(MazeController))]
internal static class MazeControllerPatches
{
    private static System.Random? rng;

    [HarmonyPatch(nameof(MazeController.LinkDoors))]
    [HarmonyPrefix]
    private static bool LinkDoors(MazeController __instance, IReadOnlyList<TransitionPoint> totalDoors)
    {
        if (!FixedMistClientAddon.IsConnected() || rng == null)
            return true;

        __instance.correctDoors.Clear();

        var validDoors = totalDoors.Select(door =>
        {
            if (door == null)
                return null;

            string name = door.name;
            string doorDirMatch = __instance.GetDoorDirMatch(name);
            if (string.IsNullOrEmpty(doorDirMatch))
                return null;

            return new
            {
                Door = door,
                DoorName = name,
                TargetDoorMatch = doorDirMatch
            };
        }).Where(x => x != null).ToList();

        var teleportMap = SceneTeleportMap.GetTeleportMap();
        var possibleScenes = teleportMap.Where(kvp => kvp.Key != __instance.gameObject.scene.name && __instance.sceneNames.Contains(kvp.Key)).ToList();

        PlayerData pd = PlayerData.instance;

        int doorCount = validDoors.Count;
        int correctDoor = pd.hasNeedolin ? rng.Next(0, doorCount) : -1; // this of course causes the layout to be different

        validDoors.Shuffle(rng);

        int requiredCorrectDoors = __instance.neededCorrectDoors - 1;

        if (!__instance.isCapScene)
        {
            __instance.specialLinkDoors.Clear();
            string sceneName;

            if (pd.CorrectMazeDoorsEntered >= requiredCorrectDoors)
            {
                sceneName = __instance.exitSceneName;

                if (__instance.forceExit)
                {
                    __instance.specialLinkDoors.AddRange(teleportMap[__instance.exitSceneName].TransitionGates);
                }
                else
                {
                    MazeController.EntryMatch exitMatch = __instance.GetExitMatch();
                    if (exitMatch != null)
                    {
                        __instance.specialLinkDoors.AddRange(teleportMap[__instance.exitSceneName].TransitionGates
                            .Where(gate => gate.StartsWith(exitMatch.ExitDoorDir)).ToList());
                    }
                    else
                    {
                        __instance.specialLinkDoors.AddRange(teleportMap[__instance.exitSceneName].TransitionGates);
                    }
                }
            }
            else
            {
                if (pd.CorrectMazeDoorsEntered < __instance.restScenePoint - 1 || pd.EnteredMazeRestScene)
                {
                    goto SkipSpecialDoors;
                }

                sceneName = __instance.restSceneName;
                __instance.specialLinkDoors.AddRange(teleportMap[__instance.restSceneName].TransitionGates);
            }

            __instance.specialLinkDoors.Shuffle(rng);

            for (int i = validDoors.Count - 1; i >= 0; i--)
            {
                var item = validDoors[i];
                TransitionPoint door = item!.Door;
                string doorName = item.DoorName;

                if (string.IsNullOrEmpty(pd.PreviousMazeTargetDoor) || pd.PreviousMazeTargetDoor != doorName)
                {
                    bool matched = false;

                    foreach (string name in __instance.specialLinkDoors)
                    {
                        if (__instance.TryMatchDoor(sceneName, name, item.TargetDoorMatch, door, true))
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (matched)
                    {
                        correctDoor = -1;
                        validDoors.RemoveAt(i);
                        doorCount--;
                        break;
                    }
                }
            }

            SkipSpecialDoors:
                __instance.specialLinkDoors.Clear();
        }

        for (int j = 0; j < doorCount; j++)
        {
            var doorObj = validDoors[j];
            TransitionPoint door = doorObj!.Door;
            string name = doorObj.DoorName;

            if (!string.IsNullOrEmpty(pd.PreviousMazeTargetDoor) && name == pd.PreviousMazeTargetDoor)
            {
                door.SetTargetScene(pd.PreviousMazeScene);
                door.entryPoint = pd.PreviousMazeDoor;

                if (pd.PreviousMazeScene == __instance.exitSceneName)
                {
                    correctDoor = j;
                    __instance.correctDoors.Clear();
                }
                else if (pd.DidEnterPreviousMazeDoor)
                {
                    correctDoor = j;
                    __instance.correctDoors.Clear();
                }
                else if (doorCount > 1)
                {
                    while (correctDoor == j)
                    {
                        correctDoor = rng.Next(0, doorCount);
                    }
                }

                if (correctDoor == j)
                    __instance.correctDoors.Add(door);
            }
            else
            {
                possibleScenes.Shuffle(rng);

                if (!string.IsNullOrEmpty(pd.PreviousMazeScene))
                {
                    for (int k = possibleScenes.Count - 1; k >= 0; k--)
                    {
                        var kvp = possibleScenes[k];
                        if (kvp.Key == pd.PreviousMazeScene)
                        {
                            possibleScenes.RemoveAt(k);
                            possibleScenes.Add(kvp);
                        }
                    }
                }

                foreach (var kvp in possibleScenes)
                    if (__instance.TryFindMatchingDoor(kvp.Key, kvp.Value.TransitionGates, doorObj.TargetDoorMatch, door, correctDoor == j))
                        break;
            }
        }

        if (__instance.isCapScene || __instance.correctDoors.Count > 0 || correctDoor < 0)
        {
            return false;
        }

        __instance.LinkDoors(totalDoors);
        return false;
    }

    [HarmonyPatch(typeof(MazeController), nameof(MazeController.Activate))]
    [HarmonyPrefix]
    private static void Prefix_Activate(MazeController __instance)
    {
        if (FixedMistClientAddon.IsConnected())
            rng = new System.Random(FixedMistClientAddon.Instance!.GetSeed(__instance.gameObject));
    }

    public static void Shuffle<T>(this IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int k = rng.Next(i + 1);
            (list[i], list[k]) = (list[k], list[i]);
        }
    }

    /* Unused stuff
    
    private static Random.State lastState;

    [HarmonyPatch(typeof(MazeController), nameof(MazeController.Activate))]
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
        Random.InitState(FixedMistClientAddon.Instance!.GetSeed(mc.gameObject));
    }
    
    [HarmonyPatch(typeof(MazeController), nameof(MazeController.Activate))]
    [HarmonyPostfix]
    public static void Postfix_Activate()
    {
        if (!FixedMistClientAddon.IsConnected())
            return;
            
        Random.state = lastState;
    }*/
}
