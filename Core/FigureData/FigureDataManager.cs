using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using Microsoft.Extensions.Logging;
using Plus.Core.FigureData.Types;
using Plus.HabboHotel.Catalog.Clothing;
using Plus.HabboHotel.Users.Clothing.Parts;
using Plus.Utilities;

namespace Plus.Core.FigureData;

public class FigureDataManager : IFigureDataManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly IClothingManager _clothingManager;
    private readonly ILogger<FigureDataManager> _logger;
    private readonly Dictionary<int, Palette> _palettes;
    private readonly List<string> _requirements;
    private readonly Dictionary<string, FigureSet> _setTypes;

    public FigureDataManager(IClothingManager clothingManager, ILogger<FigureDataManager> logger)
    {
        _clothingManager = clothingManager;
        _logger = logger;
        _palettes = new();
        _setTypes = new();
        _requirements = ["hd", "ch", "lg"];
    }

    public void Init()
    {
        _palettes.Clear();
        _setTypes.Clear();

        var configDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Config");
        var jsonPath = Path.Combine(configDirectory, "figuredata.json");
        var xmlPath = Path.Combine(configDirectory, "figuredata.xml");

        if (File.Exists(jsonPath))
        {
            LoadFromJson(jsonPath);
        }
        else
        {
            var document = LoadDocumentFromXml(xmlPath);
            PersistJsonDocument(jsonPath, document);
            LoadDocument(document);
            _logger.LogInformation("Figure data json cache created at {path}", jsonPath);
        }

        _setTypes["hd"].Sets[99999] = new(99999, "U", 0, true, false, false);
        _logger.LogInformation("Loaded " + _palettes.Count + " Color Palettes");
        _logger.LogInformation("Loaded " + _setTypes.Count + " Set Types");
    }

    public string ProcessFigure(string figure, string gender, ICollection<ClothingParts> clothingParts, bool hasHabboClub)
    {
        figure = figure.ToLower();
        gender = gender.ToUpper();
        var rebuildFigure = string.Empty;
        var figureParts = figure.Split('.');
        foreach (var part in figureParts.ToList())
        {
            var type = part.Split('-')[0];
            if (_setTypes.TryGetValue(type, out var figureSet))
            {
                var partId = Convert.ToInt32(part.Split('-')[1]);
                var colorId = 0;
                var secondColorId = 0;
                if (figureSet.Sets.TryGetValue(partId, out var set))
                {
                    if (set.Gender != gender && set.Gender != "U")
                    {
                        if (figureSet.Sets.Count(x => x.Value.Gender == gender || x.Value.Gender == "U") > 0)
                        {
                            partId = figureSet.Sets.FirstOrDefault(x => x.Value.Gender == gender || x.Value.Gender == "U").Value.Id;
                            figureSet.Sets.TryGetValue(partId, out set);
                            colorId = GetRandomColor(figureSet.PalletId);
                        }
                    }
                    if (set == null)
                        continue;
                    if (set.Colorable)
                    {
                        var splitterCounter = part.Count(x => x == '-');
                        if (splitterCounter == 2 || splitterCounter == 3)
                        {
                            if (!string.IsNullOrEmpty(part.Split('-')[2]))
                            {
                                if (int.TryParse(part.Split('-')[2], out colorId))
                                {
                                    colorId = Convert.ToInt32(part.Split('-')[2]);
                                    var palette = GetPalette(colorId);
                                    if (palette != null && colorId != 0)
                                    {
                                        if (figureSet.PalletId != palette.Id)
                                            colorId = GetRandomColor(figureSet.PalletId);
                                    }
                                    else if (palette == null && colorId != 0)
                                    {
                                        colorId = GetRandomColor(figureSet.PalletId);
                                    }
                                }
                                else
                                {
                                    colorId = 0;
                                }
                            }
                            else
                            {
                                colorId = 0;
                            }
                        }
                        if (splitterCounter == 3)
                        {
                            if (!string.IsNullOrEmpty(part.Split('-')[3]))
                            {
                                if (int.TryParse(part.Split('-')[3], out secondColorId))
                                {
                                    secondColorId = Convert.ToInt32(part.Split('-')[3]);
                                    var palette = GetPalette(secondColorId);
                                    if (palette != null && secondColorId != 0)
                                    {
                                        if (figureSet.PalletId != palette.Id)
                                            secondColorId = GetRandomColor(figureSet.PalletId);
                                    }
                                    else if (palette == null && secondColorId != 0)
                                    {
                                        secondColorId = GetRandomColor(figureSet.PalletId);
                                    }
                                }
                                else
                                {
                                    secondColorId = 0;
                                }
                            }
                            else
                            {
                                secondColorId = 0;
                            }
                        }
                    }
                    else
                    {
                        var ignore = new[] { "ca", "wa" };
                        if (ignore.Contains(type) && !string.IsNullOrEmpty(part.Split('-')[2]))
                            colorId = Convert.ToInt32(part.Split('-')[2]);
                    }
                    if (set.ClubLevel > 0 && !hasHabboClub)
                    {
                        partId = figureSet.Sets.FirstOrDefault(x => x.Value.Gender == gender || x.Value.Gender == "U" && x.Value.ClubLevel == 0).Value.Id;
                        figureSet.Sets.TryGetValue(partId, out set);
                        colorId = GetRandomColor(figureSet.PalletId);
                    }
                    rebuildFigure = secondColorId == 0
                        ? $"{rebuildFigure}{type}-{partId}-{colorId}."
                        : $"{rebuildFigure}{type}-{partId}-{colorId}-{secondColorId}.";
                }
            }
        }
        foreach (var requirement in _requirements)
        {
            if (!rebuildFigure.Contains(requirement))
            {
                if (requirement == "ch" && gender == "M")
                    continue;
                if (_setTypes.TryGetValue(requirement, out var figureSet))
                {
                    var set = figureSet.Sets.FirstOrDefault(x => x.Value.Gender == gender || x.Value.Gender == "U").Value;
                    if (set != null)
                    {
                        var partId = figureSet.Sets.FirstOrDefault(x => x.Value.Gender == gender || x.Value.Gender == "U").Value.Id;
                        var colorId = GetRandomColor(figureSet.PalletId);
                        rebuildFigure = $"{rebuildFigure}{requirement}-{partId}-{colorId}.";
                    }
                }
            }
        }
        if (clothingParts != null)
        {
            var purchasableParts = _clothingManager.GetClothingAllParts;
            figureParts = rebuildFigure.TrimEnd('.').Split('.');
            foreach (var part in figureParts.ToList())
            {
                var partId = Convert.ToInt32(part.Split('-')[1]);
                if (purchasableParts.Count(x => x.PartIds.Contains(partId)) > 0 && clothingParts.Count(x => x.PartId == partId) == 0)
                {
                    var type = part.Split('-')[0];
                    if (_setTypes.TryGetValue(type, out var figureSet))
                    {
                        var set = figureSet.Sets.FirstOrDefault(x => x.Value.Gender == gender || x.Value.Gender == "U").Value;
                        if (set != null)
                        {
                            partId = figureSet.Sets.FirstOrDefault(x => x.Value.Gender == gender || x.Value.Gender == "U").Value.Id;
                            var colorId = GetRandomColor(figureSet.PalletId);
                            rebuildFigure = $"{rebuildFigure}{type}-{partId}-{colorId}.";
                        }
                    }
                }
            }
        }
        return rebuildFigure;
    }

    public Palette? GetPalette(int colorId) => _palettes.FirstOrDefault(x => x.Value.Colors.ContainsKey(colorId)).Value;

    public bool TryGetPalette(int palletId, out Palette? palette) => _palettes.TryGetValue(palletId, out palette);

    public int GetRandomColor(int palletId) => _palettes[palletId].Colors.FirstOrDefault().Value.Id;

    public string FilterFigure(string figure) => StringCharFilter.IsValid(figure) ? figure : IFigureDataManager.DefaultFigure;

    private void LoadFromJson(string jsonPath)
    {
        var document = JsonSerializer.Deserialize<FigureDataDocument>(File.ReadAllText(jsonPath), JsonOptions)
                       ?? throw new InvalidOperationException($"Unable to deserialize figuredata json at {jsonPath}");
        LoadDocument(document);
        _logger.LogInformation("Loaded figure data from json {path}", jsonPath);
    }

    private void LoadDocument(FigureDataDocument document)
    {
        foreach (var paletteDocument in document.Palettes)
        {
            var palette = new Palette(paletteDocument.Id);
            foreach (var colorDocument in paletteDocument.Colors)
                palette.Colors[colorDocument.Id] = new(colorDocument.Id, colorDocument.Index, colorDocument.ClubLevel, colorDocument.Selectable, colorDocument.Value);
            _palettes[palette.Id] = palette;
        }

        foreach (var setTypeDocument in document.SetTypes)
        {
            var figureSet = new FigureSet(SetTypeUtility.GetSetType(setTypeDocument.Type), setTypeDocument.PaletteId);
            foreach (var setDocument in setTypeDocument.Sets)
            {
                var set = new Set(setDocument.Id, setDocument.Gender, setDocument.ClubLevel, setDocument.Colorable, setDocument.Selectable, setDocument.Preselectable);
                foreach (var partDocument in setDocument.Parts)
                    set.Parts[$"{partDocument.Id}-{partDocument.Type}"] = new(partDocument.Id, SetTypeUtility.GetSetType(partDocument.Type), partDocument.Colorable, partDocument.Index, partDocument.ColorIndex);
                figureSet.Sets[set.Id] = set;
            }
            _setTypes[setTypeDocument.Type] = figureSet;
        }
    }

    private FigureDataDocument LoadDocumentFromXml(string xmlPath)
    {
        if (!File.Exists(xmlPath))
            throw new FileNotFoundException("figuredata xml/json source not found", xmlPath);

        var document = new FigureDataDocument();
        var xDoc = new XmlDocument();
        xDoc.Load(xmlPath);

        var colors = xDoc.GetElementsByTagName("colors");
        foreach (XmlNode node in colors)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                var palette = new PaletteDocument { Id = GetRequiredIntAttribute(child, "id") };
                foreach (XmlNode sub in child.ChildNodes)
                {
                    palette.Colors.Add(new()
                    {
                        Id = GetRequiredIntAttribute(sub, "id"),
                        Index = GetRequiredIntAttribute(sub, "index"),
                        ClubLevel = GetRequiredIntAttribute(sub, "club"),
                        Selectable = GetRequiredIntAttribute(sub, "selectable") == 1,
                        Value = sub.InnerText ?? string.Empty
                    });
                }
                document.Palettes.Add(palette);
            }
        }

        var sets = xDoc.GetElementsByTagName("sets");
        foreach (XmlNode node in sets)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                var figureSet = new FigureSetDocument
                {
                    Type = GetRequiredStringAttribute(child, "type"),
                    PaletteId = GetRequiredIntAttribute(child, "paletteid")
                };

                foreach (XmlNode sub in child.ChildNodes)
                {
                    var set = new SetDocument
                    {
                        Id = GetRequiredIntAttribute(sub, "id"),
                        Gender = GetRequiredStringAttribute(sub, "gender"),
                        ClubLevel = GetRequiredIntAttribute(sub, "club"),
                        Colorable = GetRequiredIntAttribute(sub, "colorable") == 1,
                        Selectable = GetRequiredIntAttribute(sub, "selectable") == 1,
                        Preselectable = GetRequiredIntAttribute(sub, "preselectable") == 1
                    };

                    foreach (XmlNode subb in sub.ChildNodes)
                    {
                        if (subb.Attributes?["type"] == null)
                            continue;

                        set.Parts.Add(new()
                        {
                            Id = GetRequiredIntAttribute(subb, "id"),
                            Type = GetRequiredStringAttribute(subb, "type"),
                            Colorable = GetRequiredIntAttribute(subb, "colorable") == 1,
                            Index = GetRequiredIntAttribute(subb, "index"),
                            ColorIndex = GetRequiredIntAttribute(subb, "colorindex")
                        });
                    }

                    figureSet.Sets.Add(set);
                }

                document.SetTypes.Add(figureSet);
            }
        }

        return document;
    }

    private void PersistJsonDocument(string jsonPath, FigureDataDocument document)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(jsonPath)!);
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(document, JsonOptions));
    }

    private static int GetRequiredIntAttribute(XmlNode node, string attributeName) => Convert.ToInt32(GetRequiredStringAttribute(node, attributeName));

    private static string GetRequiredStringAttribute(XmlNode node, string attributeName) =>
        node.Attributes?[attributeName]?.Value ?? throw new XmlException($"Missing attribute '{attributeName}' on node '{node.Name}'.");

    private sealed class FlexibleIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt32(),
                JsonTokenType.String when int.TryParse(reader.GetString(), out var value) => value,
                _ => throw new JsonException($"Unable to convert token {reader.TokenType} to int.")
            };
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
    }

    private sealed class FlexibleBoolConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number => reader.GetInt32() != 0,
                JsonTokenType.String => ParseString(reader.GetString()),
                _ => throw new JsonException($"Unable to convert token {reader.TokenType} to bool.")
            };
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue(value);

        private static bool ParseString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (bool.TryParse(value, out var boolValue))
                return boolValue;

            if (int.TryParse(value, out var intValue))
                return intValue != 0;

            throw new JsonException($"Unable to convert '{value}' to bool.");
        }
    }

    private sealed class FlexibleLongConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt64(),
                JsonTokenType.String when long.TryParse(reader.GetString(), out var value) => value,
                _ => throw new JsonException($"Unable to convert token {reader.TokenType} to long.")
            };
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
    }

    private sealed class FigureDataDocument
    {
        [JsonPropertyName("palettes")]
        public List<PaletteDocument> Palettes { get; set; } = [];

        [JsonPropertyName("setTypes")]
        public List<FigureSetDocument> SetTypes { get; set; } = [];
    }

    private sealed class PaletteDocument
    {
        [JsonPropertyName("id")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int Id { get; set; }

        [JsonPropertyName("colors")]
        public List<ColorDocument> Colors { get; set; } = [];
    }

    private sealed class ColorDocument
    {
        [JsonPropertyName("id")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int Id { get; set; }

        [JsonPropertyName("index")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int Index { get; set; }

        [JsonPropertyName("club")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int ClubLevel { get; set; }

        [JsonPropertyName("selectable")]
        [JsonConverter(typeof(FlexibleBoolConverter))]
        public bool Selectable { get; set; }

        [JsonPropertyName("hexCode")]
        public string Value { get; set; } = string.Empty;
    }

    private sealed class FigureSetDocument
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("paletteid")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int PaletteId { get; set; }

        [JsonPropertyName("sets")]
        public List<SetDocument> Sets { get; set; } = [];
    }

    private sealed class SetDocument
    {
        [JsonPropertyName("id")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int Id { get; set; }

        [JsonPropertyName("gender")]
        public string Gender { get; set; } = string.Empty;

        [JsonPropertyName("club")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int ClubLevel { get; set; }

        [JsonPropertyName("colorable")]
        [JsonConverter(typeof(FlexibleBoolConverter))]
        public bool Colorable { get; set; }

        [JsonPropertyName("selectable")]
        [JsonConverter(typeof(FlexibleBoolConverter))]
        public bool Selectable { get; set; }

        [JsonPropertyName("preselectable")]
        [JsonConverter(typeof(FlexibleBoolConverter))]
        public bool Preselectable { get; set; }

        [JsonPropertyName("parts")]
        public List<PartDocument> Parts { get; set; } = [];
    }

    private sealed class PartDocument
    {
        [JsonPropertyName("id")]
        [JsonConverter(typeof(FlexibleLongConverter))]
        public long Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("colorable")]
        [JsonConverter(typeof(FlexibleBoolConverter))]
        public bool Colorable { get; set; }

        [JsonPropertyName("index")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int Index { get; set; }

        [JsonPropertyName("colorindex")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int ColorIndex { get; set; }
    }
}
