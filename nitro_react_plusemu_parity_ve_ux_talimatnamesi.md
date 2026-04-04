# Nitro React ve PlusEMU Parity + UX Talimatnamesi

## Amaç
Bu belge, `nitro-react` client ile PlusEMU server arasındaki eksik parity alanlarini ve emulator tarafinda kullanici deneyimini artiracak runtime/urun iyilestirmelerini tek bir uygulama rehberi halinde toplar.

Hedef:
- Client'ta gorunen ama server'da eksik, stub veya uyumsuz olan ozellikleri netlestirmek
- Bunlar icin uygulanabilir entegrasyon sirasi belirlemek
- Emulator'u daha canli, daha stabil ve daha kullanisli hale getirecek UX/back-end iyilestirmelerini backlog'a cevirmek

## Revision Notu
PlusEMU revision desteklemektedir.

Mevcut kod akisi:
- `ClientHelloEvent` build string alir
- `RevisionsCache` icindeki uygun revision secilir
- Bilinmeyen build gelirse baglanti kesilir

Mevcut build hedefleri:
- `NITRO-1-6-6`
- `PRODUCTION-201701242205-837386173`

Sonuc:
- Ana problem revision eksikligi degil
- Ana problem packet/feature implementasyon eksigi, stub handler'lar ve modern Nitro packet surface uyumsuzlugu

## Versioning Policy
Parity ve adapter katmani acik version contract ile yonetilecektir.

Zorunlu version alanlari:
- `packetSurfaceVersion`
- `adapterLayerVersion`
- `compatibilityMatrixVersion`

Kurallar:
- yeni Nitro adapter davranisi breaking ise `adapterLayerVersion` artirilir
- packet mapping tablosu versiyonlu tutulur
- revision JSON degisikligi ile packet surface degisikligi birlikte kayda girer
- feature parity tamamlandi diye legacy packet support hemen kaldirilmaz

## Client Var / Server Yok veya Eksik Ozellikler
Bu liste 3 kategoriye ayrilir:
- `Yok`: Client'ta kullaniliyor, server tarafinda gercek karsilik yok
- `Stub`: Server tarafinda dosya veya header var ama implementasyon yok
- `Uyarlama Gerekli`: Ozellik server'da var ama packet adi, payload sekli veya akis birebir Nitro client beklentisiyle ortusmuyor

### 1. Guide Tool
- Durum: `Kismi`
- Client tarafi:
  - Duty durumu
  - Guide/help/bully toggle
  - Request olusturma
  - Session attach/start/message/invite/end/error akisi
  - Guide ve user ongoing state yonetimi
- Server tarafi:
  - Minimum `GuideSession*` packet/composer surface artik var
  - Duty state, request queue, accept/decline, chat relay ve close/report akisi service-backed
  - Guardian on-duty, bully report queue, accept ve vote surface'in ilk dilimi artik var
  - Guardian timeout/resend ve moderation fallback mantiginin ilk dilimi artik var
  - Requester room ve room invite surface artik var
  - Typing surface artik var
  - Playing runtime sinyalinin ilk dilimi artik var
  - Reporter artik guardian review sonucu veya moderator fallback durumunu gorunur sekilde aliyor
- Kullanici etkisi:
  - Guide Tool UI artik yalnizca bos acilmiyor; temel helper-request-session akisi calisiyor
  - Guardian review de artik tamamen bos degil; session, timeout, resend ve reporter sonuc sinyali calisiyor
- Teknik etkisi:
  - Ilk service, queue ve transient session store eklendi
  - Kalan is exact guardian result packet semantics ve modern Help/CFH surface'in tamamlanmasi
- Oncelik: `Kritik`

### 2. Help / Modern Call For Help Akislari
- Durum: `Yok` ve `Kismi`
- Client tarafi:
  - Room report
  - IM report
  - Forum thread report
  - Forum message report
  - Photo report
  - Pending calls
  - Reply / disabled notify
