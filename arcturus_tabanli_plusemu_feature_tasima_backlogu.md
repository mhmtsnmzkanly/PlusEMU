# Arcturus Tabanli PlusEMU Feature Tasima Backlogu

## Amac
Bu belge, Arcturus'ta bulunan ve PlusEMU'ye tasinmasi veya parity olarak tamamlanmasi mantikli gorunen feature alanlarini onceliklendirir.

## Kullanım Sekli
Bu belge "Arcturus'ta var, burada da aynen kopyalayalim" listesi degildir.
Hedef:
- davranis olarak kritik eksikleri kapatmak
- Nitro uyumunu artirmak
- mevcut PlusEMU mimarisini bozmadan feature parity kazanmak

## Onceliklendirme Kurali

### P0
Client bozulmasi, packet time-out, bos UI, login/room/moderation akis bozulmasi

### P1
Kullanicinin dogrudan fark ettigi ama kritik olmayan Nitro UX eksikleri

### P2
Legacy/community ozellikleri, operasyonel kolayliklar, plugin/genisletilebilirlik iyilestirmeleri

## P0 Feature Tasima Alani

### 1. Targeted Offer Akisi
Durum:
- PlusEMU tarafinda event dosyalari var ama implementasyon backlog gorunuyor.

Kaynaklar:
- [GetNextTargetedOfferEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Catalog/GetNextTargetedOfferEvent.cs)
- [PurchaseTargetedOfferEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Catalog/PurchaseTargetedOfferEvent.cs)
- [SetTargetedOfferStateEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Catalog/SetTargetedOfferStateEvent.cs)
- [ShopTargetedOfferViewedEvent.cs](/home/duldul/Belgeler/PlusEMU/Communication/Packets/Incoming/Catalog/ShopTargetedOfferViewedEvent.cs)

Neden oncelikli:
- Nitro katalog UX'inde sessiz bozukluk uretebilir
- packet surface var gorundugu icin "calisiyor sanilan" ama gercekte eksik alan sinifina girer

Yapilacaklar:
- offer state modeli tanimla
- per-user persistence belirle
- composer ve purchase flow tamamla
- telemetry ekle

### 2. Guide / Guardian Yardim Akisi
Durum:
- `Kismi`

Tamamlanan ilk dilim:
- helper duty durumu
- guide request queue
- accept / decline
- session start / message / close
- helper report

Kalan ana bosluk:
- richer guardian sonuc UX'i

Arcturus'ta guardian sistemi ve scheduler izleri var:
- [GuardianTicketFindMoreSlaves.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/threading/runnables/GuardianTicketFindMoreSlaves.java)
- [GuardianNotAccepted.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/threading/runnables/GuardianNotAccepted.java)

Neden oncelikli:
- modern Help surface ile baglantili
- support/moderation deneyimini etkiler

Yapilacaklar:
- ticket state ownership
- claim/accept/timeout akisi
- moderator ve helper rollerinin ayrimi

### 3. Camera Runtime
Arcturus'ta camera reconnect ve client yapisi dogrudan var:
- [Emulator.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/Emulator.java#L66)
- [CameraClientAutoReconnect.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/threading/runnables/CameraClientAutoReconnect.java)

Neden oncelikli:
- modern istemci yuzeylerinden biri
- bagli packetler eksikse runtime capability hissedilir sekilde dusuk kalir

Yapilacaklar:
- protocol/yuzey karari
- reconnect strategy
- timeout ve observability

## P1 Feature Tasima Alani

### 4. Wired Highscores
Arcturus tarafinda belirgin bir alt sistem olarak var:
- [WiredHighscoreManager.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/habbohotel/wired/highscores/WiredHighscoreManager.java)

Neden degerli:
- oyun odalari icin kullaniciya gorunen fark yaratir
- community content uyumunu artirir

Tasimada dikkat:
- mevcut wired runtime ile uyumlu registry lazim
- midnight reset ve score persistence ayri ele alinmali

### 5. HC Kickback / Payday Benzeri Subscription Davranislari
Arcturus'ta subscription scheduler ve payday akislari daha zengin:
- [SubscriptionScheduler.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/habbohotel/users/subscriptions/SubscriptionScheduler.java)
- [SubscriptionHabboClub.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/habbohotel/users/subscriptions/SubscriptionHabboClub.java)

Neden degerli:
- VIP ekonomisi ve retention hissini etkiler
- Nitro parity backlog'u ile dogrudan iliskili

Yapilacaklar:
- subscription event modeli
- scheduled payout mantigi
- duplicate grant onleme

### 6. Help / CFH UX Tamamlama
Arcturus support/CFH alani daha zengin:
- [ModToolManager.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/habbohotel/modtool/ModToolManager.java#L71)

PlusEMU moderasyon tabani var:
- [ModerationManager.cs](/home/duldul/Belgeler/PlusEMU/HabboHotel/Moderation/ModerationManager.cs#L107)

Neden degerli:
- temel veri modeli zaten mevcut
- packet ve ekran tamamlamasi ile hizli kazanc saglanabilir

## P2 Feature Tasima Alani

### 7. YouTube Runtime Zenginlestirme
Bu repo YouTube packetlerini destekliyor ama runtime scheduling tamligi dogrulanmali.
Arcturus referansi:
- [YoutubeAdvanceVideo.java](/home/duldul/Belgeler/Arcturus-Community-master/src/main/java/com/eu/habbo/threading/runnables/YoutubeAdvanceVideo.java)

### 8. Group Forums Derinligi
Packet ve alan izleri bu repoda var, fakat davranis tamligi ayri incelenmeli.

### 9. Plugin Event Surface Genisletme
Arcturus'ta domain event coverage daha yuksek.
Ama bu alan kopyalanarak degil, yeni ADR ile tasarlanarak alinmali.

## Tasimama Gerekenler
- Arcturus'taki monolitik manager tasarimini aynen kopyalamak
- static/global erişim desenlerini geri getirmek
- feature parity ugruna servis ayrisimini bozmak

## Onerilen Uygulama Sirasi
1. Targeted Offer
2. Guide / Guardian
3. Camera
4. Help / CFH UX tamamlama
5. HC kickback / subscription eventleri
6. Wired highscores
7. YouTube runtime iyilestirmeleri
8. Forum completeness
9. Plugin event surface ADR

## Kabul Kriterleri
- her tasinan feature icin incoming/outgoing packetler birlikte tamamlanmali
- memory-only state birakilmamali
- reconnect ve restart sonrasi davranis korunmali
- build temiz kalmali
- feature, Nitro istemci davranisiyla canli test veya packet trace ile dogrulanmali

## Ilgili Belgeler
- [arcturus_plusemu_habbohotel_parity_analizi.md](/home/duldul/Belgeler/PlusEMU/arcturus_plusemu_habbohotel_parity_analizi.md)
- [arcturus_plusemu_packet_ve_nitro_farklari.md](/home/duldul/Belgeler/PlusEMU/arcturus_plusemu_packet_ve_nitro_farklari.md)
- [nitro_react_plusemu_parity_ve_ux_talimatnamesi.md](/home/duldul/Belgeler/PlusEMU/nitro_react_plusemu_parity_ve_ux_talimatnamesi.md)
