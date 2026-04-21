using SSMP.Networking.Packet;

public class UpdateSeedPacketData : IPacketData
{
    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;

    public int Seed { get; set; }

    public void ReadData(IPacket packet)
    {
        Seed = packet.ReadInt();
    }

    public void WriteData(IPacket packet)
    {
        packet.Write(Seed);
    }
}