- Server tarafi:
  - Ticket sistemi var
  - `SubmitNewTicketEvent` mevcut moderation ticket service'e bagli
  - help tool acilisi artik moderation topic tree'yi `CfhTopicsInitComposer` ile gonderiyor
  - kullanicinin acik bir ticket'i varsa help tool acilisinda gorunur pending-request notification donuyor
  - moderation ticket olusturma artik oda-zorunlu degil; roomless / non-user-scoped modern report varyantlari server tarafinda dusmeden kaydolabiliyor
  - reported user offline olsa bile ticket snapshot'i moderator UI'da hedef id/username bilgisini koruyor
  - basarili ticket submit artik reporter'a gorunur bir onay donduruyor ve topic action `message_text` varsa onu reuse ediyor
  - `auto_reply` moderation topic'leri artik mod ticket acmadan dogrudan configured reply ile sonuclaniyor
  - `mods_till_logout` topic'leri artik moderator kuyruğuna yukseltilmis priority ile dusuyor
  - `CallForHelpPendingCallsComposer` exact packet id'si halen dogrulanmis degil
- Kullanici etkisi:
  - Yardim araci artik yalnizca bos acilmiyor; temel topic surface mevcut
  - acik ticket durumunda sessiz bozulma yerine gorunur geri bildirim var
- Teknik etkisi:
  - moderation topic tree ve open-state ilk kez help tool acilisina baglandi
  - kalan is exact pending-calls packet semantics, `mods_till_logout` / guardian action davranis eslesmesi ve room/im/forum/photo report varyantlarinin ayri payload semantics'ini daraltmak
- Oncelik: `Kritik`

### 3. Sanction Status
- Durum: `Kismi`
- Client tarafi:
  - Sanction status response bekliyor
- Server tarafi:
  - `GetSanctionStatusEvent` artik response gonderiyor
  - mevcut uygulama mute, trade-lock, caution ve aktif ban verisini moderation-backed query ile composer'a tasiyor
  - sanction ekraninin packet akisi acildi, fakat exact Nitro UX/payload semantics icin halen client-level dogrulama gerekli
- Kullanici etkisi:
  - Yardim ekraninda yaptirim durumu eksik kalir
- Teknik etkisi:
  - Ilk sanction query + response composer tamamlandi
  - Sonraki adimda Nitro client payload davranisi dogrulanip gerekirse field semantics daraltilacak
- Oncelik: `Kritik`

### 3.5 Seasonal Calendar
- Durum: `Kismi`
- Client tarafi:
  - login calendar data surface
  - day open / force open
- Server tarafi:
  - kullanici calendar state yukleniyor
  - login sirasinda calendar data composer gonderiliyor
  - `OpenCampaignCalendarDoorEvent` ve `OpenCampaignCalendarDoorAsStaffEvent` artik persistence ile calisiyor
  - `user_xmas15_calendar` tablosu eksikse akis guvenli sekilde pas geciliyor
  - exact daily-offer payload semantics halen canli dogrulama istiyor
- Kullanici etkisi:
  - advent/calendar UI artik tamamen kirik degil
- Teknik etkisi:
  - ilk state + open-door parity katmani tamamlandi
  - sonraki adim gunluk reward/product payload ve campaign metadata surface
- Oncelik: `Yuksek`

### 4. Targeted Offer
- Durum: `Kismi`
- Client tarafi:
  - Offer fetch
  - Offer show
  - Purchase
  - Viewed/state update
- Server tarafi:
  - `GetNextTargetedOfferEvent`, `PurchaseTargetedOfferEvent`, `SetTargetedOfferStateEvent`, `ShopTargetedOfferViewedEvent` artik service-backed
  - `TargetedOfferManager`, `TargetedOfferService`, `TargetedOfferComposer` ve SQL tablo yolu eklendi
  - Login sirasinda aktif teklif push ediliyor
  - Offer state ve purchase sayaci `users_target_offer_purchases` uzerinden saklaniyor
  - `catalog_target_offers` tablosu eksikse akis guvenli sekilde pas geciliyor
  - `ShopTargetedOfferViewedEvent` handler'i var fakat exact client payload semantics hala canli dogrulama istiyor
  - Packet ID'leri Arcturus mapping'inden turetildi; `PRODUCTION-201701242205-837386173` icin canli dogrulama hala gerekli
