using System.Reflection;
using System.Text.Json;
using Plus.Communication.Packets.Incoming;
using Plus.Communication.Packets.Outgoing;
using Plus.Core;

namespace Plus.Communication.Revisions;

public class RevisionsCache : IRevisionsCache, IStartable
{
    public IReadOnlyDictionary<string, Revision> Revisions { get; set; } = new Dictionary<string, Revision>();
    public Revision InternalRevision { get; private set; } = new();

    private string? _directory;
    public string Location => _directory ??= Path.Join(Directory.GetCurrentDirectory(), "Config", "Revisions");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public async Task Start()
    {
        LoadInternalRevision();
        await LoadRevisions();
        Validate();
    }

    private void LoadInternalRevision()
    {
        var incomingHeaders = new Dictionary<string, uint>();
        foreach (var field in typeof(ClientPacketHeader).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            if (!incomingHeaders.TryAdd(field.Name, (uint)field.GetRawConstantValue()!))
            {
                Console.WriteLine($"[CRITICAL] Duplicate field name {field.Name} in ClientPacketHeader.");
            }
        }

        var outgoingHeaders = new Dictionary<string, uint>();
        foreach (var field in typeof(ServerPacketHeader).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            if (!outgoingHeaders.TryAdd(field.Name, (uint)field.GetRawConstantValue()!))
            {
                Console.WriteLine($"[CRITICAL] Duplicate field name {field.Name} in ServerPacketHeader.");
            }
        }

        var incomingMapping = new Dictionary<uint, uint>();
        foreach (var kvp in incomingHeaders.Where(kvp => kvp.Value > 0))
        {
            if (!incomingMapping.TryAdd(kvp.Value, kvp.Value))
            {
                Console.WriteLine($"[CRITICAL] Duplicate incoming packet ID {kvp.Value} detected for header {kvp.Key}. Skipping mapping.");
            }
        }

        var outgoingMapping = new Dictionary<uint, uint>();
        foreach (var kvp in outgoingHeaders.Where(kvp => kvp.Value > 0))
        {
            if (!outgoingMapping.TryAdd(kvp.Value, kvp.Value))
            {
                Console.WriteLine($"[CRITICAL] Duplicate outgoing packet ID {kvp.Value} detected for header {kvp.Key}. Skipping mapping.");
            }
        }

        InternalRevision = new()
        {
            Name = "PRODUCTION-201701242205-837386173",
            IncomingHeaders = incomingHeaders,
            IncomingIdToInternalIdMapping = incomingMapping,
            OutgoingHeaders = outgoingHeaders,
            InternalIdToOutgoingIdMapping = outgoingMapping
        };

        if (!Directory.Exists(Location))
            Directory.CreateDirectory(Location);

        File.WriteAllText(Path.Join(Location, "example.json"), JsonSerializer.Serialize(InternalRevision, SerializerOptions));
    }

    private async Task LoadRevisions()
    {
        var revisions = new Dictionary<string, Revision>();
        foreach (var file in Directory.GetFiles(Location).Where(f => f.EndsWith(".json")))
        {
            var revision = JsonSerializer.Deserialize<Revision>(await File.ReadAllTextAsync(file), SerializerOptions);
            if (revision == null)
                continue;
            if (revision.Name.Equals(InternalRevision.Name)) continue;
            revisions[revision.Name] = revision;
        }
        revisions[InternalRevision.Name] = InternalRevision;
        Revisions = revisions;
    }

    private void Validate()
    {
        foreach (var revision in Revisions.Values)
        {
            var undefinedIncoming = revision.IncomingHeaders.Keys.Where(key => !InternalRevision.IncomingHeaders.ContainsKey(key)).ToList();
            var undefinedOutgoing = revision.OutgoingHeaders.Keys.Where(key => !InternalRevision.OutgoingHeaders.ContainsKey(key)).ToList();

            if (undefinedIncoming.Any())
            {
                Console.WriteLine($"{revision.Name}: Missing Incoming Headers ({undefinedIncoming.Count}):");
                foreach (var incoming in undefinedIncoming)
                    Console.WriteLine(incoming);
            }
            if (undefinedOutgoing.Any())
            {
                Console.WriteLine($"{revision.Name}: Missing Outgoing Headers ({undefinedOutgoing.Count}):");
                foreach (var outgoing in undefinedOutgoing)
                    Console.WriteLine(outgoing);
            }


            var incomingMapping = new Dictionary<uint, uint>();
            foreach (var kvp in revision.IncomingHeaders.Where(kvp => kvp.Value > 0))
            {
                if (!incomingMapping.TryAdd(kvp.Value, InternalRevision.IncomingHeaders[kvp.Key]))
                {
                    Console.WriteLine($"[CRITICAL] {revision.Name}: Duplicate incoming packet ID {kvp.Value} detected for header {kvp.Key}. Skipping.");
                }
            }

            var outgoingMapping = new Dictionary<uint, uint>();
            foreach (var kvp in revision.OutgoingHeaders.Where(kvp => kvp.Value > 0))
            {
                if (!outgoingMapping.TryAdd(InternalRevision.OutgoingHeaders[kvp.Key], kvp.Value))
                {
                    Console.WriteLine($"[CRITICAL] {revision.Name}: Duplicate internal outgoing packet ID {InternalRevision.OutgoingHeaders[kvp.Key]} detected for header {kvp.Key}. Skipping.");
                }
            }

            revision.IncomingIdToInternalIdMapping = incomingMapping;
            revision.InternalIdToOutgoingIdMapping = outgoingMapping;
        }
    }
}
