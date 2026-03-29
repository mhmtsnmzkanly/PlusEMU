using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms.Games.Teams;
using Plus.HabboHotel.Users.Inventory.Furniture;

namespace Plus.HabboHotel.Items;

public class ItemDefinition
{
    public uint Id { get; set; }
    public int SpriteId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string PublicName { get; set; } = string.Empty;
    public ItemType Type { get; set; }
    public FurniCategory Category { get; set; } = FurniCategory.Default;
    public int Width { get; set; }
    public int Length { get; set; }
    public double Height { get; set; }
    public bool Stackable { get; set; }
    public bool Walkable { get; set; }
    public bool IsSeat { get; set; }
    public bool AllowEcotronRecycle { get; set; }
    public bool AllowTrade { get; set; }
    public bool AllowMarketplaceSell { get; set; }
    public bool AllowGift { get; set; }
    public bool AllowInventoryStack { get; set; }

    /// TODO @80O: Convert to string so plugins can add new interactions.
    public InteractionType InteractionType { get; set; }
    public int BehaviourData { get; set; }
    public int Modes { get; set; }
    public List<int> VendingIds { get; set; } = [];
    public List<double> AdjustableHeights { get; set; } = [];
    public int EffectId { get; set; }

    /// Legacy Wired box identity. Prefer interaction and helper predicates where possible.
    public WiredBoxType WiredType { get; set; }

    /// Catalog/display compatibility flag for rare items.
    public bool IsRare { get; set; }

    /// Allows non-cardinal rotation values on floor placement.
    public bool ExtraRot { get; set; }

    public bool IsWired =>
        InteractionType is InteractionType.WiredEffect
            or InteractionType.WiredCondition
            or InteractionType.WiredTrigger;

    public bool IsWiredTrigger => InteractionType == InteractionType.WiredTrigger;

    public bool IsWiredEffect => InteractionType == InteractionType.WiredEffect;

    public bool IsWiredCondition => InteractionType == InteractionType.WiredCondition;

    public bool IsTent =>
        InteractionType is InteractionType.Tent
            or InteractionType.TentSmall;

    public bool IsRoomDecoration =>
        InteractionType is InteractionType.Wallpaper
            or InteractionType.Floor
            or InteractionType.Landscape;

    public bool IsDeal =>
        InteractionType is InteractionType.Deal
            or InteractionType.Roomdeal;

    public bool IsBot => InteractionType == InteractionType.Bot;

    public bool IsExchange => InteractionType == InteractionType.Exchange;

    public bool IsGift => InteractionType == InteractionType.Gift;

    public bool IsDice => InteractionType == InteractionType.Dice;

    public bool IsHopper => InteractionType == InteractionType.Hopper;

    public bool IsRoller => InteractionType == InteractionType.Roller;

    public bool IsPet => InteractionType == InteractionType.Pet;

    public bool IsFreezeTile => InteractionType == InteractionType.FreezeTile;

    public bool IsFreezeTileBlock => InteractionType == InteractionType.FreezeTileBlock;

    public bool IsFreezeExit => InteractionType == InteractionType.Freezeexit;

    public bool IsBanzaiFloor => InteractionType == InteractionType.Banzaifloor;

    public bool IsBanzaiPyramid => InteractionType == InteractionType.Banzaipyramid;

    public bool IsBanzaiTeleport => InteractionType == InteractionType.Banzaitele;

    public bool IsBanzaiPuck => InteractionType == InteractionType.Banzaipuck;

    public bool IsFootball => InteractionType == InteractionType.Football;

    public bool IsFootballGate => InteractionType == InteractionType.FootballGate;

    public bool IsGate => InteractionType == InteractionType.Gate;

    public bool IsOneWayGate => InteractionType == InteractionType.OneWayGate;

    public bool IsStacktool => InteractionType == InteractionType.Stacktool;

    public bool IsPostIt => InteractionType == InteractionType.Postit;

    public bool IsBadgeDisplay => InteractionType == InteractionType.BadgeDisplay;

    public bool IsMannequin => InteractionType == InteractionType.Mannequin;

    public bool IsTrophy => InteractionType == InteractionType.Trophy;

    public bool IsPetBreedingBox => InteractionType == InteractionType.PetBreedingBox;

    public bool IsPurchasableClothing => InteractionType == InteractionType.PurchasableClothing;

    public bool IsMonsterplantSeed => InteractionType == InteractionType.MonsterplantSeed;

    public bool IsTeleport => InteractionType == InteractionType.Teleport;

    public bool IsHorseSaddle1 => InteractionType == InteractionType.HorseSaddle1;

    public bool IsHorseSaddle2 => InteractionType == InteractionType.HorseSaddle2;

    public bool IsHorseHairstyle => InteractionType == InteractionType.HorseHairstyle;

    public bool IsHorseHairDye => InteractionType == InteractionType.HorseHairDye;