- Kullanici etkisi:
  - Hedefli teklif UI artik tamamen bos/stub degil
- Teknik etkisi:
  - Ilk parity katmani tamamlandi
  - Sonraki adim exact packet mapping ve viewed payload semantics dogrulamasidir
- Oncelik: `Kritik`

### 5. HC Kickback / HC Center
- Durum: `Kismi`
- Client tarafi:
  - `GetClubGiftInfo`
  - `ScrGetKickbackInfo`
  - `ScrSendKickbackInfo`
- Server tarafi:
  - `GetHabboClubWindowEvent` artik service-backed ve `ClubCenterDataComposer` gonderiyor
  - `GetClubGiftInfoEvent` artik parametrik `ClubGiftsComposer` gonderiyor
  - club gifts icin eski hardcoded junk payload kaldirildi
  - kickback/payday sayilari su an mevcut subscription/account state'ten turetilmis fallback degerler
  - exact Nitro HC center economics ve `ScrGetKickbackInfo` / `ScrSendKickbackInfo` semantics halen canli dogrulama istiyor
- Kullanici etkisi:
  - HC center ve club gifts yuzeyi artik tamamen bos/no-op degil
- Teknik etkisi:
  - ilk club-center data ve club-gifts parity katmani tamamlandi
  - sonraki adim exact kickback hesap mantigi ve gift claim/runtime akisi
- Oncelik: `Yuksek`

### 6. Camera Configuration ve Camera Response Surface
- Durum: `Kismi`
- Client tarafi:
  - `RequestCameraConfiguration`
  - camera init/result olaylari
- Server tarafi:
  - `InitCameraEvent`, `RenderRoomEvent`, `RenderRoomThumbnailEvent`, `PurchasePhotoEvent`, `PublishPhotoEvent` artik service-backed
  - `CameraPriceComposer`, `CameraURLComposer`, `CameraRoomThumbnailSavedComposer`, `CameraPurchaseSuccesfullComposer`, `CameraPublishWaitMessageComposer` eklendi
  - kullanici bazli gecici camera state artik `Habbo` uzerinde tutuluyor
  - `camera_web` tablosu yoksa publish persistence guvenli sekilde pas geciliyor
  - exact binary render payload isleme ve `PhotoCompetitionEvent` semantics halen eksik
- Kullanici etkisi:
  - Kamera acilisi ve temel satin alma/publish akisi artik tamamen kirik degil
- Teknik etkisi:
  - ilk config/render/purchase/publish parity katmani tamamlandi
  - sonraki adim real renderer entegrasyonu ve competition/report surface
- Oncelik: `Yuksek`

### 7. Rentable Bot Command Configuration
- Durum: `Yok`
- Client tarafi:
  - `RequestBotCommandConfiguration`
  - `BotCommandConfigurationEvent`
  - `BotSkillSave`
- Server tarafi:
  - `OpenBotAction` var
  - Command configuration akis izleri gorunmuyor
- Kullanici etkisi:
  - Rentable bot menu tam calismaz
- Teknik etkisi:
  - Bot skill config read/write response modeli gerekir
- Oncelik: `Yuksek`

### 8. Official Song Id
- Durum: `Yok`
- Client tarafi:
  - Katalog sound machine sayfasinda `GetOfficialSongId`
  - `OfficialSongIdEvent`
- Server tarafi:
  - `GetSongInfo` ve `TraxSongInfo` var
  - `GetOfficialSongId` izi yok
- Kullanici etkisi:
  - Muzik katalog akisinda bozuk veya eksik data olur
- Teknik etkisi:
  - extraParam -> official song id lookup gerekir
- Oncelik: `Orta`

