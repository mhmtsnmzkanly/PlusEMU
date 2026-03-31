# PlusEMU Plugin System Modernization Talimatnamesi

## Amaç
Bu belge, PlusEMU içindeki mevcut minimal plugin yükleme mekanizmasını tam extensibility sağlayan resmi bir plugin platformuna dönüştürmek için uygulanabilir teknik talimatnamedir.

Hedef:
- plugin yükleme, başlatma, durdurma, unload ve sağlık yönetimini resmi hale getirmek
- command, interaction, game mode ve event hook yüzeylerini açmak
- plugin-owned user/config/shared data katmanını çekirdeğe eklemek
- plugin yazan geliştiricinin core kodu patch etmeden anlamlı özellik geliştirebilmesini sağlamak

Temel kararlar:
- backward compatibility zorunlu değildir
- mevcut `IPlugin` ve `IPluginDefinition` modeli kırılabilir
- yeni model manifest tabanlı olacaktır
- resmi storage modeli MariaDB üstünde ortak plugin tabloları üzerinden çalışacaktır

## Versioning Policy
Plugin ekosistemi stabil tutulmak için her katmanda açık version contract uygulanacaktır.

Zorunlu version alanları:
- `manifestVersion`
- `apiVersion`
- `abiVersion`
- `minEmulatorVersion`
- `targetEmulatorVersion`

Kurallar:
- `manifestVersion` manifest dosya formatını temsil eder
- `apiVersion` plugin-facing interface setini temsil eder
- `abiVersion` binary uyumluluğu temsil eder
- emulator yalnız uyumlu `abiVersion` taşıyan pluginleri load eder
- `manifestVersion` eskiyse manifest upgrader çalıştırılır veya load reddedilir

Örnek:
- `manifestVersion: 2`
- `apiVersion: 1`
- `abiVersion: 1`
- `minEmulatorVersion: 2026.1`

## Mevcut Durum Analizi
Mevcut sistem aşağıdaki dosyalarda görülen dar bir başlangıç yükleme mekanizmasıdır:
- [Program.cs](/home/duldul/Belgeler/PlusEMU/Program.cs)
- [IPlugin.cs](/home/duldul/Belgeler/PlusEMU/Plugins/IPlugin.cs)
- [IPluginDefinition.cs](/home/duldul/Belgeler/PlusEMU/Plugins/IPluginDefinition.cs)
- [IPluginsCache.cs](/home/duldul/Belgeler/PlusEMU/Plugins/IPluginsCache.cs)
- [PluginsCache.cs](/home/duldul/Belgeler/PlusEMU/Plugins/PluginsCache.cs)
- [PluginLoadContext.cs](/home/duldul/Belgeler/PlusEMU/Plugins/PluginLoadContext.cs)

Mevcut davranış:
- `Program.cs` başlangıçta `plugins/` klasöründeki DLL'leri yükler
- assembly içinden `IPluginDefinition` implementasyonları bulunur
- plugin `ConfigureServices` ile DI'a ek servis yazabilir
- `PluginsCache.Start()` her plugin için `Start()` çağırır

Mevcut sınırlar:
- manifest yok
- dependency beyanı yok
- capability modeli yok
- stop/unload/reload lifecycle yok
- plugin health ve runtime durum modeli yok
- resmi hook/event bus yok
- command registration API yok
- interaction extensibility resmi değil
- game mode registry yok
- plugin-owned data/storage katmanı yok
- `IPluginsCache` fiilen boş

Sonuç:
mevcut yapı "DLL yükle + DI genişlet" düzeyindedir. Gerçek gameplay, command, room behavior ve kalıcı plugin verisi için yeterli değildir.

## Hedef Mimari
Yeni sistem aşağıdaki çekirdek parçalardan oluşacaktır:
- `PluginManifest`
- `PluginDescriptor`
- `PluginManager`
- `PluginState`
- `PluginCapability`
- `PluginContext`
- `PluginRuntime`

Yeni lifecycle:
1. discover
2. validate
3. resolve dependencies
4. load
5. initialize
6. start
7. stop
8. unload

