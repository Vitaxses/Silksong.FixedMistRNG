using System;
using System.Linq;
using FixedMistRNG.SSMPAddon;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Silksong.FsmUtil;

namespace FixedMistRNG;

[HarmonyPatch(typeof(PlayMakerFSM), nameof(PlayMakerFSM.Start))]
internal static class PlayMakerFSMPatch
{

    [HarmonyPostfix]
    private static void Postfix_Start(PlayMakerFSM __instance)
    {
        if (!FixedMistClientAddon.IsConnected())
            return;
            
        MistMazeController(__instance);
        WraithSpawner(__instance);
    }

    private static void SetNode(PlayMakerFSM fsm, FsmGameObject? nodes)
    {
        if (nodes == null || nodes.value == null)
            return;

        var transform = nodes.Value.transform;
        
        if (transform.childCount == 0)
            return;

        var nextNode = nodes.Value.transform.GetChild(0).gameObject;

        fsm.FindGameObjectVariable("Next Node")!.Value = nextNode;
    }
    
    private static void MistMazeController(PlayMakerFSM fsm)
    {
        if (fsm.FsmName != "mist_maze_controller")
            return;

        FsmState silkfliesState = fsm.GetState("Set Silkflies")!;
        silkfliesState.DisableAction(1);
        silkfliesState.InsertMethod(1, (action) =>
        {
            SetNode(fsm, fsm.FindGameObjectVariable("Silkfly Nodes"));
        });

        FsmState trapState = fsm.GetState("Set Traps")!;
        trapState.DisableAction(0);
        trapState.InsertMethod(0, (action) =>
        {
            SetNode(fsm, fsm.FindGameObjectVariable("Trap Sets"));
        });

        FsmState wraithState = fsm.GetState("Set Wraiths")!;
        wraithState.DisableAction(0);
        wraithState.InsertMethod(0, (action) =>
        {
            SetNode(fsm, fsm.FindGameObjectVariable("Wraith Nodes"));
        });
    }

    private static void WraithSpawner(PlayMakerFSM fsm)
    {
        if (fsm.FsmName != "Control" || !fsm.name.StartsWith("Wraith Summoner"))
            return;

        Random rng = new(FixedMistClientAddon.Instance!.CurrentSeed);

        FsmState chooseState = fsm.GetState("Choose")!;
        var sendRandom = chooseState.GetAction<SendRandomEvent>(0)!;
        var originalEvents = sendRandom.events;
        var remaining = originalEvents.ToList();

        chooseState.DisableAction(0);

        chooseState.AddMethod(() =>
        {
            if (remaining.Count == 0)
                remaining = originalEvents.ToList();

            int index = rng.Next(remaining.Count);
            var chosen = remaining[index];
            
            remaining.RemoveAt(index);
            fsm.SendEvent(chosen.Name);
        });
    }
}
