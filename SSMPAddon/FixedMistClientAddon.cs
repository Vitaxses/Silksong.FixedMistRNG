using SSMP.Api.Client;
using SSMP.Api.Client.Networking;
using SSMP.Networking.Packet;

using UnityEngine.SceneManagement;

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

    public int CurrentSeed { get; private set; }
    public bool AdjustSeed { get; private set; }

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
            AdjustSeed = packetData.AdjustSeed;
            MazeController newestInstance = MazeController.NewestInstance;
            if (newestInstance && newestInstance.IsCapScene)
            {
                MazeControllerPatches.Postfix_Activate();
                MazeControllerPatches.SaveAndInitRngState(newestInstance);
                newestInstance.LinkDoors(newestInstance.entryDoors);
            }
        });
    }

    public static IPacketData? InstantiatePacket(S2CPacketId id)
    {
        return id switch
        {
            S2CPacketId.UpdateSeed => new UpdateSeedPacketData(),
            _ => null,
        };
    }

    public void SendOptionUpdate(int Seed, bool AdjustSeed)
    {
        if (CurrentSeed == Seed && this.AdjustSeed == AdjustSeed)
            return;

        if (sender == null || !IsConnected())
            return;
            
        sender.SendSingleData(C2SPacketId.UpdateSettings, new UpdateSeedPacketData() { Seed = Seed, AdjustSeed = AdjustSeed });
    }

    internal static bool IsConnected()
    {
        return Instance != null && Instance.ClientApi != null && Instance.ClientApi.NetClient.IsConnected;
    }

    internal int GetSeed()
    {
        return CurrentSeed ^ SceneManager.GetActiveScene().name.GetHashCode();
    }
}
