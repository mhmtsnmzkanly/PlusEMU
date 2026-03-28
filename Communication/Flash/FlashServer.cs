using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.Communication.Abstractions;
using Plus.Communication.Packets;

namespace Plus.Communication.Flash;

public class FlashServer : TcpGameServer<FlashServerConfiguration>, IFlashServer
{
    public FlashServer(IOptions<FlashServerConfiguration> options, FlashClientFactory flashClientFactory, IPacketManager packetManager, IGameClientManager gameClientManager, IDatabase database, ILogger<FlashServer> logger) : base(options, flashClientFactory, packetManager, gameClientManager, database, logger)
    {
    }
}
