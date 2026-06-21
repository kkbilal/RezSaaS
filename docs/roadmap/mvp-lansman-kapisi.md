# MVP Lansman Kapısı

> Bu dosya ADR-068 (yol haritası refactor) ile eklendi. Önceden MVP lansman
> eşiği örtük olarak Phase 3 = "tamamlandı" + frontend F7 = "başlanmadı"
> arasında kaybolduğu için burada **açık ve doğrulanabilir** bir kapı olarak
> tanımlanır.

## Amaç

"RezSaaS MVP'si ne zaman lanse edilebilir?" sorusuna tek, net ve test edilebilir
bir yanıt vermek. Bu kapı, Phase 4/5 özelliklerini (ödeme, analytics, CRM, SMS,
i18n) **içermez**; bunlar MVP sonrası genişlemedir.

## MVP Tanımı

MVP; tek domain altında anonim keşif, paylaşılabilir işletme sayfaları, hesap
gerekli rezervasyon isteği, işletme onaylı randevu (`PendingApproval → Approved/
Declined/Expired/Superseded`), zorunlu e-posta bildirimi, temel abuse önleme ve
denetlenebilir tenant/rol/audit altyapısıdır (bkz. `docs/00-kapsam-ozeti.md`,
ADR-005/006/007/008/009).

## Lansman Kapısı Kontrol Listesi

Her madde açıkça doğrulanmış olmalıdır.

### Backend (Phase 0-3)

- [x] **Phase 0 - Keşif ve Karar:** kapsam, glossary, yetki matrisi, state machine,
  veri envanteri taslağı ve bildirim kanalı kararı tamamlandı.
- [x] **Phase 1 - Çekirdek SaaS:** modüler monolith iskeleti, tenancy (`tenant_id`
  global query filter, ADR-024), RBAC, audit ve Identity (ADR-016/018/019/020).
- [x] **Phase 2 - Müşteri Keşif ve Rezervasyon MVP:** anonim keşif, işletme profili,
  slot bulma (`PendingApproval` bloklamaz, ADR-033), işletme onaylı rezervasyon
  (ADR-027/034/035), TTL (ADR-007), abuse limitleri (ADR-049), verified review.
- [x] **Phase 3 - Güvenlik ve Operasyon:** booking operasyon state geçişleri
  (ADR-062), abuse control-plane (ADR-048/050), tenant lifecycle (ADR-047),
  account closure workflow (ADR-050), platform bildirim outbox (ADR-057/058),
  operasyon reconciliation, backup/restore tatbikatı ve CI güvenlik kapıları
  (ADR-063).

### Frontend (F0-F6)

- [x] **F0:** Next.js iskeleti, OpenAPI client generation, route haritası.
- [x] **F1:** Tasarım sistemi primitive'leri, auth route'ları, session guard.
  (Storybook/a11y tooling kapanışı açık izleniyor — bkz. `docs/24`.)
