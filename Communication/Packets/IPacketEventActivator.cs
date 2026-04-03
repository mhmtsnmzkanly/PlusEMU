namespace Plus.Communication.Packets;

public interface IPacketEventActivator
{
    bool TryActivate(Type packetType, out IPacketEvent packetEvent);
}