Plugin durumları:
- `Discovered`
- `Loaded`
- `Initialized`
- `Running`
- `Stopped`
- `Disabled`
- `Failed`

Her plugin için:
- ayrı load context korunur
- structured log context atanır
- capability doğrulaması yapılır
- version/ABI uyuşmazlığı load öncesi reddedilir

## Manifest Modeli
Her plugin dağıtımı bir manifest taşıyacaktır.

Önerilen dosya:
- `plugin.json`

Zorunlu alanlar:
- `id`
- `name`
- `version`
- `author`
- `description`
- `entryAssembly`
- `entryType`
- `targetEmulatorVersion`
- `capabilities`

Opsiyonel alanlar:
- `dependencies`
- `loadOrderHint`
- `configNamespace`
- `localeNamespace`
- `migrations`
- `defaultEnabled`

Örnek capability grupları:
- `Commands`
- `Interactions`
- `GameModes`
- `PacketHooks`
- `DomainEvents`
- `BackgroundJobs`
- `UserDataRead`
- `UserDataWrite`
- `SettingsRead`
- `SettingsWrite`
- `SharedDataRead`
- `SharedDataWrite`

## Dependency Version Policy
Plugin dependency cozumleme semver araliklari ile yapilacaktir.

Zorunlu dependency alanlari:
- `pluginId`
- `versionRange`
- `optional`

Desteklenecek range ornekleri:
- `>=2.1 <3.0`
- `=1.4.2`
- `^2.3`
- `~1.8`

Kurallar:
- `optional: false` dependency cozulmezse plugin load reddedilir
- `optional: true` dependency cozulmezse plugin degraded mode ile acilabilir
- ayni dependency icin celisen version range varsa plugin `Failed` durumuna alinir
- dependency resolution deterministic sirada calisir
- semver parser emulator icinde tek bir resmi uygulama kullanir

## Runtime Permission Enforcement
Capability beyanı yalnız metadata olmayacak, runtime enforcement ile uygulanacaktır.

Varsayılan politika:
- deny by default
- explicit capability grant
- plugin-scoped resource access

Zorunlu enforcement alanları:
- file access
- outbound HTTP
- DB raw query
- scheduler/background task spawn
- packet interception scope
- room mutation
- user data write

Kurallar:
- plugin doğrudan `IServiceProvider` root erişimi almayacak
- plugin yalnız `IPluginContext` içinden izinli servisleri görecek
- DB erişimi varsayılan olarak plugin-scoped store servisleri ile yapılacak
- raw query capability ayrı ve yüksek riskli izin olarak değerlendirilecek
- packet hook izni direction ve packet family bazında scope edilecek

## Concurrency Contract
Plugin callback ve runtime hook davranisi acik concurrency contract ile tanimlanacaktir.

Temel kurallar:
- room-scoped hook'lar room execution context affinity ile calisir
- ayni room icindeki game tick ve interaction hook'lari paralel calistirilmaz
- command execution ile background job ayni plugin state'i mutate edecekse plugin thread-safe olmak zorundadir
- user data mutate operasyonlari resmi service uzerinden yapilir ve atomic mutate helper kullanir

Thread affinity kurallari:
- room lifecycle hook'lari room context affinity tasir
- global background job'lar room context disinda calisir
- packet hook'lari session/network pipeline context'inde calisir

Async kurallari:
- tum async hook'lar cancellation token kabul etmelidir
- timeout'a ugrayan hook warning + metric uretir
- timed-out hook sonrasi emulator ana loop bloklanmaz

Lock ownership kurallari:
- core lock'larini plugin kodu sahiplenmez
- plugin kendi ic state lock'larini kendisi yonetir
- room state mutasyonu yalniz room execution context icinden yapilir

## Çekirdek Public API
Yeni sistemin resmi public yüzeyi aşağıdaki tiplerden oluşacaktır:

Runtime ve lifecycle:
- `IPluginModule`
- `IPluginLifecycle`
- `IPluginContext`
- `IPluginManager`
- `IPluginRegistry`

Hook yüzeyleri:
- `IIncomingPacketHook`
- `IOutgoingPacketHook`
- `IDomainEventSubscriber`
- `IPluginBackgroundJob`

