using System.Text.Json.Serialization;

namespace Plus.Communication.Revisions;

public class Revision
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, uint> IncomingHeaders { get; set; } = new Dictionary<string, uint>();
    [JsonIgnore]
    public IReadOnlyDictionary<uint, uint> IncomingIdToInternalIdMapping { get; set; } = new Dictionary<uint, uint>();
    public IReadOnlyDictionary<string, uint> OutgoingHeaders { get; set; } = new Dictionary<string, uint>();
    [JsonIgnore]
    public IReadOnlyDictionary<uint, uint> InternalIdToOutgoingIdMapping { get; set; } = new Dictionary<uint, uint>();
}
