using Dapper;
using Plus.Database;

namespace Plus.HabboHotel.Catalog.Vouchers;

public class VoucherManager : IVoucherManager
{
    private readonly IDatabase _database;
    private readonly Dictionary<string, Voucher> _vouchers;

    public VoucherManager(IDatabase database)
    {
        _database = database;
        _vouchers = new();
    }

    public void Init()
    {
        if (_vouchers.Count > 0)
            _vouchers.Clear();
        using var db = _database.Connection();
        var rows = db.Query("SELECT `voucher`, `type`, `value`, `current_uses`, `max_uses` FROM `catalog_vouchers` WHERE `enabled` = '1'");
        foreach (var row in rows)
        {
            var code = ((string?)row.voucher) ?? string.Empty;
            _vouchers.Add(code, new(
                code,
                ((string?)row.type) ?? string.Empty,
                (int)row.value,
                (int)row.current_uses,
                (int)row.max_uses));
        }
    }

    public bool TryGetVoucher(string code, out Voucher? voucher) => _vouchers.TryGetValue(code, out voucher);

    public void UpdateUses(Voucher voucher)
    {
        voucher.IncrementUses();
        using var db = _database.Connection();
        db.Execute(
            "UPDATE `catalog_vouchers` SET `current_uses` = `current_uses` + 1 WHERE `voucher` = @code LIMIT 1",
            new { code = voucher.Code });
    }
}
