# PlusEMU Configuration ve Localization Hardening Talimatnamesi

## Amaç
Bu belge, PlusEMU icindeki `server_settings` ve `server_locale` kullanimini guvenli, dogrulanabilir ve genisletilebilir hale getirmek icin uygulanacak refactor ve hardening adimlarini tanimlar.

Odak:
- DB'de duran ama kodda etkisiz kalan ayarlari tespit etmek
- string tabanli config erisimini typed hale getirmek
- locale tablosunu gercekten merkezi mesaj kaynagi haline getirmek
- hardcoded mesajlari azaltmak

## Versioning ve Schema Policy
Config ve locale katmani acik version contract ile yonetilecektir.

Zorunlu version alanlari:
- `settingsSchemaVersion`
- `localeSchemaVersion`
- `settingsApiVersion`

Kurallar:
- typed settings API breaking degisiklik yaptiginda `settingsApiVersion` artirilir
- locale placeholder kurallari `localeSchemaVersion` ile takip edilir
- audit komutlari aktif schema version bilgisini raporlar
- DB tablo yapisi degisirse migration versiyonu belgeye ve SQL migration'a islenir

## Canli Analiz Ozeti
Veritabani:
- DB: `plus_original`
- Tablolar:
  - `server_settings`
  - `server_locale`

Canli sayilar:
- `server_settings`: 15 kayit
- `server_locale`: 9 kayit

### server_settings Canli Anahtarlar
- `catalog.enabled`
- `catalog.group.purchase.cost`
- `group.delete.member.limit`
- `messenger.buddy_limit`
- `room.chat.filter.banned_phrases.chances`
- `room.item.exchangeables.enabled`
- `room.item.gifts.enabled`
- `room.item.placement_limit`
- `room.pets.placement_limit`
- `room.promotion.lifespan`
- `trading.auto_exchange_redeemables`
- `user.currency_scheduler.credit_reward`
- `user.currency_scheduler.ducket_reward`
- `user.currency_scheduler.tick`
- `user.login.message.enabled`

### server_locale Canli Anahtarlar
- `moderation.kick.disallowed`
- `room.creation.model.not_found`
- `room.creation.name.too_short`
- `room.item.already_placed`
- `room.rights.user.has_rights`
- `server.console.alert`
- `server.shutdown.message`
- `user.login.message`
- `user.not_found`

## Temel Sorunlar
### 1. SettingsManager zayif tasarlanmis
Mevcut dosya:
- [SettingsManager.cs](/home/duldul/Belgeler/PlusEMU/Core/Settings/SettingsManager.cs)

Sorunlar:
- Tum degerler `string` olarak tutuluyor
- Yukleme sirasinda `value.ToLower()` yapiliyor
- Eksik key'de `TryGetValue()` `"0"` donduruyor
- Bu nedenle:
  - key eksikligi ile gercek `0` ayni seye donusuyor
  - parse hatalari sessizce gizleniyor
  - config yanlislari gec fark ediliyor

### 2. LanguageManager production-safe degil
Mevcut dosya:
- [LanguageManager.cs](/home/duldul/Belgeler/PlusEMU/Core/Language/LanguageManager.cs)

Sorunlar:
- Eksik locale key icin `"No language locale found for [...]"` donduruyor
- Bu string kullaniciya sizabilir
- Warning/log var ama fallback stratejisi dogru degil

### 3. Bazi settings anahtarlari tamamen olu
Kodda 0 kullanim bulunan canli ayarlar:
- `user.currency_scheduler.credit_reward`
- `user.currency_scheduler.ducket_reward`
- `user.currency_scheduler.tick`

