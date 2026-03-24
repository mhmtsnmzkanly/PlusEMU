namespace Plus.HabboHotel.Catalog.Vouchers;

public interface IVoucherManager
{
    void Init();
    bool TryGetVoucher(string code, out Voucher? voucher);
}
