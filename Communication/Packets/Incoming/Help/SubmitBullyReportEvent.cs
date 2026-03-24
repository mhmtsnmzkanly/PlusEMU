using Plus.Communication.Packets.Outgoing.Help;
using Plus.HabboHotel.GameClients;
using Plus.Utilities;

namespace Plus.Communication.Packets.Incoming.Help;

internal class SubmitBullyReportEvent : IPacketEvent
{
    private readonly IGameClientManager _clientManager;

    public SubmitBullyReportEvent(IGameClientManager clientManager)
    {
        _clientManager = clientManager;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        //0 = sent, 1 = blocked, 2 = no chat, 3 = already reported.
        var habbo = session.GetHabbo();
        var userId = packet.ReadInt();
        if (userId == habbo.Id) //Hax
            return Task.CompletedTask;
        if (habbo.AdvertisingReportedBlocked)
        {
            session.Send(new SubmitBullyReportComposer(1)); //This user is blocked from reporting.
            return Task.CompletedTask;
        }
        var client = _clientManager.GetClientByUserId(Convert.ToInt32(userId));
        if (client == null)
        {
            session.Send(new SubmitBullyReportComposer(0)); //Just say it's sent, the user isn't found.
            return Task.CompletedTask;
        }
        if (habbo.LastAdvertiseReport > UnixTimestamp.GetNow())
        {
            session.SendNotification("Reports can only be sent per 5 minutes!");
            return Task.CompletedTask;
        }
        var targetHabbo = client.GetHabbo();
        if (targetHabbo == null)
            return Task.CompletedTask;
        if (targetHabbo.Permissions?.HasRight("mod_tool") == true) //Reporting staff, nope!
        {
            session.SendNotification("Sorry, you cannot report staff members via this tool.");
            return Task.CompletedTask;
        }

        //This user hasn't even said a word, nope!
        if (!targetHabbo.HasSpoken)
        {
            session.Send(new SubmitBullyReportComposer(2));
            return Task.CompletedTask;
        }

        //Already reported, nope.
        if (targetHabbo.AdvertisingReported && habbo.Rank < 2)
        {
            session.Send(new SubmitBullyReportComposer(3));
            return Task.CompletedTask;
        }
        if (habbo.Rank <= 1)
            habbo.LastAdvertiseReport = UnixTimestamp.GetNow() + 300;
        else
            habbo.LastAdvertiseReport = UnixTimestamp.GetNow();
        targetHabbo.AdvertisingReported = true;
        session.Send(new SubmitBullyReportComposer(0));
        //_clientManager.ModAlert("New advertising report! " + Client.GetHabbo().Username + " has been reported for advertising by " + Session.GetHabbo().Username +".");
        _clientManager.DoAdvertisingReport(session, client);
        return Task.CompletedTask;
    }
}
