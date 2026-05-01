using System.Collections.Generic;

using FixedMistRNG.SSMPAddon.Command.Server;
using SSMP.Api.Server;
using SSMP.Api.Server.Networking;
using SSMP.Networking.Packet;

namespace FixedMistRNG.SSMPAddon;

public class FixedMistServerAddon : ServerAddon
{
    public override bool NeedsNetwork => true;

    public override uint ApiVersion => 1u;
    protected override string Name => AddonIdentifiers.NAME;
    protected override string Version => AddonIdentifiers.VERSION;

    private IServerApi? api;
    private IServerAddonNetworkSender<S2CPacketId>? sender;
    private IServerAddonNetworkReceiver<C2SPacketId>? receiver;

    private List<ushort> playersInMist = [];
    private Random? rng;

    public int CurrentSeed { get; private set; }
    private bool AdjustSeed;

    public override void Initialize(IServerApi Api)
    {
        api = Api;

        api.CommandManager.RegisterCommand(new SetSeedCommand(this));
        
        sender = api.NetServer.GetNetworkSender<S2CPacketId>(this);
        receiver = api.NetServer.GetNetworkReceiver<C2SPacketId>(this, InstantiatePacket!);
        
        RegisterPacketHandlers();

        api.ServerManager.PlayerDisconnectEvent += player => playersInMist.Remove(player.Id);
        api.ServerManager.PlayerConnectEvent += OnPlayerConnect;
        api.ServerManager.PlayerEnterSceneEvent += OnPlayerEnterScene;
    }

    private void OnPlayerEnterScene(IServerPlayer player)
    {
        string CurrentScene = player.CurrentScene;
        if (IsMist(CurrentScene) && !playersInMist.Contains(player.Id))
        {
            playersInMist.Add(player.Id);
        } else if (!IsMist(CurrentScene))
        {
            if (playersInMist.Remove(player.Id))
                CheckForMistUpdate();
        }
    }

    private void OnPlayerConnect(IServerPlayer player)
    {
        if (IsMist(player.CurrentScene) && !playersInMist.Contains(player.Id))
        {
            playersInMist.Add(player.Id);
        }
            
        sender?.SendSingleData(S2CPacketId.UpdateSeed, new UpdateSeedPacketData() { Seed = CurrentSeed, AdjustSeed = AdjustSeed }, player.Id);
    }

    private void RegisterPacketHandlers()
    {
        receiver?.RegisterPacketHandler<UpdateSeedPacketData>(C2SPacketId.UpdateSettings, (id, packetData) =>
        {
            var playerSender = api!.ServerManager.GetPlayer(id)!;
            if (!playerSender.IsAuthorized)
            {
                return;
            }

            CurrentSeed = packetData.Seed;
            AdjustSeed = packetData.AdjustSeed;
            api.ServerManager.BroadcastMessage($"{playerSender.Username} updated FixedMistRNG settings.");
            sender?.BroadcastSingleData(S2CPacketId.UpdateSeed, new UpdateSeedPacketData() { Seed = CurrentSeed, AdjustSeed = AdjustSeed });
        });
    }

    public IPacketData? InstantiatePacket(C2SPacketId id)
    {
        return id switch
        {
            C2SPacketId.UpdateSettings => new UpdateSeedPacketData(),
            _ => null,
        };
    }

    public void CheckForMistUpdate()
    {
        if (!AdjustSeed || playersInMist.Count > 0)
        {
            return;
        }

        SetSeed(GenerateNewSeed());
    }

    public int GenerateNewSeed()
    {
        rng ??= new(DateTime.UtcNow.Millisecond);
        return rng.Next(1001);
    }

    public void SetSeed(int value)
    {
        CurrentSeed = value;
        sender?.BroadcastSingleData(S2CPacketId.UpdateSeed, new UpdateSeedPacketData() { Seed = CurrentSeed, AdjustSeed = AdjustSeed });
    }

    public bool IsMist(string scene)
    {
        return scene.StartsWith("Dust_Maze") && !scene.EndsWith("_Last_Hall") && !scene.EndsWith("_entrance");
    }
}