### 9. Product Offer
- Durum: `Yok`
- Client tarafi:
  - `GetProductOffer`
  - `ProductOfferEvent`
- Server tarafi:
  - Acik packet/composer karsiligi gorunmuyor
- Kullanici etkisi:
  - Bazı katalog/preview akislarinda urun detayi eksik kalir
- Teknik etkisi:
  - Offer query ve response modeli gerekir
- Oncelik: `Orta`

### 10. User Subscription
- Durum: `Yok`
- Client tarafi:
  - `UserSubscriptionComposer`
  - `UserSubscriptionEvent`
- Server tarafi:
  - Subscription domain var
  - Nitro'nun bekledigi packet surface gorunmuyor
- Kullanici etkisi:
  - Purse / club state yenilemesi eksik kalir
- Teknik etkisi:
  - Subscription adapter gerekir
- Oncelik: `Orta`

### 11. Pet Package
- Durum: `Yok`
- Client tarafi:
  - `OpenPetPackage`
  - `RoomSessionPetPackageEvent`
- Server tarafi:
  - Acik packet karsiligi gorunmuyor
- Kullanici etkisi:
  - Pet package acma UI kirik kalir
- Teknik etkisi:
  - Package open handler + response gerekir
- Oncelik: `Orta`

### 12. Mystery Box Cancel/Wait Flow
- Durum: `Yok`
- Client tarafi:
  - `MysteryBoxWaitingCanceled`
  - `ShowMysteryBoxWait`
  - `GotMysteryBoxPrize`
- Server tarafi:
  - Acik karsilik gorunmuyor
- Kullanici etkisi:
  - Mystery box acilisi kirik kalir
- Teknik etkisi:
  - Wait/cancel/result packet seti gerekir
- Oncelik: `Orta`

### 13. Navigator Init / Desktop / Global Room Conversion
- Durum: `Uyarlama Gerekli`
- Client tarafi:
  - `NavigatorInit`
  - `DesktopView`
  - `ConvertGlobalRoomId`
- Server tarafi:
  - Navigator domain var
  - Packet isimleri birebir ortusmuyor
- Kullanici etkisi:
  - Bazi modern Nitro navigation akislarinda beklenmedik uyumsuzluk olabilir
- Teknik etkisi:
  - Compatibility/alias packet katmani gerekir
- Oncelik: `Orta`

### 14. YouTube Modern Nitro Packet Surface
- Durum: `Uyarlama Gerekli`
- Client tarafi:
  - `GetYoutubeDisplayStatus`
  - `SetYoutubeDisplayPlaylist`
  - `ControlYoutubeDisplayPlayback`
- Server tarafi:
  - `GetYouTubeTelevision`
  - `ToggleYouTubeVideo`
  - `YouTubeGetNextVideo`
  - `YouTubeVideoInformation`
- Kullanici etkisi:
  - Widget var ama tam uyum garanti degil
- Teknik etkisi:
  - Adapter layer ile Nitro packet beklentisi saglanmali
- Oncelik: `Orta`

### 15. User Settings Modern Packet Surface
- Durum: `Uyarlama Gerekli`
- Client tarafi:
  - `UserSettingsCameraFollow`
  - `UserSettingsOldChat`
  - `UserSettingsRoomInvites`
  - `UserSettingsSound`
- Server tarafi:
  - `SetSoundSettings`
  - `SetChatPreference`
  - `SetUIFlags`
- Kullanici etkisi:
  - Ayarlar kaydolsa bile birebir Nitro akis uyumu olmayabilir
- Teknik etkisi:
  - Translation layer gerekir
- Oncelik: `Orta`

### 16. Nitropedia
- Durum: `Client-only by design`
- Client tarafi:
  - `habbopages.url` uzerinden fetch
- Server tarafi:
  - Packet gerektirmez
- Karar:
  - Bu alan parity backlog'una alinmayacak

## Packet Compatibility Matrix
Adapter fazi icin zorunlu packet mapping tablosu tutulacaktir.