    public bool IsHorseBodyDye => InteractionType == InteractionType.HorseBodyDye;

    public bool IsGnomeBox => InteractionType == InteractionType.GnomeBox;

    public bool IsLovelock => InteractionType == InteractionType.Lovelock;

    public bool IsBackground => InteractionType == InteractionType.Background;

    public bool IsFxProvider => InteractionType == InteractionType.FxProvider;

    public bool IsTelevision => InteractionType == InteractionType.Television;

    public bool IsBedLike =>
        InteractionType is InteractionType.Bed
            or InteractionType.TentSmall;

    public bool IsFloorSwitch =>
        InteractionType is InteractionType.WfFloorSwitch1
            or InteractionType.WfFloorSwitch2;

    public bool IsFootballGoal =>
        InteractionType is InteractionType.FootballGoalBlue
            or InteractionType.FootballGoalGreen
            or InteractionType.FootballGoalRed
            or InteractionType.FootballGoalYellow;

    public bool IsFootballCounter =>
        InteractionType is InteractionType.Footballcounterblue
            or InteractionType.Footballcountergreen
            or InteractionType.Footballcounterred
            or InteractionType.Footballcounteryellow;

    public bool IsFootballGoalOrCounter => IsFootballGoal || IsFootballCounter;

    public bool IsBanzaiScore =>
        InteractionType is InteractionType.Banzaiscoreblue
            or InteractionType.Banzaiscoregreen
            or InteractionType.Banzaiscorered
            or InteractionType.Banzaiscoreyellow;

    public bool IsTeamGate =>
        InteractionType is InteractionType.FreezeBlueGate
            or InteractionType.FreezeGreenGate
            or InteractionType.FreezeRedGate
            or InteractionType.FreezeYellowGate
            or InteractionType.Banzaigateblue
            or InteractionType.Banzaigatered
            or InteractionType.Banzaigategreen
            or InteractionType.Banzaigateyellow;

    public bool IsStickyNoteOrPhoto =>
        InteractionType is InteractionType.Postit
            or InteractionType.CameraPicture;

    public bool IsRandomWiredAddon => WiredType == WiredBoxType.AddonRandomEffect;

    public bool IsRegenerateMapsWired => WiredType == WiredBoxType.EffectRegenerateMaps;

    public bool IsGroupFurni =>
        InteractionType is InteractionType.GuildItem
            or InteractionType.GuildGate
            or InteractionType.GuildForum;

    public bool IsGroupGate => InteractionType == InteractionType.GuildGate;

    public bool IsMoodlight => InteractionType == InteractionType.Moodlight;

    public bool IsToner => InteractionType == InteractionType.Toner;

    public bool AllowsExtraRotation => ExtraRot;

    public bool BlocksWalkAsOccupiedTile =>
        IsSeat
        || IsBedLike;

    public string? RoomDecorationKey =>
        InteractionType switch
        {
            InteractionType.Floor => "floor",
            InteractionType.Wallpaper => "wallpaper",
            InteractionType.Landscape => "landscape",
            _ => null
        };

    public int RoomDecorationExtradataType =>
        InteractionType switch
        {
            InteractionType.Wallpaper => 2,
            InteractionType.Floor => 3,
            InteractionType.Landscape => 4,
            _ => 0
        };

    public byte RoomEffectMapType =>
        InteractionType switch
        {
            InteractionType.Pool => 1,
            InteractionType.NormalSkates => 2,
            InteractionType.IceSkates => 3,
            InteractionType.Lowpool => 4,
            InteractionType.Haloweenpool => 5,
            _ => 0
        };

    public Team GetTeamOrNone()
    {
        return InteractionType switch
        {
            InteractionType.FootballGoalBlue or
            InteractionType.Footballcounterblue or
            InteractionType.Banzaiscoreblue or
            InteractionType.Banzaigateblue or
            InteractionType.FreezeBlueGate or
            InteractionType.Freezebluecounter => Team.Blue,
            InteractionType.FootballGoalRed or
            InteractionType.Footballcounterred or
            InteractionType.Banzaiscorered or
            InteractionType.Banzaigatered or
            InteractionType.FreezeRedGate or
            InteractionType.Freezeredcounter => Team.Red,
            InteractionType.FootballGoalGreen or
            InteractionType.Footballcountergreen or
            InteractionType.Banzaiscoregreen or
            InteractionType.Banzaigategreen or
            InteractionType.FreezeGreenGate or
            InteractionType.Freezegreencounter => Team.Green,
            InteractionType.FootballGoalYellow or
            InteractionType.Footballcounteryellow or
            InteractionType.Banzaiscoreyellow or
            InteractionType.Banzaigateyellow or
            InteractionType.FreezeYellowGate or
            InteractionType.Freezeyellowcounter => Team.Yellow,
            _ => Team.None
        };
    }
}