Ilgili yerler:
- [ProcessComponent.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Users/Process/ProcessComponent.cs)
- [Habbo.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Users/Habbo.cs#L143)

Durum:
- Kullanici process timer'i var
- Currency scheduler config DB'de var
- Ama implementasyon baglanmamis
- `CheckCreditsTimer(...)` placeholder

### 4. Bazi locale anahtarlari tamamen olu
Kodda 0 kullanim bulunan canli locale key'ler:
- `room.creation.model.not_found`
- `room.creation.name.too_short`
- `user.not_found`

Sonuc:
- Ya eski refactor sonrasi call-site kaybolmus
- Ya da locale kaydi kullanilmadan tabloda birakilmis

### 5. Hardcoded mesajlar fazla
Ornek:
- [CatalogService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Catalog/CatalogService.cs#L107)
- Voucher akislarindaki hardcoded `SendNotification` mesajlari
- Oda, trade, permission ve inventory akislarindaki hardcoded İngilizce mesajlar

Sonuc:
- `server_locale` merkezi localization kaynagi haline gelmemis

## server_settings Refactor Plani
### Hedef API
Degistirilecek dosyalar:
- [ISettingsManager.cs](/home/duldul/Belgeler/PlusEMU/Core/Settings/ISettingsManager.cs)
- [SettingsManager.cs](/home/duldul/Belgeler/PlusEMU/Core/Settings/SettingsManager.cs)

Yeni API onerisi:
- `bool TryGetString(string key, out string value)`
- `string GetStringOrDefault(string key, string defaultValue)`
- `int GetIntOrDefault(string key, int defaultValue)`
- `bool GetBoolOrDefault(string key, bool defaultValue)`
- `int RequireInt(string key, int? min = null, int? max = null)`
- `bool RequireBool(string key)`

Kurallar:
- `TryGetValue(string)` legacy kalabilir ama yeni kodda kullanilmayacak
- `.ToLower()` yukleme asamasindan kaldirilacak
- Eksik key otomatik `"0"` olmayacak
- Kritik ayarlarda explicit validation olacak

### Startup Validation
`SettingsManager.Reload()` veya startup sonrasinda raporlanacak:
- DB'de olup kodda hic kullanilmayan key'ler
- Koddaki contract'ta olup DB'de olmayan key'ler
- int/bool parse edilemeyen key'ler
- min/max ihlali yapan key'ler

Log davranisi:
- Tek tek spam yerine toplu rapor
- warning ve error seviyeleri ayrilmali

## Typed Key Registry
Typed settings API'nin daginik buyumemesi icin merkezi key registry tutulacaktir.

Her key icin resmi kontrat:
- canonical key name
- expected type
- default behavior
- required veya optional durumu
- owner module
- validation rule

Onerilen tip:
- `SettingsKeyDefinition`

Onerilen alanlar:
- `Key`
- `Type`
- `Required`
- `DefaultValue`
- `Owner`
- `Min`
- `Max`

Kurallar:
- yeni config anahtari registry kaydi olmadan eklenmez
- audit komutu registry'de olup DB'de olmayan key'leri raporlar
- owner module bilgisi olmayan key teknik borc olarak kabul edilir
- settings migration ve cleanup calismalari bu registry uzerinden yurutulur

## Distributed Config Refresh
Reload davranisi yalniz tek instance icin degil, coklu node senaryosu icin de tanimlanacaktir.

Zorunlu kurallar:
- manual reload command korunacak
- cache entry'leri TTL destekleyebilir ama source of truth DB kalacak
- `updated_at` veya esdeger timestamp alanina dayali invalidation modeli tanimlanacak
- multi-node deployment varsa config refresh politikasi acik olacak

Varsayilan politika:
- tek instance ortamda manuel reload yeterli
- coklu instance ortamda DB timestamp tabanli poll + manuel force reload tercih edilir

## server_locale Refactor Plani
### Hedef API
Degistirilecek dosyalar:
- [ILanguageManager.cs](/home/duldul/Belgeler/PlusEMU/Core/Language/ILanguageManager.cs)
- [LanguageManager.cs](/home/duldul/Belgeler/PlusEMU/Core/Language/LanguageManager.cs)

Yeni API onerisi:
- `bool TryGetString(string key, out string value)`
- `string GetOrDefault(string key, string fallback)`
- `string Require(string key)`

Kurallar:
- Missing locale kullaniciya debug string gostermeyecek
- Warning log yazilacak
- Fallback:
  - ya guvenli sabit text
  - ya locale key'in kendisi
  - ya caller fallback degeri

## Locale Template Standardi
Locale degerleri string concat ile degil, named placeholder sistemiyle kullanilacaktir.

Standart:
- yalniz named placeholder kabul edilir
- ornek: `Welcome {username}`
- placeholder casing sabit olmalidir

Kurallar:
- missing placeholder warning log uretir
- ekstra placeholder verisi ignore edilmez, warning olarak raporlanir
- call-site tarafinda string concat yasaklanir
- locale render helper'i placeholder binding sorumlulugunu ustlenir

Oncelikli kullanim alanlari:
- login mesaji
- moderation feedback
- catalog notifications
- trade ve room permission mesajlari

## Pluralization ve I18n Scope
Locale sistemi tek dil odakli baslasa da i18n kapsam sinirlari simdiden tanimlanacaktir.

v1 karari:
- ana hedef tek aktif server dili
- locale key yapisi ileride coklu dil destegine genisleyebilir

Zorunlu i18n kurallari:
- sayi ve cogul ifadeleri icin locale helper destegi planlanacak
- dogrudan `"1 item"` / `"2 items"` tarzı concat yaklasimi yasaklanacak
- future-proof key naming kullanilacak

Ornek:
- `inventory.items.count.one`
- `inventory.items.count.other`

Kurallar:
- v1'de tam pluralization engine zorunlu degil, ama key tasarimi buna uyumlu olacak
- formatlama davranislari locale helper icinde toplanacak
- coklu dil destegi gelirse `server_locale` tablosu language-aware modele tasinabilecek sekilde ele alinacak

### Locale Audit
Reload veya audit komutunda raporlanacak:
- DB'de var ama kodda hic kullanilmayan locale key'ler
- Kodda referansi var ama DB'de olmayan locale key'ler
- Hardcoded string adayi olan call-site'lar

## Secret ve Sensitive Config Ayrimi
`server_settings` butun runtime ayarlarin tutuldugu yer olabilir, ama secret tutma yeri olmayacaktir.

Ayrilacak siniflar:
- public runtime config
- sensitive runtime config
- secret config

Kurallar:
- `server_settings` yalniz public runtime config icin kullanilacak
- API key, webhook secret, SMTP password gibi degerler ayrica secret provider veya environment tabanli source'tan gelecek
- typed settings API secret okumak icin kullanilmayacak
- audit komutu secret class key'lerin `server_settings` icinde bulunmasini error olarak raporlayacak

## Ayri Ayri Degistirilecek Call-Site'lar
Bu bolum uygulayici icin dogrudan is listesidir.

### Settings call-site migration
#### Catalog
- [CatalogService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Catalog/CatalogService.cs#L107)
  - `catalog.enabled`
  - mevcut string check yerine typed bool kullan

#### Group
- [GroupService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Groups/GroupService.cs#L482)
  - `group.delete.member.limit`
- [GroupService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Groups/GroupService.cs#L517)
  - `catalog.group.purchase.cost`
- [GroupCreationWindowComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/Groups/GroupCreationWindowComposer.cs#L21)
  - `catalog.group.purchase.cost`

#### Messenger
- [MessengerInitComposer.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Outgoing/FriendList/MessengerInitComposer.cs#L18)
  - `messenger.buddy_limit`

#### Room / Item
- [ItemService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Items/ItemService.cs#L53)
  - `room.item.placement_limit`
- [ItemService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Items/ItemService.cs#L55)
  - `room.item.placement_limit`
- [RoomCreatureService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Rooms/AI/RoomCreatureService.cs#L96)
  - `room.pets.placement_limit`
- [RoomPromotion.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Rooms/RoomPromotion.cs#L12)
  - `room.promotion.lifespan`

#### Trading
- [TradingService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Rooms/Trading/TradingService.cs#L375)
  - `trading.auto_exchange_redeemables`

#### Gifts / Redeemables
- [PurchaseFromCatalogAsGiftEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Catalog/PurchaseFromCatalogAsGiftEvent.cs#L68)
  - `room.item.gifts.enabled`
- [CreditFurniRedeemEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Rooms/Furni/CreditFurniRedeemEvent.cs#L29)
  - `room.item.exchangeables.enabled`

#### Chat
- [ChatService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Rooms/Chat/ChatService.cs#L236)
  - `room.chat.filter.banned_phrases.chances`

#### Login
- [SSOTicketEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Handshake/SSOTicketEvent.cs#L142)
  - `user.login.message.enabled`

### Currency scheduler implementasyonu
#### Ilgili yerler
- [ProcessComponent.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Users/Process/ProcessComponent.cs#L68)
- [Habbo.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Users/Habbo.cs#L143)

#### Yapilacaklar
- `user.currency_scheduler.credit_reward` ayarini gercek odul dagitimina bagla
- `user.currency_scheduler.ducket_reward` ayarini gercek odul dagitimina bagla
- `user.currency_scheduler.tick` ayarini zamanlama periyodu olarak kullan
- `CheckCreditsTimer(...)` placeholder kalmayacak

#### Karar
- Reward mantigi `ProcessComponent` icinde acik ve typed config ile yonetilecek
- 0 degerli reward'lar no-op olacak
- Reward gonderiminde ilgili outgoing composer'lar kullanilacak

## Locale Call-Site Degisimleri
Bu call-site'lar locale API refactor sonrasi guclendirilmeli.

### Zaten locale kullanan yerler
- [PlusEnvironment.cs](/home/duldul/Belgeler/PlusEMU/PlusEnvironment.cs#L299)
  - `server.shutdown.message`
- [ConsoleCommands.cs](/home/duldul/Belgeler/PlusEMU/Core/ConsoleCommands.cs#L30)
  - `server.console.alert`
- [ModerationActionService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Moderation/ModerationActionService.cs#L116)
  - `moderation.kick.disallowed`
- [RoomItemPlacementApplyService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Rooms/RoomItemPlacementApplyService.cs#L32)
  - `room.item.already_placed`
- [RoomItemPlacementApplyService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Rooms/RoomItemPlacementApplyService.cs#L84)
  - `room.item.already_placed`
- [RoomAccessService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Rooms/RoomAccessService.cs#L50)
  - `room.rights.user.has_rights`
- [SSOTicketEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Handshake/SSOTicketEvent.cs#L143)
  - `user.login.message`

### DB'de var ama baglanmasi gereken locale'ler
- `room.creation.model.not_found`
- `room.creation.name.too_short`
- `user.not_found`

Uygulama kuralı:
- ilgili call-site bulunup locale'a baglanacak
- uygun aktif call-site yoksa audit raporunda `unused` olarak birakilacak

## Observability Standardi
Config ve localization katmani asagidaki minimum olcumleri uretmelidir:
- settings reload success count
- settings reload failure count
- locale reload success count
- locale reload failure count
- parse failure count
- missing key count
- missing locale count
- cache hit/miss count
- reload duration

Ozel metrikler:
- settings validation duration
- locale template render duration
- placeholder missing rate
- hardcoded string candidate count

Log kurallari:
- parse failure log'lari key adi ve beklenen tip bilgisini tasimali
- missing locale log'lari call-site veya feature etiketiyle zenginlestirilmeli

## Rollback Strategy
Typed config ve locale refactor'u legacy yol korunarak alinacaktir.

Kurallar:
- `TryGetValue()` ve eski locale API ilk gecis fazinda compatibility shim olarak tutulabilir
- yeni typed API feature flag veya compile-time migration asamasi ile kademeli alinacak
- locale template renderer production hatasi verirse plain text fallback ile calismaya devam edecek
- distributed refresh bozulursa manuel reload komutu her zaman calisir durumda kalmali
- schema degisikligi gerektiren migration'lar rollback script veya backwards-compatible SQL ile gelmeli

## Hardcoded String -> Locale Migrasyonu
Oncelikli tasinacak mesajlar:
- [CatalogService.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Catalog/CatalogService.cs#L107)
  - `catalog.disabled`
- Voucher akislarindaki:
  - max uses
  - already used
  - generic voucher errors
- Item limit mesajlari:
  - `room.item.limit_reached`
- Pet limit mesajlari:
  - `room.pets.limit_reached`
- Trade / permission / room creation hatalari

Onerilen yeni locale key ailesi:
- `catalog.disabled`
- `catalog.voucher.max_uses`
- `catalog.voucher.already_used`
- `room.item.limit_reached`
- `room.pets.limit_reached`
- `room.creation.model.not_found`
- `room.creation.name.too_short`
- `user.not_found`
- `trading.auto_exchange.notice`

## Operasyonel Audit Komutlari
Yeni komutlar eklenmeli:
- `audit_server_settings`
- `audit_server_locale`

Her komut su raporu uretmeli:
- used keys
- unused DB keys
- missing DB keys
- parse failures
- hardcoded candidate paths

Bu komutlar RCON veya admin command olarak eklenebilir.

## Uygulama Sirası
### Adim 1
- `SettingsManager` typed API
- `LanguageManager` safe fallback

### Adim 2
- Kritik settings call-site migration
- Kritik locale call-site migration

### Adim 3
- Currency scheduler implementasyonu

### Adim 4
- Hardcoded string migration

### Adim 5
- Audit komutlari

## Test ve Kabul Kriterleri
- Eksik settings key artik otomatik `"0"` olmayacak
- Eksik locale key kullaniciya debug metni gostermeyecek
- Currency scheduler DB ayarlariyla gercekten calisacak
- `catalog.enabled` kapatilinca katalog deterministic sekilde disable olacak
- `room.item.placement_limit`, `room.pets.placement_limit`, `trading.auto_exchange_redeemables` typed config ile okunacak
- Login MOTD `user.login.message.enabled` ve `user.login.message` ile calisacak
- Audit komutlari:
  - currency scheduler key'lerini refactor oncesi `unused`
  - locale'deki 3 olu key'i `unused`
  olarak gosterebilmeli

## Son Not
Bu refactor yalniz temizlik degildir. Etkisi:
- config hatalarini erken yakalar
- locale sistemini gercek hale getirir
- canli ortamda ayar davranislarini ongorulebilir yapar
- parity backlog'unda eklenecek yeni ozellikler icin saglam config/locale zemini kurar