Minimum kolonlar:
- `Nitro Packet`
- `Legacy Packet`
- `Direction`
- `Adapter`
- `Owner Service`
- `State Type`

Ilk doldurulacak ornek eslesmeler:
- `GetYoutubeDisplayStatus` -> `GetYouTubeTelevision`
- `SetYoutubeDisplayPlaylist` -> `ToggleYouTubeVideo` veya yeni adapter surface
- `ControlYoutubeDisplayPlayback` -> `YouTubeGetNextVideo` veya yeni playback adapter
- `UserSettingsSound` -> `SetSoundSettings`
- `UserSettingsOldChat` -> `SetChatPreference`
- `NavigatorInit` -> mevcut navigator bootstrap surface

Kural:
- adapter fazinda packet ismi bazli gelistirme bu matris olmadan baslatilmayacak
- her parity ozelligi owner service ile isaretlenecek

## Entegrasyon Fazlari
### Faz 1: Kritik Parity
- Guide Tool
- Help / CFH modern report akislar
- Sanction Status
- Targeted Offer

### Faz 2: Kritik UX Surface
- HC Kickback / HC Center
- Camera config ve response surface
- Bot command configuration
- Product Offer
- User Subscription
- Pet Package
- Mystery Box flow

### Faz 3: Compatibility Layer
- YouTube Nitro adapter
- Navigator init / desktop / room conversion adapter
- Marketplace modern packet aliasing
- User settings translation layer

### Faz 4: Runtime Kalite ve Emulator UX
- Bot davranislari
- Oda canliligi
- Room tick ve performans
- Moderasyon gozlemlenebilirligi
- QoL / retention

## Runtime State Ownership
Parity backlog'undaki her ozellik state ownership acisindan asagidaki siniflardan birine baglanacaktir:
- transient session store
- per-user persistent store
- room-scoped state
- global service state

Ilk owner kararlar:
- Guide session: transient session store + per-user queue metadata
- Targeted offer: per-user persistent store
- Camera flow: transient session store
- Mystery box wait state: transient session store
- HC kickback: per-user persistent store
- YouTube display control: room-scoped state
- Bot command configuration: per-user persistent store veya bot-owned persistent store

Kural:
- owner service tanimi yapilmayan feature implementasyona alinmayacak
- transient state icin cleanup hook zorunlu olacak

## Teknik Uygulama Kurallari
- `NotImplementedException` ile packet handler birakilmayacak
- Bir feature eklendiginde su uc alan birlikte tamamlanacak:
  - incoming packet
  - outgoing packet/composer
  - revision mapping
- Server-only mevcut domain varsa yeniden yazmak yerine adapter/alias katmani tercih edilecek
- DB gerektiren state'ler memory-only birakilmayacak
- Feature flag ile kapatilabilir olmasi tercih edilecek

## Packet Backpressure Policy
Modern Nitro client burst packet gonderebildigi icin parity feature'lari rate control ile cikacaktir.

Zorunlu korumalar:
- per-session burst limit
- per-room packet quota
- duplicate suppression window
- handler timeout telemetry

Oncelikli korunacak alanlar:
- navigator
- camera
- youtube
- guide chat
- help/report submit

Kural:
- backpressure policy olmadan chat veya high-frequency UI akisi production'a alinmayacak

## Observability Standardi
Her parity feature ve adapter asagidaki minimum metrikleri uretmelidir:
- success count
- failure count
- latency histogram
- timeout count
- retry count
- DB read/write duration
- packet send frequency

Parity backlog'una ozel metrikler:
- targeted offer fetch latency
- guide session create success rate
- sanction status response latency
- camera init failure rate
- navigator adapter hit count
- youtube adapter translation failure count
- room tick drift

Log kurallari:
- packet handler log'lari packet adi, revision, user id ve feature owner bilgisini tasimali
- stub/stale adapter kullanimi warning olarak raporlanmali