Command yüzeyleri:
- `IPluginChatCommand`
- `IPluginConsoleCommand`
- `IPluginRconCommand`

Interaction yüzeyleri:
- `IInteractionDefinition`
- `IInteractionHandler`
- `IInteractionRegistry`

Game yüzeyleri:
- `IGameModeDefinition`
- `IGameMode`
- `IGameModeRegistry`
- `IGameInteractionHandler`
- `IGameTeamPolicy`

Plugin data yüzeyleri:
- `IPluginUserDataDefinition<TState>`
- `IPluginUserDataService`
- `IPluginUserDataStore`
- `IPluginSettingsService`
- `IPluginSettingsStore`
- `IPluginDataService`
- `IPluginDataStore`
- `IPluginUserDataBag`

## Command Plugin Sistemi
Pluginler resmi olarak komut ekleyebilmelidir.

Desteklenecek komut tipleri:
- chat command
- console command
- RCON command

Her komut aşağıdaki metadata'yı taşımalıdır:
- `Name`
- `Aliases`
- `Permission`
- `Scope`
- `Description`
- `Usage`

Desteklenecek scope tipleri:
- `RoomOnly`
- `Global`
- `StaffOnly`
- `ConsoleOnly`
- `RconOnly`

Örnek plugin komutları:
- `:eventstart`
- `:daily`
- `:unstuck`
- `:faction`
- `:rp`
- `:profile`

Kural:
- command registration reflection hilesiyle değil, resmi registry üzerinden yapılacaktır
- plugin unload olduğunda komut kayıtları temizlenir
- reload sonrası duplicate registration oluşmaz

## Interaction Plugin Sistemi
Pluginler yeni furni/interaction davranışları ekleyebilmelidir.

Mevcut kodda bu ihtiyaç açıkça görülmektedir:
- [ItemDefinition.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Items/ItemDefinition.cs) içinde `InteractionType` için plugin extensibility notu vardır

Karar:
- `InteractionType` kapalı enum/int mantığına sıkı bağlı kalmayacak
- v2 modelde interaction kimliği string/registry tabanlı olacaktır

Önerilen yapı:
- item definition içinde `InteractionKey`
- core bir `InteractionRegistry`
- plugin kendi interaction tanımını register eder

Desteklenecek handler aşamaları:
- `OnPlace`
- `OnUse`
- `OnRemove`
- `OnTick`
- `OnUserWalkOn`
- `OnUserWalkOff`
- `OnStateChange`

Örnek plugin interaction'ları:
- `roleplay.job_board`
- `roleplay.bank_terminal`
- `events.daily_reward_terminal`
- `games.capture_flag_tile`
- `moderation.room_scanner`

Kural:
- plugin bir interaction eklemek için core switch-case patch etmeye ihtiyaç duymamalı
- item definition lookup registry üstünden çözülmelidir

## Plugin Migration Engine
Manifest içindeki migration tanımları resmi bir execution engine ile yönetilecektir.

Zorunlu davranış:
- ordered steps
- exactly-once execution
- migration history table
- idempotent migration contract
- failed migration recovery

Önerilen tablo:
- `plugin_migration_history`

Kolonlar:
- `plugin_id`
- `migration_id`
- `applied_at`
- `success`
- `duration_ms`

Kurallar:
- aynı migration iki kez çalıştırılmaz
- migration sırası manifestte açıkça belirlenir
- plugin start aşaması migration tamamlanmadan devam etmez
- rollback desteklenmeyecekse bu açıkça belirtilir
- rollback varsa explicit migration step olarak yazılır

## Game Mode Plugin Sistemi
Pluginler `BattleBanzai`, `Soccer`, `Freeze` benzeri yeni oyun modları ekleyebilmelidir.

Mevcut sınır:
- [Room.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Rooms/Room.cs) içinde `_banzai`, `_freeze`, `_soccer` özel alanlar olarak tutuluyor
- oyun mantığı [GameManager.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Rooms/Games/GameManager.cs) ve mevcut game sınıflarına sıkı bağlı

