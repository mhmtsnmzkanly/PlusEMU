# PlusEMU Architecture Decision Record Talimatnamesi

## Amaç
Bu belge, PlusEMU icindeki buyuk mimari kararlarin kalici ve denetlenebilir bicimde kayda alinmasi icin ADR standardini tanimlar.

Hedef:
- `neden bu yapi secildi` sorusuna kalici cevap vermek
- gelecekte ayni tartismalarin yeniden acilmasini azaltmak
- plugin, parity, config ve runtime kararlarini versioned ve denetlenebilir hale getirmek

## Ne Zaman ADR Yazilacak
Asagidaki durumlarda ADR zorunludur:
- yeni public API veya runtime contract ekleniyorsa
- schema veya storage modeli belirleniyorsa
- enum -> registry gibi temel mimari donusum yapiliyorsa
- legacy yol yerine compatibility layer seciliyorsa
- rollback veya migration riski olan karar aliniyorsa
- operasyonel etkisi buyuk bir performans veya observability karari aliniyorsa

Ornek ADR konulari:
- neden plugin storage varsayilan olarak JSON secildi
- neden `InteractionType` enum modelinden registry modeline geciliyor
- neden typed settings API secildi
- neden Nitro uyumu icin adapter layer tercih edildi
- neden plugin capability enforcement deny-by-default tasarlandi

## Dosya Formati
ADR dosyalari ayri markdown belgeleri olarak tutulacaktir.

Onerilen klasor:
- `Belgeler/adr/`

Dosya adlandirma:
- `ADR-0001-kisa-baslik.md`
- `ADR-0002-kisa-baslik.md`

Durum alanlari:
- `Proposed`
- `Accepted`
- `Superseded`
- `Deprecated`
- `Rejected`

## ADR SabloNu
Her ADR asagidaki sabit bolumleri icerecektir:

### 1. Baslik
- kisa, net, karar odakli

### 2. Durum
- `Proposed`, `Accepted`, `Superseded`, `Deprecated`, `Rejected`

### 3. Tarih
- ISO tarih

### 4. Baglam
- problemi doguran teknik ve urunsel baglam

### 5. Karar
- secilen cozum

### 6. Alternatifler
- degerlendirilen ama secilmeyen alternatifler

### 7. Sonuclar
- olumlu etkiler
- olumsuz etkiler
- teknik borc

### 8. Rollback / Exit Strategy
- karar geri alinmak istenirse ne yapilacak

### 9. Gozlemlenebilirlik
- hangi metrik/log ile karar izlenecek

### 10. Ilgili Belgeler
- parity, plugin, config veya baska talimatname referanslari

## Ownership ve Review Workflow
ADR sureci yalniz belge yazimi degil, yonetisimi tanimli bir review akisi ile yurutulecektir.

Minimum roller:
- author
- primary reviewer
- architecture reviewer

Kurallar:
- ADR'yi degisikligi oneren muhendis veya agent acar
- en az 1 teknik reviewer zorunludur
- plugin/runtime/schema gibi yuksek etkili kararlar icin 1 architecture reviewer zorunludur
- `Accepted` durumu review tamamlanmadan verilemez
- `Superseded` karari onceki ADR'ye acik referans vermelidir

PR merge kurali:
- major architecture degisikligi ADR referansi olmadan merge edilmez
- PR aciklamasi ilgili ADR numarasini tasir
- ADR `Proposed` durumdaysa merge istisnai olabilir, ama follow-up kabul tarihi belirlenmelidir

Checklist:
- problem acik mi
- secilen karar net mi
- alternatifler yazildi mi
- rollback stratejisi var mi
- observability etkisi yazildi mi
- ilgili belgeler baglandi mi

## Zorunlu Kalite Kurallari
- her ADR tek bir ana karari anlatmali
- karar cumlesi muğlak olmamali
- "ileride bakariz" tipinde acik uc birakilmamali
- secilmeyen alternatifler mutlaka yazilmali
- rollback veya neden rollback olmadigi yazilmali
- observability etkisi belirtilmeli
- owner ve reviewer bilgisi belirtilmeli
- superseded iliskisi tek yonlu degil, cift yonlu izlenebilir olmali

## Oncelikli Yazilacak ADR'ler
- `ADR-0001`: Plugin storage modeli neden `user_plugin_data`, `plugin_settings`, `plugin_data`
- `ADR-0002`: Interaction enum modelinden registry modeline gecis
- `ADR-0003`: Typed settings API ve locale template standardi
- `ADR-0004`: Nitro parity icin adapter layer secimi
- `ADR-0005`: Plugin capability enforcement deny-by-default modeli

## Kabul Kriterleri
- bundan sonraki major architecture degisiklikleri ADR olmadan merge edilmemeli
- her talimatname en az ilgili ADR listesine referans verebilmeli
- superseded kararlar acik zincir halinde gorulebilmeli
- major kararlar en az 2 goz tarafindan incelenmeli
- yuksek riskli kararlar architecture reviewer onayi almali

## Varsayimlar
- ADR belgeleri teknik ekip icin yazilacak
- repo disi operasyon dokumani olarak `Belgeler` altinda tutulabilir
- ileride istenirse repo icine de tasinabilecek kadar sistematik olacak
