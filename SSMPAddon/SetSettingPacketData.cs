using SSMP.Networking.Packet;

public class SetSettingPacketData : IPacketData
{
    public bool IsReliable => true;

    public bool DropReliableDataIfNewerExists => true;

    public bool AdjustSeed { get; set; }

    public void ReadData(IPacket packet)
    {
        AdjustSeed = packet.ReadBool();
    }

    public void WriteData(IPacket packet)
    {
        packet.Write(AdjustSeed);
    }
}
