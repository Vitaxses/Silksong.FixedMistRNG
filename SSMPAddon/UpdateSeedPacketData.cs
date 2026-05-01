using SSMP.Networking.Packet;

public class UpdateSeedPacketData : IPacketData
{
    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;

    public int Seed { get; set; }
    public bool AdjustSeed { get; set; }

    public void ReadData(IPacket packet)
    {
        Seed = packet.ReadInt();
        AdjustSeed = packet.ReadBool();
    }

    public void WriteData(IPacket packet)
    {
        packet.Write(Seed);
        packet.Write(AdjustSeed);
    }
}