Karar:
- room içindeki hardcoded oyun alanları zamanla registry tabanlı `ActiveGameMode` modeline taşınacaktır
- `BattleBanzai`, `Soccer`, `Freeze` yeni API için referans implementasyon olarak korunacaktır

Yeni game plugin API gereksinimleri:
- `GameId`
- `Name`
- `SupportsTeams`
- `CanActivate(room)`
- `Start(room)`
- `Stop(room)`
- `Reset(room)`
- `OnUserJoin`
- `OnUserLeave`
- `OnUserWalk`
- `OnItemTriggered`
- `OnTick`
- `GetWinner`
- `RewardPolicy`

Örnek yeni oyun modları:
- capture the flag
- king of the hill
- infection
- parkour race
- team survival
- cooperative puzzle

Kural:
- yeni oyun modu eklemek için `Room` içine yeni field açılmayacak
- aktivasyon `IGameModeRegistry` üzerinden yapılacak

## Plugin-Owned Data Katmanı
Pluginler kullanıcıya, room'a, event'e veya global sisteme ait kalıcı veri tutabilmelidir.

Bu katman resmi olarak üçe ayrılır:
- kullanıcıya özel plugin verisi
- plugin genel ayarları
- plugin shared/system verisi

### 1. Kullanıcıya özel plugin verisi
Tablo:
- `user_plugin_data`

Kolonlar:
- `user_id`
- `plugin_id`
- `data_key`
- `schema_version`
- `data_json`
- `created_at`
- `updated_at`

Unique key:
- `(user_id, plugin_id, data_key)`

Kullanım örnekleri:
- roleplay karakter profili
- roleplay statlar
- sezon görev ilerlemesi
- custom reputation
- mini game MMR

Kural:
- plugin kullanıcı verisi `users` veya `user_statistics` içine yeni kolon açarak tutulmayacak
- tüm plugin-owned user state bu tabloya yazılacak

## Resource Quota Policy
Capability verilmesi tek basina yeterli olmayacak; plugin runtime quota ile sinirlanacaktir.

Zorunlu quota alanlari:
- outbound HTTP rate
- background job concurrency
- command execution timeout
- packet hook CPU budget
- DB operation concurrency
- log flood suppression

Varsayilan politika:
- quota asiminda once throttle, sonra warning, sonra feature-local deny uygulanir
- tek bir plugin quota ihlali emulator genel sagligini bozmaz

Ornek kontroller:
- dakikadaki outbound HTTP istegi siniri
- ayni anda calisabilecek background task sayisi
- bir packet hook icin maksimum sure
- bir plugin icin maksimum per-minute DB write sayisi

### 2. Plugin genel ayarları
Tablo:
- `plugin_settings`

Kolonlar:
- `plugin_id`
- `setting_key`
- `setting_value`
- `value_type`
- `updated_at`

Unique key:
- `(plugin_id, setting_key)`

Kullanım örnekleri:
- roleplay XP oranı
- plugin feature toggle
- cooldown limitleri
- event tuning parametreleri

Kural:
- `server_settings` yalnız emulator core ayarları için kalacaktır
- plugin config bu tabloya namespace'li olarak yazılacaktır

### 3. Plugin shared/system verisi
Tablo:
- `plugin_data`

Kolonlar:
- `plugin_id`
- `data_key`
- `scope_type`
- `scope_id`
- `schema_version`
- `data_json`
- `created_at`
- `updated_at`

Unique key:
- `(plugin_id, data_key, scope_type, scope_id)`

Kullanım örnekleri:
- faction state
- room control state
- event leaderboard metadata
- active campaign world state

Scope örnekleri:
- `global`
- `room`
- `group`
- `event`

## Plugin User Data Runtime Akışı
Plugin-owned user state, mevcut user loading zincirine doğal şekilde bağlanacaktır.

Bağlanacak merkez:
- [UserDataFactory.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Users/UserData/UserDataFactory.cs)
- [IUserDataLoadingTask.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Users/UserData/IUserDataLoadingTask.cs)
- [Habbo.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Users/Habbo.cs)