- [x] **F2:** Public keşif ve işletme profili.
  (Backend facet/taksonomi endpoint'i ve Playwright/perf smoke kapanışı açık.)
- [x] **F3:** Auth, rezervasyon ve müşteri self-service.
  (Playwright booking journey ve error-envelope detayları açık.)
- [x] **F4:** İşletme talep kutusu.
  (Cursor pagination/search contract'ı ve Playwright business journey açık.)
- [x] **F5:** Platform control-plane (salt okunur + tenant lifecycle).
  (Provisioning/membership/abuse mutation'ları hâlâ kapalı.)
- [~] **F6:** İşletme operasyon derinliği — appointment calendar/ops + profil
  ayar formu tamamlandı; **settings CRUD (branch/staff/service/variant/working
  hours/resource) Phase 5a backend contract'larını bekliyor.**
  - F6.1 Appointment operasyonları: tamamlandı.
  - F6.2 Settings CRUD: **Phase 5a'ya bağımlı** (MVP için zorunlu mu? Aşağıda).
  - F6.3 Verified review operasyonu: tamamlandı.

### F7 - Lansman Sertleşmesi (MVP için zorunlu kapı)

- [ ] Kritik customer/business/platform journey E2E paketi (Playwright).
- [ ] WCAG 2.2 AA otomatik + manuel kontrol.
- [ ] Mobil gerçek cihaz ve düşük ağ profili testi.
- [ ] Core Web Vitals (`LCP <= 2.5s`, `INP <= 200ms`, `CLS <= 0.1`) ölçüm planı.
- [ ] SEO, canonical, sitemap, robots, structured-data doğrulaması.
- [ ] Error monitoring, correlation id görünürlüğü ve PII redaction.
- [ ] Dependency/secret tarama ve frontend security header doğrulaması.
- [ ] Production cookie/origin/reverse proxy smoke testi.
- [ ] İçerik, boş durum, hata metni ve Türkçe dil QA.

### Açık Sorular (blokaj değerlendirmesi)

`docs/12-acik-sorular.md` içindeki soruların MVP lansmanı **bloke eden** olup
olmadığı değerlendirilmeli (bkz. `docs/12` yeni "Faz Blokaj İzi" bölümü).
MVP'yi bloke ettiği değerlendirilen başlıca maddeler:

- [ ] Production SMTP sağlayıcısı seçimi + secret yönetimi + confirmation/password
  reset teslimatının uçtan uca doğrulanması (ADR-019 fail-fast kuralı).
- [ ] Platform admin/işletme yönetimi için MFA enrollment UI + güvenilir cihaz
  davranışı (ADR-059).
- [ ] İlk `PlatformAdmin` bootstrap token runbook'u (ADR-026/044).
- [ ] `responseBuffer` ve hazırlık/temizlik buffer süresi kararları.
- [ ] Booking idempotency kayıtları saklama/temizleme politikası.

### CI ve Operasyon Kapıları (ADR-063)

- [x] Backup/restore tatbikat scriptleri (`scripts/Backup-Postgres.ps1`,
  `scripts/Verify-PostgresRestore.ps1`, `docs/27`).
- [x] Genel incident runbook'u (`docs/28`).
- [ ] Build/test/OpenAPI contract drift CI kapısı (ADR-063 — tam otomasyon bekler).
- [ ] Secret scan + dependency audit + CodeQL SAST workflow'ları.

## F6.2 (Settings CRUD) MVP İçin Zorunlu mu?

**Karar gerekli (ürün çağrısı).** İki seçenek:

1. **MVP'de işletme kendi branch/staff/service/resource/working-hours ayarlarını
   panel üzerinden yönetemeyebilir** — bu durumda kurulum platform admin veya
   migration seed (rol üretemez, ADR-021) üzerinden desteklenir ve Phase 5a
   MVP sonrasına bırakılır. Bu, lansmanı hızlandırır ama işletme self-servisini
   sınırlar.
2. **MVP'de self-servis kurulum isteniyorsa** Phase 5a'nın en azından branch/
   staff/service/variant/working-hours CRUD kısmı MVP kapısına dahil edilmelidir.

Varsayılan tavsiye: **Seçenek 2'nin minimal alt kümesi** (branch + staff + service
+ variant + working hours) MVP kapısına dahil edilir; resource CRUD ve çoklu şube
karşılaştırma Phase 5a'nın MVP sonrası kalanı olarak bırakılır. Bu karar ADR olarak
kaydedilmelidir.

## Lansman Sonrası Sıralama (özet)

Aşağıdaki sıra, bağımlılık ve risk profiline göre önerilir; her biri ayrı faz
dosyasına sahiptir:

1. **Phase 5a** (kalan settings CRUD) — F6.2 kapanışı.
2. **Phase 4a** (depozito + no-show) — en küçük ödeme adımı.
3. **Phase 5b** (Analytics) — gerçek metriklerle dashboard.
4. **Phase 5c** (Açık API + webhook) — dış entegrasyon.
5. **Phase 5d** (SMS/WhatsApp/İYS) — bildirim genişleme.
6. **Phase 4b** (tam ön-ödeme + iptal politikası) — 4a sonrası.
7. **Phase 4c** (paket/membership/gift card) — opsiyonel.
8. **Phase 5e** (marketplace + i18n) — en yüksek riskli, en son.

## Bu Kapıyı Açma Yetkisi

Lansman kapısı; ürün sahibi ve platform admin onayıyla, yukarıdaki kontrol
listesinin işaretlenmesi ve kalan blokajların ADR ile kapatılmasıyla açılır.
Kapı "büyük ölçüde tamamlandı" ifadeleriyle örtülü kapatılamaz; her madde açıkça
doğrulanır.