# Arcturus vs PlusEMU Packet ve Nitro Farklari

## Amac
Bu belge, iki emulator arasindaki packet surface ve Nitro entegrasyon farklarini ozetler.

## Ust Seviye Sonuc
- Arcturus Nitro destegini plugin destekli bir ek katman olarak ele aliyor.
- PlusEMU ise Nitro'yu cekirdek bootstrap ve packet map seviyesinde dogrudan dusunuyor.

## Baslangic Modeli

### Arcturus
README acikca Nitro icin ek websocket plugin gerektigini belirtiyor:
- [README.md](/home/duldul/Belgeler/Arcturus-Community-master/README.md#L6)

Bu su anlama geliyor:
- emulator core tek basina Nitro icin yeterli degil
- Nitro surface bazi durumlarda core disi pluginlerle tamamlanmis

### PlusEMU
Nitro config ve server yapisi core icinde:
- [Program.cs](/home/duldul/Belgeler/PlusEMU/Program.cs#L77)

Dogrudan register edilen alanlar:
- `FlashServerConfiguration`
- `NitroServerConfiguration`
- `RconConfiguration`

Bu repo packet compatibility ve revision mapping tarafini cekirdegin bir parcasi olarak ele aliyor.

## Packet Surface Farki

### Arcturus
Feature surface daha genis, dolayisiyla outgoing/incoming packet davranisi da daha zengin.
Repo taramasinda gorulen alanlar:
- Guide / guardian
- Camera
- YouTube
- Calendar
- Guild forums
- Wired highscores
- Subscription payday

### PlusEMU
Packet basliklari ve event dosyalari modern revision hedefleriyle tanimli, ancak bazi packetler stub veya eksik durumda.
Ornek alanlar:
- [ClientPacketHeader.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/ClientPacketHeader.cs#L345)
- [ServerPacketHeader.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/ServerPacketHeader.cs#L263)

Burada yorum satirinda duran veya sonradan tamamlanacak packetler var:
- `GetNextTargetedOfferEvent`
- `PurchaseTargetedOfferEvent`
- `SetTargetedOfferStateEvent`
- `ShopTargetedOfferViewedEvent`

Guide tarafinda ise ilk parity katmani artik mevcut:
- [GuideService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Guides/GuideService.cs)
- [HelperToolComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Moderation/HelperToolComposer.cs)
- [GuideSessionAttachedComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Guides/GuideSessionAttachedComposer.cs)
- [GuideSessionStartedComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Guides/GuideSessionStartedComposer.cs)
- [GuideSessionRequesterRoomComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Guides/GuideSessionRequesterRoomComposer.cs)
- [GuideSessionInvitedToGuideRoomComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Guides/GuideSessionInvitedToGuideRoomComposer.cs)
- [GuideSessionPartnerIsTypingComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Guides/GuideSessionPartnerIsTypingComposer.cs)
- [GuideSessionPartnerIsPlayingComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Guides/GuideSessionPartnerIsPlayingComposer.cs)
- [RequestGuideToolEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Guides/RequestGuideToolEvent.cs)
- [RequestGuideAssistanceEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Guides/RequestGuideAssistanceEvent.cs)
- [GuideVisitUserEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Guides/GuideVisitUserEvent.cs)
- [GuideInviteUserEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Guides/GuideInviteUserEvent.cs)
- [GuideUserTypingEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Guides/GuideUserTypingEvent.cs)
- [JoinQueueEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Game/Lobby/JoinQueueEvent.cs)
- [Game2ExitGameEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Game/Arena/Game2ExitGameEvent.cs)

Guardian tarafinda da ilk parity katmani artik mevcut:
- [GuardianService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Guides/GuardianService.cs)
- [GuardianNewReportReceivedComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Guides/GuardianNewReportReceivedComposer.cs)
- [GuardianVotingRequestedComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Guides/GuardianVotingRequestedComposer.cs)
- [GuardianAcceptRequestEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Guides/GuardianAcceptRequestEvent.cs)
- [GuardianVoteEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Guides/GuardianVoteEvent.cs)
- bu ilk dilim mevcut `SubmitBullyReportEvent` akisini guardian queue ile baglar, guardian yoksa moderation fallback devam eder
- accept-timeout, bounded resend ve under-voted moderation fallback artik mevcut
- daha zengin guardian sonuc UX'i halen backlog durumunda

Camera tarafinda ise ilk parity katmani artik mevcut:
- [CameraService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Camera/CameraService.cs)
- [CameraPriceComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Camera/CameraPriceComposer.cs)
- [CameraURLComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Camera/CameraURLComposer.cs)
- [CameraPublishWaitMessageComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Camera/CameraPublishWaitMessageComposer.cs)
- `PhotoCompetitionEvent` halen backlog durumunda

## Somut Nitro Durumlari

### Sanction Status
Bu alan once eksikti, artik temel olarak calisiyor:
- [GetSanctionStatusEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Help/GetSanctionStatusEvent.cs#L7)
- [SanctionStatusComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Help/SanctionStatusComposer.cs#L6)
- [SanctionStatusService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Moderation/SanctionStatusService.cs#L8)

Durum:
- stub degil
- moderation verisine bagli
- exact Nitro field semantics hala client tarafinda canli dogrulama isteyebilir

### Targeted Offer
Bu alan artik ilk parity katmanina sahip:
- [GetNextTargetedOfferEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Catalog/GetNextTargetedOfferEvent.cs)
- [PurchaseTargetedOfferEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Catalog/PurchaseTargetedOfferEvent.cs)
- [SetTargetedOfferStateEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Catalog/SetTargetedOfferStateEvent.cs)
- [ShopTargetedOfferViewedEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Catalog/ShopTargetedOfferViewedEvent.cs)
- [TargetedOfferManager.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Catalog/TargetedOfferManager.cs)
- [TargetedOfferService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Catalog/TargetedOfferService.cs)
- [TargetedOfferComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Catalog/TargetedOfferComposer.cs)

Durum:
- artik `NotImplementedException` atmiyor
- aktif teklif login sirasinda ve explicit request akisinda gonderilebiliyor
- satin alim/state persistence katmani var
- packet ID map'i Arcturus'tan turetildi; exact Nitro 2017 revision dogrulamasi halen gerekli

### HC Center / Club Gifts
Bu alan artik ilk parity katmanina sahip:
- [ClubCenterService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Subscriptions/ClubCenterService.cs)
- [ClubCenterDataComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Catalog/ClubCenterDataComposer.cs)
- [ClubGiftsComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Catalog/ClubGiftsComposer.cs)

Durum:
- `GetHabboClubWindowEvent` artik no-op degil
- `GetClubGiftInfoEvent` artik hardcoded junk payload yerine parametrik response donduruyor
- kickback/payday rakamlari mevcut hesap/subscription state'ten turetiliyor
- exact Nitro economics ve gift-claim flow halen tamamlanmis degil

### YouTube
Bu repo YouTube packet yuzeyine sahip:
- [GetYouTubeTelevisionEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Rooms/Furni/YouTubeTelevisions/GetYouTubeTelevisionEvent.cs)
- [YouTubeGetNextVideo.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Rooms/Furni/YouTubeTelevisions/YouTubeGetNextVideo.cs)
- [GetYouTubePlaylistComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Rooms/Furni/YouTubeTelevisions/GetYouTubePlaylistComposer.cs)

Arcturus tarafinda buna ek scheduler taraflari da gorunuyor:
- [YoutubeAdvanceVideo.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/threading/runnables/YoutubeAdvanceVideo.java)

### Calendar
PlusEMU tarafinda calendar user state izleri var:
- [Habbo.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Users/Habbo.cs#L135)
- [SeasonalCalendarService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Users/Calendar/SeasonalCalendarService.cs)
- [SeasonalCalendarDataComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Campaign/SeasonalCalendarDataComposer.cs)

Packet header tarafinda da calendar id'leri mevcut:
- [ClientPacketHeader.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/ClientPacketHeader.cs#L259)

Guncel durum:
- login sirasinda calendar state gonderiliyor
- `OpenCampaignCalendarDoorEvent` ve staff varyanti artik persistence ile calisiyor
- `user_xmas15_calendar` tablosu yoksa akis guvenli sekilde pas geciliyor
- exact daily-offer response semantics ve tam reward surface halen Arcturus kadar olgun degil

### Forums
PlusEMU packet basliklarinda forum yuzeyi var:
- [ClientPacketHeader.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/ClientPacketHeader.cs#L120)
- [ServerPacketHeader.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/ServerPacketHeader.cs#L167)

Ama feature completeness seviyesini tek basina header varligi garanti etmiyor.

## Asil Fark
Fark sadece "hangi packet var" degil.
Asil fark sunlar:
- state ownership nerede
- packetin arkasinda gerçek domain service var mi
- runtime scheduling tamam mi
- Nitro istemcinin bekledigi exact response shape karsilaniyor mu

Bu acidan PlusEMU'de bazi packetler tanimli olsa bile Arcturus tarafindaki kadar olgun backend akisina henuz sahip degil.

## Riskli Alanlar
- packet header var ama incoming event no-op ise istemci sessiz bozulur
- composer var ama field sirasi/meaning yanlissa Nitro UI bozulur
- state DB'ye yazilmiyor ise reconnect sonrasi parity bozulur

## Onerilen Packet/Nitro Onceligi
1. stub incoming eventleri kapat
2. her incoming event icin kalici state ownership tanimla
3. outgoing composer field semantics'i Nitro istemci davranisiyla dogrula
4. revision adapter farklarini tek belgede topla

## Onerilen Takip Listesi
- Targeted Offer
- seasonal calendar offers
- camera packet surface
- guide/help modern packetleri
- forum runtime completeness

## Ilgili Belgeler
- [nitro_react_plusemu_parity_ve_ux_talimatnamesi.md](/home/duldul/Belgeler/PlusEMU/nitro_react_plusemu_parity_ve_ux_talimatnamesi.md)
