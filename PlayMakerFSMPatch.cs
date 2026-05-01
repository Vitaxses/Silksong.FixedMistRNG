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

    private static void SetNode(PlayMakerFSM fsm, FsmGameObject? nodes, Random rng)
    {
        if (nodes == null || nodes.Value == null)
            return;

        var transform = nodes.Value.transform;
        
        if (transform.childCount == 0)
            return;

        var nextNode = transform.GetChild(rng.Next(transform.childCount)).gameObject;

        fsm.FindGameObjectVariable("Next Node")!.Value = nextNode;
    }
    
    private static void MistMazeController(PlayMakerFSM fsm)
    {
        if (fsm.FsmName != "mist_maze_controller")
            return;

        Random rng = new(FixedMistClientAddon.Instance!.GetSeed());

        FsmState silkfliesState = fsm.GetState("Set Silkflies")!;
        silkfliesState.DisableAction(1);
        var silkFlyNodes = fsm.FindGameObjectVariable("Silkfly Nodes");
        silkfliesState.InsertMethod(1, (action) =>
        {
            SetNode(fsm, silkFlyNodes, rng);
        });

        FsmState trapState = fsm.GetState("Set Traps")!;
        trapState.DisableAction(0);
        var traps = fsm.FindGameObjectVariable("Trap Sets");
        trapState.InsertMethod(0, (action) =>
        {
            SetNode(fsm, traps, rng);
        });

        FsmState wraithState = fsm.GetState("Set Wraiths")!;
        wraithState.DisableAction(0);
        var wraithNodes = fsm.FindGameObjectVariable("Wraith Nodes");
        wraithState.InsertMethod(0, (action) =>
        {
            SetNode(fsm, wraithNodes, rng);
        });
    }

    private static void WraithSpawner(PlayMakerFSM fsm)
    {
        if (fsm.FsmName != "Control" || !fsm.name.StartsWith("Wraith Summoner"))
            return;

        Random rng = new(FixedMistClientAddon.Instance!.GetSeed());

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