Yeni akış:
1. core `Habbo` temel alanlarını yükler
2. built-in `IUserDataLoadingTask` implementasyonları çalışır
3. `LoadPluginUserDataTask` devreye girer
4. plugin manifestinde `OnLogin` load mode isteyen state'ler yüklenir
5. state `Habbo.PluginData` içine konur
6. plugin runtime boyunca state'i mutate eder
7. dirty state disconnect veya periyodik flush sırasında kaydedilir

Habbo üstüne eklenecek runtime alan:
- `PluginData`

Kural:
- plugin-specific property'ler `Habbo` üstüne tek tek eklenmeyecek
- tüm plugin-owned state data bag içinde tutulacak

## Observability Standardı
Her plugin ve her plugin capability aşağıdaki minimum metrikleri üretmelidir:
- success count
- failure count
- latency histogram
- timeout count
- retry count
- DB read duration
- DB write duration
- packet send frequency

Plugin sistemine özel zorunlu ölçümler:
- plugin load duration
- plugin initialize duration
- plugin start duration
- plugin stop duration
- migration duration
- command execution latency
- interaction execution latency
- game tick handler latency
- user data load/save latency

Log standardı:
- her log `plugin_id`, `plugin_version`, `plugin_state` alanlarını taşımalı
- failed plugin load için tek satırlık özet ve ayrıntılı exception birlikte tutulmalı

## Rollback Strategy
Yeni plugin runtime bileşenleri production’a legacy yol korunarak alınacaktır.

Kurallar:
- feature flags ile `PluginManagerV2`, `InteractionRegistryV2`, `GameModeRegistryV2` ayrı ayrı kapatılabilir olmalı
- legacy code path en az ilgili faz tamamlanana kadar korunmalı
- manifest v2 load başarısız olursa plugin disabled duruma çekilmeli
- migration başarısızlığında plugin start bloke edilmeli, emulator start değil
- unload/reload bozulursa plugin hard-disabled yapılıp process çalışmaya devam etmeli

Özellikle rollback gerektiren alanlar:
- manifest v2
- runtime capability enforcement
- interaction registry
- game mode registry
- plugin-owned storage load/save zinciri

## Plugin Data Erişim Kuralları
Her plugin yalnız kendi namespace'inde veri okuyup yazabilir.

Kurallar:
- plugin `plugin_id` dışındaki `user_plugin_data` kayıtlarına erişemez
- plugin başka plugin'in `plugin_settings` alanlarına erişemez
- plugin başka plugin'in `plugin_data` state'ine erişemez
- cross-plugin veri paylaşımı v1'de desteklenmez

İsimlendirme önerisi:
- `roleplay.profile`
- `roleplay.stats`
- `seasonpass.progress`
- `conquest.region_state`

## JSON ve Özel Tablo Politikası
Varsayılan resmi storage formatı JSON olacaktır.

Gerekçe:
- yeni plugin için migration maliyetini düşürür
- küçük ve orta ölçekli pluginleri hızlı ayağa kaldırır
- roleplay ve progression gibi state'ler için yeterlidir

Plugin'e özel tablo açma istisnası:
- veri hacmi çok yüksekse
- yoğun filtreleme/sıralama gerekiyorsa
- leaderboard veya raporlama SQL ağırlıklıysa
- JSON blob performans olarak yetersiz kalıyorsa

Bu durumda tablo adı:
- `plugin_<plugin_id>_*`

Örnekler:
- `plugin_roleplay_characters`
- `plugin_roleplay_factions`
- `plugin_conquest_regions`

Kural:
- özel tablo açma istisnadır
- plugin manifestinde beyan edilmelidir
- ownership plugin namespace'i ile açık olmalıdır

## Güvenlik ve İzolasyon
Plugin runtime aşağıdaki operational kuralları uygulamalıdır:
- plugin exception emulator process'ini düşürmemeli
- `Start` ve `Stop` timeout politikası olmalı
- başarısız plugin `Failed` durumuna alınmalı
- plugin logları `plugin_id` ile etiketlenmeli
- unload sonrası hook, command ve registry kayıtları temizlenmeli
- aynı `plugin_id` ile ikinci plugin yüklenmemeli
- dependency cycle durumunda load reddedilmeli

