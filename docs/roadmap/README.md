# Faz Bazlı Yol Haritası

Bu yol haritası "kod yazmadan önce doğru parçalama" prensibiyle hazırlandı.
Her faz, **teslimat odaklı** ve test edilebilir çıktılara bağlanır.

> **2026-06-20 refactor (ADR-068):** Önceden tek parça olan Phase 4 ve Phase 5,
> bağımsız shiplenebilir alt fazlara ayrıldı. Ayrıca örtük olan MVP lansman
> eşiği açık bir kapıya (`mvp-lansman-kapisi.md`) taşındı. Eski `docs/15` ve
> `docs/18` uygulama planları superseded olarak işaretlendi.

## Fazlar

### Tamamlananlar (MVP çekirdeği)

- `phase-0-kesif-karar.md`: Ürün tanımı, glossary, yetki matrisi, açık kararlar.
- `phase-1-cekirdek-saas.md`: Çekirdek SaaS iskeleti, tenancy, RBAC, audit, Identity.
- `phase-2-musteri-kesif-rezervasyon-mvp.md`: Keşif, işletme sayfası, işletme onaylı rezervasyon.
- `phase-3-guvenlik-operasyon.md`: Güvenlik sertleşmesi, abuse/tenant/closure, backup/restore, operasyon.

### MVP Lansman Kapısı

- `mvp-lansman-kapisi.md`: "MVP ne zaman lanse edilir?" sorusunun açık, doğrulanabilir
  kontrol listesi. Phase 4/5 özelliklerini **içermez**.

### Ödeme (eski Phase 4 → 4a/4b/4c)

- `phase-4a-depozito-ve-no-show.md`: Hosted checkout + depozito + no-show + webhook imza.
- `phase-4b-tam-on-odeme-ve-iptal-politikasi.md`: Tam ön-ödeme + iptal politikası + chargeback. *(4a sonrası)*
- `phase-4c-gelir-genisleme.md`: Paket/membership/gift card. *(opsiyonel, ürün doğrulaması sonrası)*

### Platformlaşma (eski Phase 5 → 5a/5b/5c/5d/5e)

- `phase-5a-isletme-yonetim-crud.md`: Business settings CRUD + gelişmiş yetki ağacı. **Frontend F6.2'yi bloke eden tek backend ön koşul.**
- `phase-5b-analytics-modulu.md`: Analytics modülü (occupancy/no-show/dönüşüm/top services).
- `phase-5c-acik-api-ve-webhook.md`: Public API auth/scope + webhook delivery worker + business integration mutation'ları.
- `phase-5d-mesajlasma-genisleme.md`: SMS + WhatsApp Business pilotu + İYS/kanal tercihleri.
- `phase-5e-platform-buyume-ve-i18n.md`: Marketplace + i18n/locale/currency + provider abstraction + tenant taşıma. *(en yüksek riskli, en son)*

## Faz Geçiş İlkesi

- Bir fazın koduna başlamadan önce teslimatları ve kabul kriterleri gözden geçirilir.
- Güvenlik, tenancy ve audit minimumları sonraki fazlara ertelenmez.
- Açık ürün kararları `../12-acik-sorular.md`, alınan kararlar `../06-karar-kaydi.md`
  içinde izlenir. Her alt faz, bağımlı olduğu açık soruları ve ADR'leri kendi
  dosyasında listeler.

## Mevcut Durum

