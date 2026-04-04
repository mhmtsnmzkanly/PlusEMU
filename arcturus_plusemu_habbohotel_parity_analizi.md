# Arcturus vs PlusEMU HabboHotel Parity Analizi

## Amac
Bu belge, `/home/duldul/Belgeler/Arcturus-Community-master` ile bu repo arasindaki `HabboHotel` katmani farklarini davranis, mimari ve parity acisindan ozetler.

## Kapsam
- Oda yonetimi
- Moderasyon
- Kullanici/yasam dongusu
- Oyun dongusu ve manager ownership
- Plugin entegrasyonu

## Ust Seviye Sonuc
Arcturus `HabboHotel` tarafi daha buyuk ve daha feature-complete bir monolit gorunumu veriyor.
PlusEMU `HabboHotel` tarafi ise daha parcalanmis, DI tabanli ve servis ekstraksiyonu yapilmis bir mimariye sahip.

Kisa fark:
- Arcturus: daha fazla built-in davranis, daha az mimari ayrisim
- PlusEMU: daha iyi testlenebilirlik ve degistirilebilirlik, ama bazi alanlarda eksik parity

## Oda Yonetimi

### Arcturus
Ana oda ownership'i buyuk olcude tek bir manager sinifinda toplaniyor:
- [RoomManager.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/habbohotel/rooms/RoomManager.java#L64)

Bu sinif:
- room category yukluyor
- room model cache tutuyor
- aktif odalari yonetiyor
- navigator sonuclarina katkida bulunuyor
- oyun turlerini register ediyor
- oda olaylarina plugin event bagliyor

### PlusEMU
Oda katmani daha fazla sorumluluk ayrimina sahip:
- [RoomManager.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Rooms/RoomManager.cs#L24)

`RoomManager` artik dogrudan cok sayida domain servisi enjekte ediyor:
- item persistence
- placement validation
- room item tracking
- roller apply
- room dependency resolution
- badge/group/cache/user data yardimcilari

Bu, oda katmaninda teknik borcu azaltmis ama Arcturus'taki "tek noktadan her seyi yapan" feature akislarini da dagitmis.

### Davranissal Fark
- Arcturus room manager icinde hem runtime hem navigator hem plugin event akislarini daha yogun tasiyor.
- PlusEMU ayni alani manager + service agirlikli yapiya bolmus.
- Bu repo daha once parity sapmasi yasamis iki noktayi yeni duzeltti:
  - custom `room_models` lazy load
  - doorbell davranisinin legacy ile uyumu

## Oyun Dongusu

### Arcturus
Boot ve domain yukleme merkezi bir emulator sinifindan yapiliyor:
- [Emulator.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/Emulator.java#L125)

`GameEnvironment.load()` ile manager'lar ayaga kalkiyor:
- [Emulator.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/Emulator.java#L146)

### PlusEMU
Game bootstrap'i DI uzerinden yapiyor:
- [Game.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Game.cs#L125)

`Game.Init()` ile manager'lar tek tek initialize ediliyor:
- moderation
- television
- navigator
- room models
- chat
- groups
- quests
- talents
- rewards
- subscriptions

### Sonuc
- Arcturus daha dogrudan ve feature-first
- PlusEMU daha kontrol edilebilir ve service-first

## Moderasyon

### Arcturus
CFH/modtool yapisi daha genis ve eskiden beri oturmus:
- [ModToolManager.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/habbohotel/modtool/ModToolManager.java#L33)

Yuklenen alanlar:
- support issue categories
- support presets
- support tickets
- CFH categories/topics
- room ve user chatlog akislari

### PlusEMU
Moderasyon verisi daha temiz sekilde Dapper ile yukleniyor:
- [ModerationManager.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Moderation/ModerationManager.cs#L107)

Alanlar:
- moderation presets
- moderation topics
- moderation topic actions
- moderation preset action categories
- moderation preset action messages
- username/machine ban cache

### Fark
- Arcturus support/modtool deneyimi daha genis ve legacy Habbo moderation davranisina daha yakin gorunuyor.
- PlusEMU moderasyon veri modeli daha temiz ama packet ve UX tarafinda hala bazi parity bosluklari var.
- Bu farkin bir parcasi olarak `SanctionStatus` yakinda gerçek akisa baglandi:
  - [GetSanctionStatusEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Help/GetSanctionStatusEvent.cs#L7)
  - [SanctionStatusService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Moderation/SanctionStatusService.cs#L8)

## Kullanici Yasam Dongusu

### Arcturus
Kullanici/oda/plugin olaylari daha cok event-driven ve cekirdek icine gomulu:
- room enter/exit eventleri
- achievement eventleri
- trade/game/wired eventleri

Bu desenin izleri:
- [RoomManager.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/habbohotel/rooms/RoomManager.java#L44)

### PlusEMU
Bu repo son donemde lifecycle cleanup odakli ilerliyor:
- disconnect ownership
- room attach/detach
- runtime initializer
- process component wiring

Referans:
- [LEGACY-REFACTOR-STATUS.md](/home/duldul/Belgeler/PlusEMU/LEGACY-REFACTOR-STATUS.md)

### Degerlendirme
- Arcturus davranis olarak daha olgun ama degisiklik yapmasi daha riskli.
- PlusEMU lifecycle tarafinda daha savunmali ve izlenebilir bir zemine geciyor.

## Plugin Entegrasyonu

### Arcturus
Plugin eventleri domain katmaninin icine kadar inmis durumda:
- room load/unload
- user enter/exit
- wired condition/stack
- game join/leave
- moderation/sanction

Bu, plugin capability acisindan guclu.

### PlusEMU
Plugin yukleme var ama lifecycle hook seviyesi daha sinirli:
- [Program.cs](/home/duldul/Belgeler/PlusEMU/Program.cs#L63)
- [IPluginDefinition.cs](/home/duldul/Belgeler/PlusEMU/Plugins/IPluginDefinition.cs#L5)

Plugin interface sunlari veriyor:
- `ConfigureServices`
- `OnServicesConfigured`
- `OnServiceProviderBuild`

Ama Arcturus'taki kadar yaygin domain event yüzeyi su an gorunmuyor.

## HabboHotel Acisindan Eksik / Zayif Alanlar
- Guide/guardian mantigi Arcturus'ta daha belirgin
- CFH/support UX yüzeyi Arcturus'ta daha dolu
- bazi room/game/wired legacy akislar Arcturus'ta daha olgun
- plugin event coverage Arcturus'ta daha genis

## HabboHotel Acisindan PlusEMU'nun Guclu Tarafi
- servis bazli ayrisim
- Dapper ve daha acik SQL projection yapisi
- DI ile daha net dependency ownership
- lifecycle ve crash handling uzerinde daha kontrollu zemin

## Oneri
`HabboHotel` parity calismasi su sirayla ilerlemeli:
1. room entry, room unload, moderation ve support packet yuzeylerini parity checklist'e bagla
2. Arcturus'taki plugin-event temas noktalarini aynen kopyalamak yerine capability odakli event surface tasarla
3. feature parity icin once davranis kritik alanlari tamamla, sonra mimari "guzellik" refactoru yap

## Ilgili Belgeler
- [nitro_react_plusemu_parity_ve_ux_talimatnamesi.md](/home/duldul/Belgeler/PlusEMU/nitro_react_plusemu_parity_ve_ux_talimatnamesi.md)
- [LEGACY-REFACTOR-STATUS.md](/home/duldul/Belgeler/PlusEMU/LEGACY-REFACTOR-STATUS.md)
