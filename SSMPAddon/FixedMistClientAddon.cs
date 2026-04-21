using System;
using SSMP.Api.Client;
using SSMP.Api.Client.Networking;
using SSMP.Networking.Packet;
using UnityEngine;

namespace FixedMistRNG.SSMPAddon;

public class FixedMistClientAddon : ClientAddon
{
    internal static FixedMistClientAddon? Instance;
    public override bool NeedsNetwork => true;

    public override uint ApiVersion => 1u;
    protected override string Name => AddonIdentifiers.NAME;
    protected override string Version => AddonIdentifiers.VERSION;

    private IClientAddonNetworkSender<C2SPacketId>? sender;
    private IClientAddonNetworkReceiver<S2CPacketId>? receiver;

    public bool ReceivedSettingUpdate { get; set; }
    public int CurrentSeed { get; private set; }

    public override void Initialize(IClientApi clientApi)
    {
        Instance = this;

        sender = clientApi.NetClient.GetNetworkSender<C2SPacketId>(this);
        receiver = clientApi.NetClient.GetNetworkReceiver<S2CPacketId>(this, InstantiatePacket!);
        
        RegisterPacketHandlers();
    }

    private void RegisterPacketHandlers()
    {       
        receiver?.RegisterPacketHandler<UpdateSeedPacketData>(S2CPacketId.UpdateSeed, packetData =>
        {
            CurrentSeed = packetData.Seed;
        });

        receiver?.RegisterPacketHandler<SetSettingPacketData>(S2CPacketId.SetSetting, packetData =>
        {
            ReceivedSettingUpdate = true;
            FixedMistPlugin.AdjustSeed.Value = packetData.AdjustSeed;
        });
    }

    public IPacketData? InstantiatePacket(S2CPacketId id)
    {
        return id switch
        {
            S2CPacketId.UpdateSeed => new UpdateSeedPacketData(),
            S2CPacketId.SetSetting => new SetSettingPacketData(),
            _ => null,
        };
    }

    public void SendOptionUpdate()
    {
        if (sender != null && IsConnected())
            sender.SendSingleData(C2SPacketId.UpdateSetting, new SetSettingPacketData() { AdjustSeed = FixedMistPlugin.AdjustSeed.Value });
    }

    internal static bool IsConnected()
    {
        return Instance != null && Instance.ClientApi != null && Instance.ClientApi.NetClient.IsConnected;
    }

    internal int GetSeed(GameObject go)
    {
        return CurrentSeed ^ go.scene.name.GetHashCode();
    }
}