- **Phase 0-3:** tamamlandı (MVP çekirdeği).
- **MVP Lansman Kapısı:** açık. Backend Phase 0-3 ✓; frontend F0-F5 ✓, F6 kısmen
  (appointment ops ✓, settings CRUD Phase 5a'yı bekliyor), F7 (lansman sertleşmesi)
  başlanmadı. Bir dizi açık soru ve CI otomasyon kapısı bekliyor (detaylar
  `mvp-lansman-kapisi.md`).
- **Phase 4a/4b/4c:** başlamadı. `Payments` modülü readiness temeli hazır (ADR-065).
- **Phase 5a:** ✓ tamamlandı. Business settings CRUD backend endpoint'leri ve frontend sayfaları hazır.
- **Phase 5b:** ⏳ temel kuruldu (read model'ler ve DbContext hazır), application services ve endpoint'ler bekliyor.
- **Phase 5c:** `Integrations` modülü readiness + application lifecycle servisleri
  hazır (ADR-066/067); business mutation, public API, webhook worker bekliyor.
- **Phase 5d/5e:** başlamadı.

## Önerilen Uygulama Sırası (MVP sonrası)

Bağımlılık ve risk profiline göre önerilen sıra (`mvp-lansman-kapisi.md` ile aynı):

1. `phase-5a` (kalan settings CRUD) — F6.2 kapanışı.
2. `phase-4a` (depozito + no-show).
3. `phase-5b` (Analytics).
4. `phase-5c` (Açık API + webhook).
5. `phase-5d` (SMS/WhatsApp/İYS).
6. `phase-4b` (tam ön-ödeme + iptal politikası) — 4a sonrası.
7. `phase-4c` (paket/membership/gift card) — opsiyonel.
8. `phase-5e` (marketplace + i18n) — en yüksek riskli, en son.

> 5a/5b/5c/5d ve 4a/4b/4c arasında, önişkoşul olmadığı yerlerde paralel çalışma
> mümkündür. Her alt faz kendi dosyasında bağımlılıklarını belirtir.

## Frontend Paralel Planı

Frontend; ürün/backend fazlarıyla karışmaması için `F0-F7` olarak ayrı fakat
bağımlılıkları açık bir planla ilerler:

- Mimari ve tasarım kararları: `../23-frontend-mimari-tasarim-kararlari.md`
- Uygulama dilimleri (F0-F7 ve yeni F6 alt-dilimleri): `../24-frontend-uygulama-plani.md`

Frontend fazı, ihtiyaç duyduğu backend endpoint/authz/tenant sözleşmesi tamamlanmadan
sahte veri veya geçici güvenlik bypass'ı ile kapatılmaz.

### Frontend ↔ Backend eşleşme (refactor sonrası)

| Frontend | Backend dayanağı | Durum |
| --- | --- | --- |
| F0-F1 | Phase 1 Identity/Auth + mimari temel | ✓ |
| F2 | Phase 2 public discovery/profile API | ✓ (facet/perf açık) |
| F3 | Phase 2 booking + Phase 3 customer abuse | ✓ (Playwright açık) |
| F4 | Phase 2 business approve/decline + Phase 3 abuse report | ✓ (pagination açık) |
| F5 | Phase 3 tenant lifecycle + abuse/appeal/closure read | ✓ (mutation'lar kapalı) |
| F6.1 | Phase 3 appointment calendar/ops (ADR-062) | ✓ |
| F6.2 | **Phase 5a** settings CRUD | ✓ |
| F6.3 | Phase 2 verified review | ✓ |
| F7 | MVP Lansman Kapısı sertleşmesi | ⏳ |
| Ödeme UI | Phase 4a/4b/4c | ⏳ |
| Analytics UI | Phase 5b | ⏳ |
| Entegrasyon UI | Phase 5c | ⏳ |
| SMS/WhatsApp UI | Phase 5d | ⏳ |
| Marketplace/i18n UI | Phase 5e | ⏳ |

## Eski (Superseded) Dokümanlar

- `../15-phase-1-uygulama-plani.md`: superseded — içerik `phase-1-cekirdek-saas.md`
  ve ADR'ler tarafından kapsanır.
- `../18-phase-2-uygulama-plani.md`: superseded — içerik `phase-2-musteri-kesif-rezervasyon-mvp.md`
  ve ADR'ler tarafından kapsanır.

Eski tek parça `phase-4-odeme-gelir-optimizasyonu.md` ve
`phase-5-platformlastirma-genisleme.md` dosyaları 4a/4b/4c ve 5a/5b/5c/5d/5e
tarafından geçersiz kılındı.