## Rollback Strategy
Parity feature'lari fazli ve geri alinabilir sekilde deploy edilecektir.

Kurallar:
- her yeni parity ozelligi feature flag ile kapatilabilir olacak
- adapter layer eski packet yolunu en az bir faz boyunca koruyacak
- yeni persistent state migration'i feature kapali olsa da eski veriyi bozmamali
- room impact yaratan feature'lar room bazli disable edilebilir olmali
- issue durumunda legacy packet path'e donus desteklenecek

## Emulator Tarafinda UX ve Runtime Iyilestirmeleri
Bu bolum parity listesinin ustune ek backlog'tur.

### 1. Oda Icindeki Canlilik
- Reactive bot behavior
  - oyuncu girisine selam
  - dansa tepki
  - pet yaklasinca tepki
  - trade veya furni etkileşimine cevap
- Room mood profiles
  - cafe, support, game, chill room gibi profiller
- Ambient room actions
  - dusuk frekansli idle aksiyonlar
  - bakis degistirme
  - oturma/kalkma

### 2. Kullanici Aksiyon Kalitesi
- Action intent queue
  - hizli gelen user action'lari drop etmek yerine siralama
- Anti-desync movement
  - client-server hareket hissini yumusatma
- Smart error feedback
  - rights yok
  - tile dolu
  - room state izin vermiyor
  gibi nedenleri ayri iletme
- Interaction cooldown normalization
  - furni ve user action cooldown'larini tutarli yapmak

### 3. Performans ve Olcek
- Room tick partitioning
  - user, item, bot, pet, wired, pathfinding fazlarina ayirma
- Adaptive tick rate
  - bos odada daha hafif isleme
- Packet batching
  - ardışık update paketlerini birlestirme
- Read-through caches
  - navigator, profile, relationship, badge, catalog gibi alanlarda kisa omurlu cache
- Async DB boundaries
  - room loop'u bloklayan sorgularin servis katmanina alinmasi

### 4. Moderasyon ve Operasyon
- Rich moderation context
  - user room history
  - trade behavior
  - report count
- Silent anomaly detection
  - script/flood/trade probing izleme
- Room health diagnostics
  - en pahali room'lar
  - wired sorunlari
  - bot spam kaynaklari
- Live feature toggles
  - yarim modulleri config ile ac/kapat

### 5. QoL ve Retention
- Session continuity
  - reconnect sonrasi kisitli state geri kazanimi
- Contextual notifications
  - friend online
  - favorite room active
  - event basladi
- Progressive onboarding
  - yeni kullanici akislari
- Preference depth
  - notification sessize alma
  - room invite filtreleri
  - bot konusma yogunlugu

## Onerilen Yol Haritasi
### Sprint 1
- Help / CFH
- Sanction Status
- Targeted Offer

### Sprint 2
- Guide Tool

### Sprint 3
- Camera
- HC Kickback
- User Subscription
- Product Offer
- Pet Package
- Mystery Box

### Sprint 4
- Navigator / YouTube / Marketplace / User Settings compatibility layer

### Sprint 5
- Room runtime modernization
- Packet batching
- cache ve profiling

### Sprint 6
- Reactive bots
- QoL
- retention ve observability

## Test ve Kabul Kriterleri
- Iki revision da handshake seviyesinde calismali
- Guide Tool request -> accept -> chat -> invite -> resolve akisi tamamlanmali
- Help modern report akislar client'ta hata vermemeli
- Sanction status gercek veri dondurmeli
- Targeted offer fetch/view/purchase calismali
- Camera config response dondurmeli
- User settings Nitro client ile persistence'e yazilmali
- YouTube widget modern Nitro packet beklentisiyle calismali
- Kalabalik odalarda room tick stabil kalmali

## Son Not
Bu backlog ikiye ayrilarak uygulanmalidir:
- `parity`: client'ta zaten gorunen ama server'da eksik ozellikler
- `quality`: emulator'u daha canli, daha stabil ve daha kullanisli yapan runtime iyilestirmeleri