## Yönetim ve Operasyon
Yeni plugin manager aşağıdaki yönetim işlemlerini desteklemelidir:
- plugin listesi
- plugin detay görüntüleme
- plugin enable/disable
- plugin reload
- plugin unload
- plugin health/errors görüntüleme

Önerilen admin komutları:
- `plugins_list`
- `plugin_info <id>`
- `plugin_enable <id>`
- `plugin_disable <id>`
- `plugin_reload <id>`
- `plugin_errors <id>`

## Geçiş Planı
### Faz 1
- manifest modeli
- `PluginManager`
- yeni lifecycle
- plugin state/health modeli

### Faz 2
- command registry
- packet/domain hook registry
- structured logging

### Faz 3
- interaction registry
- item interaction key migration

### Faz 4
- game mode registry
- mevcut `BattleBanzai`, `Soccer`, `Freeze` adaptasyonu

### Faz 5
- `user_plugin_data`
- `plugin_settings`
- `plugin_data`
- plugin data bag ve load/save pipeline

## Test Planı
Lifecycle:
- geçerli manifestli plugin discovery ve load
- eksik manifest alanı olan plugin reject
- duplicate `plugin_id` reject
- dependency sırası doğru çözülmeli
- start exception izolasyonu sağlanmalı
- unload sonrası load context ve registry kayıtları temizlenmeli

Commands:
- chat command registration çalışmalı
- unload sonrası command kaydı silinmeli
- reload sonrası duplicate command oluşmamalı
- 100 hot reload döngüsü sonrası memory kullanımı stabil kalmalı
- unload sonrası static reference, timer ve delegate sızıntısı kalmamalı

Interactions:
- plugin yeni interaction register edebilmeli
- item interaction resolution registry üstünden yapılmalı
- plugin unload olunca interaction devre dışı kalmalı

Game modes:
- plugin yeni game mode register edebilmeli
- room içinde game activation registry ile çözülmeli
- referans oyunlar yeni API ile uyumlu çalışmalı

Plugin data:
- olmayan kullanıcı verisi default state ile oluşmalı
- JSON state doğru deserialize edilmeli
- invalid JSON warning log + default state davranışı vermeli
- dirty state disconnect'te kaydedilmeli
- cross-plugin access engellenmeli
- plugin config kendi namespace'inde okunup yazılmalı

Migration engine:
- migration adımları doğru sırada çalışmalı
- aynı migration ikinci kez uygulanmamalı
- failed migration plugin'i `Failed` durumuna almalı
- migration history tablosu doğru kayıt üretmeli

Permission enforcement:
- capability verilmeyen plugin raw DB erişimi yapamamalı
- capability verilmeyen plugin packet interception kaydı yapamamalı
- outbound HTTP ve file access policy deny-by-default davranmalı

Dependency and concurrency:
- semver range celiskisinde plugin load reddedilmeli
- optional dependency yoklugunda degraded mode davranisi deterministic olmali
- ayni room icinde interaction ve game hook'lari race condition uretmemeli
- timeout olan async hook room ana loop'unu kilitlememeli

Quota:
- outbound HTTP quota asildiginda throttle metric uretmeli
- background job concurrency siniri uygulanmali
- packet hook CPU budget asildiginda warning ve suppression davranisi gorulmeli

## Kabul Kriterleri
- plugin yazarı core kodu patch etmeden chat command ekleyebilmeli
- plugin yazarı core kodu patch etmeden yeni interaction ekleyebilmeli
- plugin yazarı yeni game mode ekleyebilmek için room içine field açmak zorunda olmamalı
- roleplay benzeri plugin kullanıcı profili ve stat verisini resmi storage katmanında tutabilmeli
- plugin config `server_settings` içine karışmadan ayrı tutulmalı
- plugin failure emulator shutdown'a dönüşmemeli

## Varsayımlar
- hedef runtime .NET ve mevcut DI altyapısı korunacaktır
- MariaDB ana storage backend olarak kalacaktır
- v1'de resmi veri formatı JSON tabanlı ortak tablolar olacaktır
- backward compatibility tercih değil, gerekirse kırılacaktır
