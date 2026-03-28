using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired;

public interface IWiredItem : IWiredExecutable
{
    Room Instance { get; set; }
    Item Item { get; set; }
    WiredBoxType Type { get; }
    ConcurrentDictionary<uint, Item> SetItems { get; set; }
    string StringData { get; set; }
    bool BoolData { get; set; }
    string ItemsData { get; set; }
    void HandleSave(IIncomingPacket packet);
    [Obsolete("Use IWiredExecutable.Execute(WiredExecutionContext) or WiredExecutionAdapter helpers instead.")]
    bool Execute(params object[] @params) => Execute(new WiredExecutionContext(@params));
}
