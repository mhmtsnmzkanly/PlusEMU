using Dapper;
using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Database;
using Plus.HabboHotel.Catalog.Vouchers;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Catalog;

public class RedeemVoucherEvent : IPacketEvent
{
    private readonly IVoucherManager _voucherManager;
    private readonly IDatabase _database;

    public RedeemVoucherEvent(IVoucherManager voucherManager, IDatabase database)
    {
        _voucherManager = voucherManager;
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null) return Task.CompletedTask;
        var code = packet.ReadString().Replace("\r", "");
        if (!_voucherManager.TryGetVoucher(code, out var voucher) || voucher == null)
        {
            session.Send(new VoucherRedeemErrorComposer(0));
            return Task.CompletedTask;
        }
        if (voucher.CurrentUses >= voucher.MaxUses)
        {
            session.SendNotification("Oops, this voucher has reached the maximum usage limit!");
            return Task.CompletedTask;
        }
        using var db = _database.Connection();
        var already = db.QueryFirstOrDefault(
            "SELECT `user_id` FROM `user_vouchers` WHERE `user_id` = @userId AND `voucher` = @voucher LIMIT 1",
            new { userId = habbo.Id, voucher = code });
        if (already != null)
        {
            session.SendNotification("You've already used this voucher code, one per each user, sorry!");
            return Task.CompletedTask;
        }
        db.Execute("INSERT INTO `user_vouchers` (`user_id`, `voucher`) VALUES (@userId, @voucher)", new { userId = habbo.Id, voucher = code });
        voucher.UpdateUses();
        if (voucher.Type == VoucherType.Credit)
        {
            habbo.Credits += voucher.Value;
            session.Send(new CreditBalanceComposer(habbo.Credits));
        }
        else if (voucher.Type == VoucherType.Ducket)
        {
            habbo.Duckets += voucher.Value;
            session.Send(new HabboActivityPointNotificationComposer(habbo.Duckets, voucher.Value));
        }
        session.Send(new VoucherRedeemOkComposer());
        return Task.CompletedTask;
    }
}
