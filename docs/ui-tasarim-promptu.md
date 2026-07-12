# RezSaaS — UI/UX Tasarım Promptu (Sidebar + Role-Based Modern Layout, Maksimum Detay)

> **Bu prompt bir tasarım aracına (Figma Make, v0, Lovable, bolt.new, Cursor UI, Galileo AI, Visily, Mage vb.) yapıştırılmak üzere yazıldı.** Tasarım kararları için aşağıdaki ürün/rol/UX/domain/engineering kurallarına **birebir** sadık kal. Tüm UI metinleri Türkçe; kod/comment/route İngilizce kalır. Bu dosya ~30 bölümden oluşur ve bir tasarım sisteminin sıfırdan kurulması için gereken tüm kararları içerir. **Hiçbir kararı varsayıma bırakma; belirsizlik olursa en tutucu seçeneği seç ve gerekçeni yaz.**

---

## İÇİNDEKİLER

0. Yapıştırmalık özet
1. Rol, hedef, öncelikler, yapma
2. Ürün context'i, domain kuralları, terimler
3. Tasarım dili: renk paleti (full scale), tipografi (full type system), spacing, radius, elevation
4. Motion kataloğu (full)
5. Background, ikon dili, görsel hiyerarşi
6. Roller ve yetki matrisi
7. Rol → sidebar navigasyon haritası (her rol)
8. Sidebar tasarım spec'i (pixel-level)
9. Sayfa envanteri (rol bazlı, her sayfa için detailed layout)
10. UX kural seti (20 kural)
11. Edge case matrix (her sayfa × her state)
12. Component library (tokens → primitives → patterns → templates → pages)
13. Component API + state matrix (her component)
14. Form field library (field-by-field spec)
15. Dialog/modal/notification taxonomy
16. Etkileşim akış diyagramları (text-based, her kritik akış)
17. User journey (her rol için 1 tam gün)
18. Veri görüntüleme pattern'leri
19. Erişilebilirlik (WCAG 2.2 AA full checklist)
20. Responsive (breakpoint bazında her sayfa)
21. Performance budget
22. Micro-interaction kataloğu (her hover/click/focus)
23. Lokalizasyon hazırlığı
24. Güvenlik/PII/audit UI yansıması
25. Tasarım dosyası organizasyonu + Storybook story şablonu
26. Token dosyası örneği (CSS + JSON)
27. Kısıtlar (hard rules)
28. Teslimat beklentisi
29. Anti-pattern listesi
30. Quality gate checklist
31. Referans ilham linkleri
32. Stil örnekleri (CSS + Tailwind)

---

## 0. Yapıştırmalık Özet (tasarım aracına ilk satır olarak ver)

> "RezSaaS adlı çok-kiracılı salon/spa/klinik operasyon + onaylı rezervasyon SaaS ürünü için, **dark glassmorphism + indigo `#6366f1` / violet `#8b5cf6` paletli**, **rol-bazlı (Customer / Staff / BranchManager / BusinessOwner / PlatformAdmin / PlatformSupport)**, **sidebar-driven modern bir tasarım sistemi** üret. Her rolün yetki kapsamına göre şekillenen **collapse-edilebilir sidebar**, her ana fonksiyon için **odaklanmış ayrı sayfa**, onaylı rezervasyon domain kurallarını (PendingApproval kesin randevu değildir, 24 saat TTL, PII maskeleme, şube saati sadakati, resource müşteriye gizli, idempotity, double-booking engeli, step-up MFA) UI'ya yansıtan detaylı mockup'lar ver. Mock veri/sahte KPA üretme; backend bekleyen ekranlar 'yakında' placeholder. UI dili Türkçe; kod dili İngilizce. Aşağıdaki tam spec'i (~30 bölüm) oku ve uygula:"

---

## 1. Rol, Hedef, Öncelikler, Yapma

Sen **senior UI/UX tasarım mühendisi**, **tasarım sistemi mimarı** ve **front-end engineer**'ısın. Görevin: `RezSaaS` adlı çok-kiracılı **salon/spa/klinik/stüdyo operasyon + onaylı rezervasyon SaaS** ürünü için **rol-bazlı, sidebar-driven, modern bir tasarım sistemi** üretmek.

### 1.1 Öncelikler (sırayla,Boz)

1. **Sidebar-driven navigasyon** — her rolün kendi yetki kapsamına göre şekillenen, collapse-edilebilir, modern sidebar. Mobilde drawer'a dönüşür.
2. **Her işleme kendi sayfası** — her ana fonksiyon ayrı, odaklanmış sayfada yaşanır. Dashboard'da her şeyi yığmak **yasak**.
3. **UX doğruluğu** — domain kuralları (onay akışı, TTL, PII maskeleme, şube saati sadakati, resource gizleme, idempotity, double-booking engeli, step-up) görsel/etkileşim diline yansır.
4. **Görsel tutarlılık** — tüm roller/yüzeyler tek tasarım sistemini paylaşır (renk, tipografi, spacing, motion, ikon dili).
5. **Erişilebilirlik** — WCAG 2.2 AA, klavye navigasyonu, screen reader, focus-visible.
6. **Türkçe UI** — tüm kullanıcı yüzey metinleri Türkçe; domain terimleri tutarlı.

### 1.2 Yapma (kritik)

- Sahte dashboard metrikleri/KPA rakamları uydurma.
- Gerçek backend akışı olmayan "çalışıyor" izlenimi veren sahte formlar yaratma.
- Tasarım token'ları atlayıp keyfi hard-coded renkler (`bg-indigo-600`, `text-gray-900` vb.) kullanma.
- İngilizce UI metni yazma (Türkçe zorunlu). Backend enum adları (`PendingApproval`) kodda kalır, UI'da Türkçe label'a map'lenir.
- "Tümünü gör" / "Daha fazla" yerine gerçek sayfalama olmadan sınırsız scroll yapma.
- Müşteri yüzeyinde resource GUID/adı gösterme.
- 24 saatlik TTL göstermeden PendingApproval taleplerini liste halinde yığma.
- Çakışma/conflict uyarısı olmadan onay akışı tasarlama.
- Step-up olmadan PlatformAdmin mutation butonlarını aktif gösterme.
- Mojibake'li Türkçe karakter (UTF-8 byte'ları Windows-1252 ile yanlış decode edilmiş halleri: 0xC3 0xA7, 0xC4 0xB1, 0xC5 0x9F, 0xC4 0x9F vb.) dosyalara yazma — her dosya UTF-8 without BOM.

---

## 2. Ürün Context'i, Domain Kuralları, Terimler

**RezSaaS**, tek domain altında çalışan **onaylı rezervasyon + operasyon SaaS**'idir. Müşteriler işletmeleri keşfeder, randevu **talebi** gönderir; işletme onaylar/reddeder. Onaylanmadan kesin randevu oluşmaz.

### 2.1 Çekirdek domain kuralları (UI'ya yansımalı)

- **Onaylı rezervasyon modeli**: Müşteri `AppointmentRequest` oluşturur (`PendingApproval`). İşletme onaylar → `Appointment` (`Confirmed`) veya reddeder (`Declined`). **PendingApproval kesin randevu DEĞİLDİR** — UI her yerde vurgular (amber badge, pulse nokta, uyarı banner'ı, "İşletme onayı bekliyor — kesin randevu değildir" copy).
- **TTL (24 saat üst sınır)**: Gerçek sonlanma zamanı `min(createdAt + 24 saat, appointmentStart - responseBuffer)`. Talep kartlarında **geri sayımsı TTL badge**:
  - ≤5 dk = **kırmızı pulse** ("12 sn kaldı") — kritik
  - 6–15 dk = **amber** ("8 dk kaldı") — uyarı
  - 16 dk–2 sa = **nötr gri** ("1 sa 24 dk kaldı")
  - >2 sa = **çok nötr** ("4 sa 12 dk kaldı")
  - ≤0 dk = **gri, üstü çizili** ("Süresi doldu")
- **Slot bloklama YOK**: `PendingApproval` slot'u bloklamaz. Aynı slota birden fazla talep düşebilir; işletme birini seçer. Diğerleri `Superseded` ("Başka talep seçildi").
- **1 Staff + 1 Resource MVP invariantı**: Her randevu **tam olarak 1 personel + 1 fiziksel kaynak** ile planlanır. Müşteri "personel tercihi" opsiyonel yapar ("Fark etmez"); resource atamasını API gizlice yapar, kullanıcıya gösterilmez.
- **Multi-service**: Tek randevuda birden fazla hizmet varyantı; toplam süre tek blok olarak planlanır (aynı staff + aynı resource).
- **Resource müşteriye GİZLİ**: Müşteri yüzeyinde (keşif, wizard, kendi talepleri) kaynak adı/GUID ASLA görünmez. Sadece işletme panelinde "iç kaynak" etiketiyle.
- **Şube saati sadakati**: Tüm saatler şube zaman diliminde. Müşteri tarayıcı TZ'sine çevrilmez. UTC saklanır, şube TZ ayrı. Tooltip: "Şube zaman dilimi: Europe/Istanbul".
- **PII maskeleme**: İşletme panelinde müşteri e-posta/telefon **maskeli** (`m***@example.com`, `+90 *** ** 12 34`). Raw PII sızdırılmaz. Yanında "PII gizli" chip.
- **Idempotency**: Onay/red/iptal/rebook/complete/no-show tekrar gönderime dayanıklı (`Idempotency-Key`). Retry güvenli — "Bu işlemi güvenle tekrar deneyebilirsin".
- **Tenant izolasyonu**: İşletme verisi tenant-bazlı izole. Başka tenant verisi görünmez (404, varlık sızdırmaz).
- **Step-up (MFA)**: Yüksek riskli platform aksiyonları (`PlatformAdmin`) için ek parola + MFA (authenticator) + recovery code fallback. Aktifse kalıcı "STEP-UP AKTİF" pulse indicator; değilse tüm platform route'ları step-up gate'e düşer.
- **Double-booking engeli**: Aynı staff için çakışan confirmed aralık oluşamaz. Aynı resource için ayrı ayrı engellenir. Onay anında DB + uygulama tekrar kontrol eder. Çakışma → onay fail.
- **Abuse/yaptırım**: Kötüye kullanımda kademeli yaptırım: uyarı → cooldown (≤24 saat) → temporary ban (24–72 saat) → kalıcı kapatma (manuel inceleme + appeal penceresi). Aktif sanction yeni booking request'i engeller; müşterinin mevcut talep görme/iptal hakkı korunur.
- **Complete/NoShow zaman kuralı**: `Complete` yalnız randevu **end**'inden sonra, `NoShow` yalnız **start**'tan sonra. Future slotu erken boşaltan operasyon yasak.
- **Rebook**: Eski randevu `Rebooked`, yeni `Confirmed` üretilir. Çakışma tekrar kontrol.
- **Hesap kapatma (AccountClosureCase)**: High risk + 2 farklı PlatformAdminWithStepUp + ≥7 gün appeal penceresi + açık appeal yok + platform rolü ve aktif tenant membership taşımama. CustomerNoticeDeliveredAtUtc (SMTP kanıtı) olmadan execution bloklanır.

### 2.2 Domain terimleri (UI Türkçe ↔ backend kod)

| Backend (kod) | UI Türkçe | Açıklama |
|---|---|---|
| `AppointmentRequest` | Talep | Müşterinin gönderdiği, onaylanmamış rezervasyon talebi |
| `Appointment` | Randevu | Onaylanmış, kesinleşmiş randevu |
| `PendingApproval` | Onay bekliyor | Talep durumu (amber, pulse) |
| `Confirmed` | Onaylandı | Onaylanmış randevu (yeşil) |
| `Declined` | Reddedildi | Reddedilen talep (kırmızı) |
| `Expired` | Süresi doldu | TTL doldu (gri, üstü çizili) |
| `Superseded` | Başka talep seçildi | Aynı slota başka talep seçildi (gri) |
| `CancelledByCustomer` | Müşteri iptal etti | Müşteri iptali (gri) |
| `CancelledByAppeal` | İtirazla kapandı | Appeal sonucu kapandı (gri) |
| `Completed` | Tamamlandı | Randevu gerçekleşti (yeşil/nötr) |
| `NoShow` | Gelmedi | Müşteri gelmedi (kırmızı/turuncu) |
| `Rebooked` | Yeniden planlandı | Rebook ile dönüştü (gri) |
| `Branch` | Şube | Fiziksel şube |
| `Resource` | Kaynak (iç kaynak) | Fiziksel kapasite — müşteriye gizli |
| `ResourceType` | Kaynak türü | chair/room/bed/station/device |
| `StaffMember` | Personel | Çalışan |
| `Service` | Hizmet | Hizmet kategorisi |
| `ServiceVariant` | Hizmet seçeneği | Varyant (süre + fiyat) |
| `Skill` | Yetkinlik | Personel yetkinliği |
| `Tenant` | İşletme | Multi-tenant izolasyon birimi |
| `Membership` | Üyelik | Tenant rol ataması |
| `Strike` | Uyarı puanı | Abuse sürelim yaptırım |
| `Sanction` | Yaptırım | Aktif bloklayıcı (cooldown/ban) |
| `Appeal` | İtiraz | Müşteri itirazı |
| `AccountClosureCase` | Hesap kapatma | Kalıcı kapatma inceleme |
| `StepUp` | Step-up | MFA doğrulaması |
| `PlatformAdmin` | Platform admin | Global platform yöneticisi |
| `BusinessOwner` | İşletme sahibi | Tenant-wide yetkili |
| `BranchManager` | Şube yöneticisi | Branch-scoped yetkili |
| `Staff` | Personel | Operasyon yetkisi yok (default deny) |
| `Customer` | Müşteri | Global Identity, tenant yok |
| `PlatformSupport` | Platform destek | Read-only platform |

---

## 3. Tasarım Dili — Renk, Tipografi, Spacing, Radius, Elevation

### 3.1 Renk paleti (full scale, her ana renk için 50–950)

> Tüm renkler CSS custom property olarak `--rs-*` prefix'iyle tanımlı. Tailwind arbitrary value (`bg-[var(--rs-accent)]`) ile kullanılır. Hard-coded hex **yasak**.

**Background & surface (dark glassmorphism):**
| Token | Değer | Kullanım |
|---|---|---|
| `--rs-bg` | `#080c14` | Ana arkaplan |
| `--rs-bg-deep` | `#050810` | Gradient alt ton |
| `--rs-surface` | `#0d1120` | Opak kart bg (modal/dialog) |
| `--rs-surface-strong` | `#111626` | Daha açık opak yüzey |
| `--rs-surface-muted` | `rgba(255,255,255,0.06)` | Bölüm bg, input bg |
| `--rs-glass` | `rgba(255,255,255,0.04)` | Glassmorphism kart (backdrop-blur-xl) |
| `--rs-glass-strong` | `rgba(255,255,255,0.08)` | Hover glass |
| `--rs-neutral-soft` | `rgba(255,255,255,0.06)` | Nötr soft bg |

**Border:**
| Token | Değer | Kullanım |
|---|---|---|
| `--rs-border` | `rgba(255,255,255,0.08)` | Normal border |
| `--rs-border-strong` | `rgba(255,255,255,0.16)` | Hover/focus |
| `--rs-warning-border` | `rgba(245,158,11,0.28)` | Warning border |

**Ink (foreground):**
| Token | Değer | Kullanım |
|---|---|---|
| `--rs-ink` | `#f0f2fa` | Birincil yazı |
| `--rs-ink-soft` | `#c7cad6` | İkincil yazı |
| `--rs-muted` | `#94a3b8` | Muted (slate-400) |
| `--rs-muted-strong` | `#cbd5e1` | Muted strong (slate-300) |

**Indigo (primary accent) — full scale:**
| Ton | Hex |
|---|---|
| `--rs-indigo-300` | `#a5b4fc` |
| `--rs-indigo-400` | `#818cf8` |
| `--rs-accent` | `#6366f1` (indigo-500, primary) |
| `--rs-indigo-600` | `#4f46e5` |
| `--rs-indigo-700` | `#4338ca` |
| `--rs-accent-strong` | `#818cf8` (hover/lighter) |
| `--rs-accent-soft` | `rgba(99,102,241,0.16)` (soft bg) |

**Violet (secondary accent) — full scale:**
| Ton | Hex |
|---|---|
| `--rs-violet-300` | `#c4b5fd` |
| `--rs-violet-400` | `#a78bfa` |
| `--rs-accent-violet` | `#8b5cf6` (violet-500, secondary) |
| `--rs-violet-600` | `#7c3aed` |
| `--rs-accent-violet-soft` | `rgba(139,92,246,0.16)` |

**Cyan (tertiary/chart):**
| Token | Hex |
|---|---|
| `--rs-chart-3` | `#22d3ee` |

**Semantic — full scale:**
| Ton | Success (emerald) | Warning (amber) | Danger (red) |
|---|---|---|---|
| 300 | `#6ee7b7` | `#fcd34d` | `#fca5a5` |
| 400 | `#34d399` | `#fbbf24` | `#f87171` |
| 500 | `#10b981` (`--rs-success`) | `#f59e0b` (`--rs-warning`) | `#ef4444` (`--rs-danger`) |
| 600 | `#059669` | `#d97706` | `#dc2626` (`--rs-danger-strong`) |
| soft bg | `rgba(16,185,129,0.16)` (`--rs-success-soft`) | `rgba(245,158,11,0.16)` (`--rs-warning-soft`) | `rgba(239,68,68,0.16)` (`--rs-danger-soft`) |

**Diğer:**
| Token | Değer |
|---|---|
| `--rs-focus` | `#6366f1` |
| Gradient primary | `linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%)` |
| Glow shadow | `0 8px 28px rgba(99,102,241,0.28)` |
| `--rs-chart-1..5` | `#818cf8, #a78bfa, #22d3ee, #fbbf24, #34d399` |

**Renk kullanım senaryoları:**
| Durum | Renk | Component |
|---|---|---|
| Onaylandı/Confirmed | success | StatusBadge yeşil |
| Onay bekliyor/PendingApproval | warning + pulse | StatusBadge amber + pulse dot |
| Reddedildi/Declined | danger | StatusBadge kırmızı |
| Süresi doldu/Expired | neutral | StatusBadge gri |
| Gelmedi/NoShow | danger | StatusBadge kırmızı |
| Tamamlandı/Completed | success/neutral | StatusBadge yeşil/nötr |
| TTL kritik (≤5dk) | danger + pulse | TtlBadge kırmızı pulse |
| TTL uyarı (≤15dk) | warning | TtlBadge amber |
| Aktif/Active | success | yeşil dot |
| Askıda/Suspended | warning | amber |
| Kapalı/Closed | danger | kırmızı |
| Step-up aktif | success + pulse | yeşil pulse dot |
| Risk yüksek/High | danger | kırmızı |
| Risk orta/Medium | warning | amber |
| Risk düşük/Low | success | yeşil |

### 3.2 Tipografi (full type system)

**Font aileleri:**
- `--rs-font-display`: **Plus Jakarta Sans** (400, 500, 600, 700, 800) → başlıklar, logo, marka.
- `--rs-font-sans`: **Inter** (400, 500, 600, italic 400) → gövde.
- `--rs-font-mono`: **JetBrains Mono** (400, 500) → badge, saat, GUID, tablo verisi, eyebrow.

**Type scale (tam spec):**
| Seviye | Font | Boyut | Weight | Letter-spacing | Line-height | Kullanım |
|---|---|---|---|---|---|---|
| Display/Hero | Plus Jakarta | 6xl–8xl (60–96px) | 700 | `-0.04em` | 1.05 | Landing hero |
| H1 sayfa | Plus Jakarta | 4xl–5xl (36–48px) | 600 | `-0.04em` | 1.1 | Sayfa başlığı |
| H2 bölüm | Plus Jakarta | 3xl–4xl (30–36px) | 600 | `-0.035em` | 1.2 | Bölüm başlığı |
| H3 kart | Plus Jakarta | xl–2xl (20–24px) | 600 | `-0.03em` | 1.3 | Kart başlığı |
| H4 alt | Plus Jakarta | lg–xl (18–20px) | 600 | `-0.025em` | 1.4 | Alt başlık |
| Body large | Inter | lg (18px) | 400 | 0 | 1.7 | Açıklama |
| Body | Inter | base (16px) | 400 | 0 | 1.6 | Paragraf |
| Body small | Inter | sm (14px) | 400 | 0 | 1.6 | Detay |
| Label | Inter | sm (14px) | 500 | 0 | 1.4 | Form label |
| Caption | Inter | xs (12px) | 400 | 0 | 1.5 | Yardımcı |
| Eyebrow | JetBrains Mono | 10–11px | 600 | `0.18em–0.24em`, uppercase | 1.4 | Üst etiket |
| Mono badge | JetBrains Mono | 11px | 500 | `0.04em` | 1.4 | Badge |
| Mono saat/GUID | JetBrains Mono | 11–13px | 500 | 0 | 1.4 | Saat, ID |
| Tabular numeric | JetBrains Mono | inherited | 500 | 0 | inherited | Tablo sayı |

**Font yüklemesi:** Google Fonts CDN, `display=swap`. Preconnect öner.

### 3.3 Spacing scale

4 / 8 / 12 / 16 / 20 / 24 / 32 / 40 / 48 / 56 / 64 / 80 / 96 / 128 px (Tailwind `space-*` ile uyumlu).

### 3.4 Radius scale

| Token | Değer | Kullanım |
|---|---|---|
| `--rs-radius-sm` | 0.375rem (6px) | Badge, küçük etiket |
| `--rs-radius` | 0.625rem (10px) | Orta element |
| `--rs-radius-md` | 0.75rem (12px) | Buton, küçük kart |
| `--rs-radius-lg` | 1rem (16px) | Standart kart, input |
| `--rs-radius-xl` | 1.5rem (24px) | Ana bölüm kartı |
| `--rs-radius-2xl` | 2rem (32px) | Hero kart, modal (eski) |
| `rounded-full` | 9999px | Avatar, pill buton (kullanılmıyor), dot |

### 3.5 Elevation / shadow

| Token | Değer | Kullanım |
|---|---|---|
| `--rs-shadow-card` | `0 24px 80px rgba(0,0,0,0.45)` | Ana kartlar, derin |
| `--rs-shadow-soft` | `0 4px 30px rgba(0,0,0,0.35)` | Input, hover, küçük kart |
| `--rs-shadow-button` | `0 1px 2px rgba(0,0,0,0.4), 0 8px 28px rgba(99,102,241,0.28), inset 0 1px 0 rgba(255,255,255,0.18)` | Gradient buton glow |

---

## 4. Motion Kataloğu (Full)

| Pattern | Duration | Easing | Kullanım |
|---|---|---|---|
| Fade-up | 0.7s | `cubic-bezier(0.16, 1, 0.3, 1)` | Sayfa/section load, stagger (+45ms/item) |
| Slide-in | 0.4s | `cubic-bezier(0.16, 1, 0.3, 1)` | Modal, drawer, step geçişi |
| Hover lift | 150ms | ease-out | Kart/buton `-translate-y-0.5`, `brightness-110` |
| Active press | 100ms | ease-in | Buton `scale-[0.98]` |
| Pulse-warning | 1.8s | `cubic-bezier(0.4, 0, 0.6, 1)` infinite | TTL kritik badge, step-up indicator |
| Sidebar collapse | 300ms | ease-in-out | Width 260px ↔ 64px |
| Layout removal | 200ms | ease-out | Talep kartı onay/red sonrası fade-out |
| Orb drift | 22s / 17s / 28s | ease-in-out infinite | Background orb'lar |
| Skeleton pulse | 1.5s | ease-in-out infinite | Loading placeholder |
| Tooltip fade | 150ms | ease | Tooltip açılış/kapanış |
| Dialog overlay fade | 200ms | ease | Overlay bg fade |
| Drawer slide | 300ms | `cubic-bezier(0.16, 1, 0.3, 1)` | Mobil drawer |
| Step bar tween | 400ms | `cubic-bezier(0.16, 1, 0.3, 1)` | Wizard progress bar dolma |
| Star hover | 100ms | ease | Star rating hover |
| Notification dot pulse | 2s | `cubic-bezier(0.4, 0, 0.6, 1)` infinite | Bell kırmızı dot |

**`prefers-reduced-motion: reduce`** tümünü devre dışı bırakır (fade-up, slide-in, pulse, orb, hover lift).

---

## 5. Background, İkon Dili, Görsel Hiyerarşi

### 5.1 AnimatedBackground (tüm sayfalarda root layout'ta)

- `fixed inset-0 -z-10`, pointer-events-none, select-none.
- Tema rengi `#080c14`.
- **3 orb** (radial gradient blur):
  - Indigo orb: 700px, opacity 0.18, blur 140px, top -15% left 5%, 22s `orb1`.
  - Violet orb: 500px, opacity 0.12, blur 110px, bottom 5% right 3%, 17s `orb2`.
  - Cyan orb: 350px, opacity 0.08, blur 90px, top 45% right 28%, 28s `orb3`.
- **Grid overlay**: 64px kare, beyaz çizgi opacity 0.025.

### 5.2 İkon dili (Lucide-react stili, stroke 2, rounded)

**Tam eşleme:**
| Fonksiyon | İkon |
|---|---|
| Dashboard | `LayoutDashboard` |
| Talepler/Inbox | `Inbox` (+ badge) |
| Takvim | `Calendar` / `CalendarDays` |
| Randevular | `CalendarCheck` |
| Randevu detay | `CalendarClock` |
| Personel | `Users` |
| Hizmetler | `Sparkles` / `Scissors` |
| Şubeler | `Store` |
| Kaynaklar | `Armchair` / `Bed` / `Wrench` |
| Kaynak türleri | `Shapes` |
| Yetenekler | `Award` / `BadgeCheck` |
| Çalışma saatleri | `Clock` |
| Ayarlar | `Settings` |
| Kullanıcı yönetimi | `UserCog` |
| Bildirimler | `Bell` |
| Avatar | `User` / `CircleUser` |
| Logout | `LogOut` |
| Onay (approve) | `Check` / `CheckCircle2` |
| Red (decline) | `X` / `XCircle` |
| İptal (cancel) | `Ban` |
| Complete | `CheckCheck` |
| No-show | `UserX` |
| Rebook | `RefreshCw` / `CalendarPlus` |
| Not | `StickyNote` |
| Resource block | `OctagonAlert` |
| Spam/abuse | `Flag` / `ShieldAlert` |
| İtiraz | `Gavel` / `Scale` |
| Hesap kapatma | `Lock` / `XOctagon` |
| Ceza | `Gavel` |
| Denetim | `FileText` / `History` |
| Step-up | `ShieldCheck` / `KeyRound` |
| Tenant | `Building2` |
| Para | `TurkishLira` / `Wallet` |
| Saat | `Clock` / `Timer` |
| TTL kritik | `AlarmClock` (pulse) |
| PII gizli | `EyeOff` / `Lock` |
| Conflict | `AlertTriangle` |
| Bilgi | `Info` |
| Uyarı | `AlertCircle` |
| Hata | `AlertOctagon` |
| Başarı | `CheckCircle2` |
| Star rating | `Star` (dolu amber) |
| Plus/Ekle | `Plus` |
| Edit | `Pencil` / `SquarePen` |
| Delete | `Trash2` |
| Archive | `Archive` |
| Search | `Search` |
| Filter | `Filter` / `SlidersHorizontal` |
| Sort | `ArrowUpDown` |
| More (menu) | `MoreHorizontal` / `MoreVertical` |
| Menu | `Menu` |
| Chevron | `ChevronDown` / `ChevronRight` / `ChevronLeft` |
| Logo mark | `CalendarRange` / `CalendarClock` |
| External link | `ExternalLink` |
| Copy | `Copy` / `Clipboard` |
| Eye (göster) | `Eye` |
| EyeOff (gizle) | `EyeOff` |
| Dark mode | `Moon` |
| Light mode | `Sun` |
| Globe (TZ) | `Globe` |
| Map pin | `MapPin` |
| Phone | `Phone` |
| Mail | `Mail` |
| Building | `Building2` |
| Credit card | `CreditCard` |
| Download | `Download` |
| Upload | `Upload` |
| Image | `Image` / `ImagePlus` |
| Camera | `Camera` |
| Video | `Video` |
| Play | `Play` |
| Pause | `Pause` |
| X (kapat) | `X` |
| Minus | `Minus` |
| Check küçük | `Check` |
| Dot | `.` (CSS ile) |

### 5.3 Görsel hiyerarşi prensipleri

- **F-pattern** (sol-üst ağırlıklı okuma).
- **Tek primary CTA** sayfa başına (gradient buton).
- **Eyebrow etiket** mono uppercase ile bölüm başlığından önce.
- **Boşluk (whitespace)**Bold, negative space önemli.
- **Renk kontrastı** ile hiyerarşi (ink > muted > muted-strong).
- **Motion** ile dikkat (pulse = kritik, fade-up = yeni içerik).

---

## 6. Roller ve Yetki Matrisi

5 birincil rol + 1 read-only. **Sidebar ve görünen sayfalar role göre şekillenir.**

### 6.1 Roller

| Rol | Kapsam | Açıklama |
|---|---|---|
| **Customer** | Global Identity (tenant yok) | Müşteri hesabı. Keşif, rezervasyon talebi, kendi talep/itiraz yönetimi. |
| **Staff** | Branch-scoped | Personel. Kendi randevu/takvim, sınırlı müşteri etkileşim. Operasyon mutation yok (default deny). |
| **BranchManager** | Branch-scoped (tek şube) | Şube yöneticisi. **Kendi şubesinin** taleplerini onaylar, takvim/personel/kaynak yönetir. Diğer şubeleri görmez. |
| **BusinessOwner** | Tenant-wide | İşletme sahibi. **Tüm şubeler** tam operasyon + ayarlar + billing hazırlığı. Şubeler arası geçiş. |
| **PlatformAdmin** | Platform-global | Platform yöneticisi. Tenant lifecycle, abuse/sanction, appeal/closure, audit log. **Step-up zorunlu.** |
| **PlatformSupport** | Platform-global (read-only) | Read-only inceleme, mutation yok. |

---

## 7. Rol → Sidebar Navigasyon Haritası

### 7.1 Customer (sade, hesap odaklı)
Minimal sidebar veya top navbar. "Hesabım" altında:
- **Genel bakış** (`LayoutDashboard`) — yaklaşan randevular + hızlı eylemler
- **Taleplerim** (`Inbox`) — geçmiş + PendingApproval iptal
- **İtirazlar** (`Scale`) — strike/sanction/closure appeal
- **Değerlendirmelerim** (`Star`) — yazdığım değerlendirmeler
- **Profil** (`User`) — read-only; düzenleme "yakında"

### 7.2 Staff (sade operasyonel)
- **Bugünün programı** (`CalendarDays`)
- **Takvimim** (`Calendar`) — haftalık/aylık
- **Randevularım** (`CalendarCheck`) — geçmiş
- **Müşteri notları** (`StickyNote`) — sınırlı PII

### 7.3 BranchManager (branch-scoped tam operasyon)
- **ANA**: Genel bakış · Talepler [badge: N] · Takvim
- **YÖNETİM** (kendi şube): Personel · Hizmetler · Kaynaklar · Çalışma saatleri
- **YAPILANDIRMA**: Şube ayarları (sadece kendi şube)

### 7.4 BusinessOwner (tenant-wide tam kontrol)
- **Üstte**: **Tenant/şube switcher**
- **ANA**: Genel bakış · Talepler [badge: N] · Takvim
- **YÖNETİM**: Şubeler · Personel · Hizmetler · Kaynaklar · Yetenekler · Çalışma saatleri
- **YAPILANDIRMA**: İşletme ayarları · Kullanıcı yönetimi · Billing hazırlığı

### 7.5 PlatformAdmin (control-plane, step-up zorunlu)
- **Üstte**: **"STEP-UP AKTİF" pulse indicator**
- **ANA**: Abuse kontrol · İtirazlar · Tenantlar
- **OPERASYON**: Denetim günlüğü · Kimlikler · Cezalar · Operasyon health (read-only reconciliation)
- **DESTEK**: Support talepleri
- Her sayfada step-up expire süresi header'da.

### 7.6 PlatformSupport (read-only control-plane)
Aynı nav (PlatformAdmin ile), tüm mutation butonları disabled/gizli:
- Abuse inceleme (read-only)
- Tenant detay (read-only)
- İtiraz görüntüleme (review yetkisi yok)

---

## 8. Sidebar Tasarım Spec'i (Pixel-Level)

### 8.1 Desktop (lg+ 1024px+)

- **Sol fixed/sticky** sidebar.
- Genişlik: **260px (geniş)** / **64px (collapsed)**.
- Yükseklik: 100vh, sticky.
- Ana içerik sağda, sol margin = sidebar genişliği.
- Background: `bg-[var(--rs-bg)]/85 backdrop-blur-2xl` (scrolled'da border-b).
- Border right: `border-r border-[var(--rs-border)]`.

### 8.2 Sidebar iç yapısı (yukarıdan aşağıya, sırayla)

#### 8.2.1 Logo bölümü (px-4 py-5, flex items-center gap-2)
- Logo kutusu: `h-8 w-8 rounded-xl rs-gradient-bg flex items-center justify-center shadow-lg shadow-[rgba(99,102,241,0.3)]`.
- İçerik: `CalendarRange` ikonu (beyaz, h-4 w-4) veya "R" harfi (bold, white).
- Yanında (collapse=false): "RezSaaS" text (Plus Jakarta Sans, bold, text-lg, tracking-tight, `--rs-ink`).
- Collapse'ta: sadece logo kutu.

#### 8.2.2 Collapse toggle (logo bölümünde sağda)
- Küçük ok buton: `‹` (collapse) / `›` (expand).
- `aria-label="Kenar çubuğunu daralt"` / `"genişlet"`.
- `rounded-full p-1 text-[var(--rs-muted)] hover:bg-[var(--rs-surface-muted)]`.

#### 8.2.3 Tenant/Şube switcher (sadece BusinessOwner/BranchManager, mx-2)
- Glass dropdown: `rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] px-3 py-2`.
- Aktif tenant display name + aktif şube adı (truncate).
- `ChevronDown` ikonu sağda.
- Açılır liste: tenant üyelikleri + şubeler (backend'den). Her item: tenant adı + şube adı + rol badge.
- **Kullanıcı serbest GUID giremez**; listeden seçer.
- Collapse'ta: tenant ilk harfi küçük kutu.

#### 8.2.4 Step-up indicator (sadece PlatformAdmin, mx-3 mb-2)
- Aktif: `rounded-2xl border border-[rgba(16,185,129,0.3)] bg-[var(--rs-success-soft)] px-3 py-2 flex items-center gap-2`.
  - `pulse-warning h-2 w-2 rounded-full bg-[var(--rs-success)]`.
  - Mono uppercase text: "STEP-UP AKTİF" (`text-[var(--rs-success)]`).
  - Tooltip: expire süresi (tr-TR format).
- Pasif: amber varyant — "STEP-UP GEREKLİ".

#### 8.2.5 Nav grupları (flex-1, overflow-y-auto, mt-2, px-2, pb-4, space-y-4)
Her grup:
- Mono uppercase etiket (`px-3 py-1 font-mono text-[10px] font-semibold uppercase tracking-[0.18em] text-[var(--rs-muted)]`): "ANA", "YÖNETİM", "YAPILANDIRMA", "OPERASYON", "DESTEK".
- Altında nav item'lar (space-y-1).

Her nav item:
- `Link` component, `flex items-center gap-2 rounded-full px-3 py-2 text-sm font-medium transition`.
- Sol: ikon (lucide, h-4 w-4).
- Orta: label (truncate).
- Sağ (opsiyonel): badge (`ml-auto inline-flex min-w-5 items-center justify-center rounded-full bg-[var(--rs-accent-soft)] px-1.5 text-[0.65rem] font-semibold text-[var(--rs-accent-strong)]`).
- **Aktif** (`aria-current="page"`): `bg-gradient-to-br from-[var(--rs-accent)] to-[var(--rs-accent-violet)] text-white shadow-lg shadow-[rgba(99,102,241,0.3)]`. İkon beyaz.
- **Pasif hover**: `text-[var(--rs-muted)] hover:bg-[var(--rs-surface-muted)] hover:text-[var(--rs-ink)]`.
- **Disabled** (yetki yok): `opacity-40 cursor-not-allowed pointer-events-none`.
- **Collapse'ta**: `justify-center`, sadece ikon, tooltip'te tam label (`title` attribute veya custom tooltip).

#### 8.2.6 User row (alt, border-top, px-3 py-4)
- Avatar (`h-8 w-8`, gradient initials).
- Display name (`truncate text-xs font-medium text-[var(--rs-ink)]`).
- Email (`truncate text-[0.65rem] text-[var(--rs-muted)]`).
- Logout butonu (ghost, `LogOut` ikonu, h-4 w-4).
- Collapse'ta: sadece avatar.

### 8.3 Mobile / tablet (md ve altı, <1024px)

- Sidebar gizli.
- **Hamburger** butonu (top header'da sol): `inline-flex h-9 w-9 items-center justify-center rounded-lg border border-[var(--rs-border)] bg-[var(--rs-glass)] text-[var(--rs-ink)] backdrop-blur-xl`.
- Tıklayınca **drawer** açılır:
  - Overlay: `fixed inset-0 z-50 bg-black/70 backdrop-blur-md` (overlay'e tıkla kapat).
  - Drawer: `fixed left-0 top-0 z-50 h-full w-[280px] slide-in` (sidebar içeriği ile aynı).
  - Kapanma: overlay tık, Esc, nav item tık.
- Drawer `role="dialog"`, `aria-modal="true"`, focus trap.
- Customer için mobilde **alt tab bar** opsiyonu (Keşfet / Taleplerim / Profil).

### 8.4 Top header (sticky, ana içerikte)

- Sticky top, `bg-[var(--rs-bg)]/85 backdrop-blur-xl border-b border-[var(--rs-border)]`.
- Sol: hamburger (mobil) + breadcrumb ("İşletme paneli / Talepler" formatı, mono küçük).
- Sağ: search (⌘K), bildirimler (`Bell` + dot), "Panele dön" / "Çıkış", step-up badge (PlatformAdmin için header'da da).

### 8.5 Erişilebilirlik (a11y)

- Sidebar `<nav aria-label="Ana navigasyon">`.
- Nav item `aria-current="page"` (aktif).
- Badge `aria-label="N bekleyen talep"`.
- Collapse toggle `aria-expanded`, `aria-controls`.
- Drawer `role="dialog"`, `aria-modal="true"`, focus trap, Esc.
- Klavye: Tab nav item'lar arası, Enter aktivasyon, Esc drawer kapat.
- Focus-visible ring (`focus-visible:ring-2 ring-[var(--rs-focus)] ring-offset-2`).
- `prefers-reduced-motion` saygılı.
- Renk kontrast WCAG 2.2 AA (ink `#f0f2fa` bg `#080c14` > 15:1).

---

## 9. Sayfa Envanteri (Rol Bazlı, Her Sayfa Detailed Layout)

> **Prensip**: Her ana fonksiyon ayrı odaklanmış sayfada. Dashboard'da yığma yok.

### 9.1 Public yüzey (auth yok)

#### `/` Landing
- **Top**: `PublicNavbar` (fixed, scroll-blur).
- **Hero** (pt-28 pb-16): 
  - Eyebrow pill ("Salon, spa, klinik ve stüdyo ekipleri için").
  - H1 hero (gradient text "Rezervasyonu onaya, operasyonu netliğe bağla", Plus Jakarta, 5xl-7xl).
  - Açıklama (lg, muted-strong).
  - 2 CTA buton (primary "Ücretsiz başla", secondary "İşletmeleri keşfet").
- **Kategori grid** (6 kategori, 2-3-6 kolon responsive): her kart gradient logo kutu + isim + hint.
- **Ürün pillarları** (3 kart, glass): başlık + açıklama.
- **İşleyiş** (4 adım, sol başlık + sağ step listesi): mono numara + başlık.
- **Fiyatlandırma** (3 paket, orta gradient highlight): eyebrow + isim + fiyat + feature list + CTA.
- **CTA kartı** (gradient bg): başlık + açıklama + 2 CTA.
- **Footer**: logo + copyright + linkler.

#### `/kesfet` Keşif
- Top header (PublicNavbar).
- Search input + filterlar (şehir, ilçe, kategori, sıralama, "şimdi açık" toggle).
- Kategori pill'leri (yatay scroll): Tümü + 6 kategori.
- Sonuç sayısı + kart grid (1-2-3 kolon).
- Her kart: görsel, açık/kapalı badge, kategori etiketi, isim, konum, yıldız puanı, yorum sayısı, fiyat aralığı, "Rezervasyon yap" CTA.
- Empty state: "Sonuç bulunamadı".

#### `/isletme/[slug]` İşletme profili
- SSR profil kartı: galeri (lazy load), hizmet menüsü (varyant süre/fiyat), şube + çalışma saatleri (Türkçe gün), personel, puan özeti.
- **Gömülü rezervasyon paneli** (`#rezervasyon` anchor).

#### Auth (`/giris`, `/kayit`, `/sifremi-unuttum`, `/sifre-sifirla`)
- İki kolonlu: sol hero (gradient + marka), sağ form kartı (glass).
- Login: e-posta + parola + opsiyonel MFA + recovery code.
- Register: e-posta + parola (min 12 karakter).
- Forgot/reset: e-posta veya `?code=` `?email=` query ile yeni parola.

### 9.2 Rezervasyon wizardu (Customer, 5 adım)

`/isletme/[slug]#rezervasyon` içinde, **Progress indicator** üstte (gradient step bar):

1. **Adım 1 — Hizmet**: multi-service varyant kart grid (her kart: hizmet adı + varyant + süre + fiyat + açıklama, seçili = accent ring + Check). `canProceed`: ≥1 seçili.
2. **Adım 2 — Personel**: "Fark etmez" kartı + personel kartları (avatar + ad + unvan + rating; "Müsait değil" badge disabled). Yetkinlik filtresi.
3. **Adım 3 — Tarih & Saat**: 7 günlük tarih pill (yatay scroll, aktif gradient) + slot grid (4 kolon, müsait olmayan line-through). Saatler şube TZ'inde, tooltip "Şube zaman dilimi: Europe/Istanbul".
4. **Adım 4 — Onay özeti**: snapshot tablo + iptal politikası kartı (`Shield` + "PendingApproval kesin randevu değildir") + PII hatırlatma.
5. **Adım 5 — Tamamlandı**: animasyonlu yeşil CheckCircle + talep ID (`#RZ-2025-4892`) + özet + "Taleplerime git" / "Anasayfa".

**Edge case'ler**: 401→login returnTo, 409/422→slot temizle + "tekrar ara", 429→rate limit, draft recovery (sessionStorage kısa TTL).

### 9.3 Customer panel (`/hesabim/*`)

| Route | Sayfa | Layout |
|---|---|---|
| `/hesabim` | Genel bakış | "Merhaba [ad] 👋" + yaklaşan randevu kartları + hızlı eylem grid |
| `/hesabim/talepler` | Taleplerim | Filtre tab'ları (Tümü / Onay bekliyor / Onaylandı / Kapandı) + kart listesi. Her kart: StatusBadge, işletme, şube, şube saati, personel, süre, toplam. PendingApproval iptal butonu |
| `/hesabim/itirazlar` | İtirazlar | Strike/sanction/closure listesi + "İtiraz aç" dialog (1000 kr not limiti + counter). Internal reason **gösterilmez** |
| `/hesabim/profil` | Profil | Read-only e-posta + display name + "yakında" EmptyState |
| `/hesabim/degerlendirmeler` | Değerlendirmelerim | Yazdığı değerlendirmeler + yazma akışı (yıldaz + yorum) |

### 9.4 Business panel (`/panel/*`)

| Route | Sayfa | Layout |
|---|---|---|
| `/panel` | Genel bakış | Tenant context kartı + **inbox** (TTL badge, PII chip, conflict uyarı, onay/red/abuse) + **schedule** (cancel/complete/no-show/note/rebook/resource-block) |
| `/panel/talepler` | Talepler | Tam ekran inbox. Acil filtre (TTL ≤15 dk kırmızı pulse chip), search, durum filtre tab'ları |
| `/panel/takvim` | Takvim | **Day/Week grid scheduler** (08–18 saat kolonu, absolute-height event'ler, durum renk tonları). Event'ten aksiyon menü. Resource gösterilmez |
| `/panel/randevular` | Randevular | Confirmed list (bugün/hafta/tümü filtre), her satır masked müşteri + hizmet + personel + saat + durum + aksiyon menü |
| `/panel/randevular/[id]` | Randevu detayı | Tüm bilgiler + timeline + notlar + aksiyon geçmişi |
| `/panel/subeler` | Şubeler | Branch CRUD (BusinessOwner tenant-wide, BranchManager kendi şube). Kart listesi: ad, slug, adres, TZ, personel sayısı, durum. Çalışma saatleri + slot config |
| `/panel/personel` | Personel | Staff CRUD + yetkinlik atama + müsaitlik. Kart: avatar, ad, unvan, şube, yetkinlik badge'leri |
| `/panel/hizmetler` | Hizmetler | Service + variant CRUD, fiyat/süre, gerekli yetkinlik. Ağaç görünüm |
| `/panel/kaynaklar` | Kaynaklar | Resource CRUD + out-of-service/block. Kart: ad, tür, şube, durum |
| `/panel/kaynak-turleri` | Kaynak türleri | ResourceType tanımlama. İkonlu grid |
| `/panel/yetenekler` | Yetenekler | Skill tanımlama + personele atama |
| `/panel/calisma-saatleri` | Çalışma saatleri | Haftalık grid + slot config |
| `/panel/ayarlar` | Ayarlar | Tenant profil + public profil metadata + capability map |
| `/panel/kullanicilar` | Kullanıcı yönetimi | Tenant membership (BusinessOwner; add/suspend/revoke) |
| `/panel/abuse-raporlari` | Abuse raporlarım | İşletmenin gönderdiği raporlar |

**Not**: F6.2 CRUD mutation backend Phase 5a bekler. UI'da **"yakında" placeholder, mock yok**. Read-only list (GET varsa) gösterilir.

### 9.5 Platform control-plane (`/platform/*`)

| Route | Sayfa | Layout |
|---|---|---|
| `/platform` | Yönlendirme | `/platform/abuse`'e redirect |
| `/platform/adim` | Step-up gate | Parola + MFA + recovery. Doğrulayana kadar tüm platform route'ları buraya |
| `/platform/abuse` | Abuse kontrol | Event/report/appeal/closure listeleri + 4 kritik metric. Mutation step-up altında |
| `/platform/abuse/kullanici/[id]` | Kullanıcı abuse detayı | Tek kullanıcı strike/sanction/closure geçmişi + appeal review + sanction apply/revoke |
| `/platform/tenantlar` | Tenantlar | Liste (search + filter), detay + lifecycle (suspend/reactivate/close) — reason + exact confirmation + audit |
| `/platform/tenantlar/yeni` | Yeni tenant | Provisioning form (backend bekler — "yakında") |
| `/platform/tenantlar/[id]/uyeler` | Tenant üyelik | Membership list + add/suspend/revoke (backend bekler) |
| `/platform/itirazlar` | İtirazlar | Appeal/closure desk. Müşteri beyanı vs InternalReason ayrımı |
| `/platform/denetim-gunlugu` | Denetim | Audit log, severity filtre, dışa aktar |
| `/platform/kimlikler` | Kimlikler | Identity arama + detay |
| `/platform/cezalar` | Cezalar | Aktif sanctions + revoke |
| `/platform/destek` | Destek | Support talepleri |
| `/platform/operasyon` | Operasyon health | Read-only reconciliation (PII/minimum, sadece sayılar + GUID) |

---

## 10. UX Kural Seti (20 Kural)

1. **PendingApproval asla kesin randevu gibi gösterilmez**: amber badge, pulse nokta, "İşletme onayı bekliyor — kesin randevu değildir".
2. **TTL görsel**: talep kartlarında geri sayımsı badge (5 dk altı kırmızı pulse, 15 dk altı amber, üzeri nötr).
3. **PII maskeleme**: müşteri e-posta/telefon maskeli + "PII gizli" chip.
4. **Şube saati**: tüm saatler şube TZ'inde, tooltip "Şube zaman dilimi: Europe/Istanbul".
5. **Resource gizli**: müşteri yüzeyinde resource adı/GUID yok.
6. **Idempotity**: retry güvenli, "Bu işlemi güvenle tekrar deneyebilirsin".
7. **Tenant switcher**: kullanıcı serbest GUID seçemez, backend membership listesinden.
8. **Step-up**: kalıcı "STEP-UP AKTİF" indicator, expire uyarısı, mutation butonları step-up'suz disabled.
9. **Abuse transparency**: müşteri internal reason/admin actor görmez, güvenli `CustomerNotice`.
10. **Onay dialog'ları**: kalıcı aksiyonlarda exact text confirmation (örn "SUSPEND" yaz) + reason zorunlu + audit.
11. **Conflict uyarısı**: talep onayında aynı slot çakışması → uyarı + "Yine de onayla".
12. **Double-booking engeli**: aynı staff + aynı resource ayrı invariantlar, onay anında DB kontrol.
13. **Complete/NoShow zaman kuralı**: Complete end'den sonra, NoShow start'tan sonra.
14. **Rebook**: eski Rebooked, yeni Confirmed, çakışma recheck.
15. **Customer hakkı**: aktif sanction olsa bile mevcut talep görme/iptal hakkı korunur.
16. **Tenant suspended/closed**: public discovery, yeni booking, işletme operasyon kapalı. Müşteri mevcut talep hakkı korunur.
17. **Tek primary CTA**: sayfa başına tek gradient buton, diğerleri secondary/ghost.
18. **Sahte metrik yok**: KPA'lar gerçek backend'den, mock rakam yok.
19. **Tutarlı Türkçe terim**: aynı kavram farklı sayfalarda farklı kelimeyle değil (Şube her yerde Şube).
20. **WCAG 2.2 AA**: kontrast, klavye, screen reader, focus-visible.

---

## 11. Edge Case Matrix (Her Sayfa × Her State)

Her sayfa **3 temel state** + spesifik edge case'ler ile gel:

### 11.1 Loading
- Skeleton kartlar (pulse animasyonu, `bg-white/[0.06]`).
- Buton içinde spinner.
- Tablo satır skeleton, takvim hücre skeleton.

### 11.2 Empty
- İkon (büyük, `EyeOff`/`Inbox`/`Search`) + başlık + açıklama + (varsa) CTA.
- "Bu filtrede talep yok", "Henüz randevunuz yok", "İşletme kaydı yok".
- **Asla sahte veri**.

### 11.3 Error
- **400/422 validation**: inline form hatrası (field altında kırmızı text).
- **401**: login redirect.
- **403 yetkisiz**: "Bu sayfayı görme yetkiniz yok" + çıkış CTA.
- **404 bulunamadı**: tenant dışı 404 sızdırmaz, "Sayfa bulunamadı".
- **409 conflict**: "Bu saat artık uygun değil, tekrar ara".
- **429 rate limit**: "Çok fazla deneme, kısa süre bekle" + geri sayım.
- **500 genel**: hata kartı + retry buton.

### 11.4 Spesifik edge case'ler (sayfa bazında)

| Sayfa | Edge case | Davranış |
|---|---|---|
| Talepler inbox | Slot expired (TTL doldu) | Kart otomatik "Süresi doldu" gri + üstü çizili |
| Talepler inbox | Double-booking denemesi | Onay butonu disabled, "Bu personel bu saatte dolu" |
| Talepler inbox | Tenant suspended | Banner "Bu işletme askıda", operasyon butonları disabled |
| Platform route | Step-up expired mid-session | Tüm route'lar step-up gate'e, "Step-up süreniz doldu" |
| Customer profil | PII edge | Customer kendi verisi maskesiz, işletme müşteri verisi maskeli |
| Takvim | Timezone/DST edge | Gece geçen talep doğru saat, "Şube saati: 02:30 (gece)" |
| Tenant switcher | Multi-membership | Aktif tenant vurgulu, "X işletmesine geç" |
| Booking wizard | Slot artık uygun değil (409) | Slot temizle, "tekrar ara" |
| Booking wizard | Auth gerekli (401) | Login redirect + returnTo |
| Hesap kapatma | Aktif membership var | "Önce üyelikleri revoke et" |
| Hesap kapatma | Identity OK + Admin fail | Retry execute, aynı e-posta yeniden gönderilmez |

---

## 12. Component Library (Tokens → Primitives → Patterns → Templates → Pages)

### 12.1 Tokens
CSS custom property'leri (`--rs-*`): renk (§3.1), font (§3.2), radius (§3.4), shadow (§3.5).

### 12.2 Primitives (UI atomları)
- `Button` (6 varyant × 3 boyut)
- `Card` / `GlassCard` (interactive opsiyonel)
- `Badge` (mono, 8 varyant)
- `Avatar` (gradient initials, 4 boyut)
- `StarRating` (display)
- `StarRatingInput` (interactive)
- `Tabs` (segment kontrolü)
- `Tooltip`
- `Progress` (wizard step)
- `Separator` (label opsiyonel)
- `Skeleton` (kart/text/buton)
- `EmptyState` (ikon + başlık + açıklama + CTA)
- `StatusBadge` (31 durum Türkçe label + renk)
- `CalendarGrid` (Day/Week scheduler)
- `AnimatedBackground`
- `FormField` / `TextInput` / `TextArea` / `Select` / `Checkbox` / `RadioGroup` / `Switch` / `Slider`
- `Dialog` / `DialogFormPanel` / `DialogOverlay` / `ConfirmDialog`
- `Toast` / `Banner` / `InlineAlert`

### 12.3 Patterns (kompozit)
- `TenantSwitcher` (dropdown, multi-membership)
- `StepUpIndicator` (pulse badge)
- `InboxToolbar` (filter tabs + search + urgent chip)
- `AppointmentRequestCard` (TTL badge + PII chip + conflict uyarı + aksiyon butonları)
- `AppointmentSchedule` (confirmed randevu listesi + aksiyon menü)
- `ConflictDialog` (çakışma uyarısı + "yine de onayla")
- `AbuseReportDialog` (reason code + PII-uyarılı not)
- `MfaOtpInput` (6 kutulu, numeric only)
- `TtlBadge` (renkli geri sayım)
- `Breadcrumb`
- `NotificationBell` (dot indicator)
- `KpiCard` (gerçek metrik + trend)
- `DataTable` (sorted kolon + cursor pagination)
- `Timeline` (dikey, saat bazlı)
- `EmptyHint` (boş saat dashed border)

### 12.4 Templates (sayfa iskeletleri)
- `PublicLayout` (PublicNavbar + content + footer)
- `CustomerShell` (sidebar + header + content)
- `BusinessPanelShell` (sidebar + tenant switcher + breadcrumb + content)
- `PlatformShell` (sidebar + step-up indicator + breadcrumb + content)
- `AuthShell` (iki kolonlu hero + form)
- `StepUpGate` (step-up form full-screen)

### 12.5 Pages
Bkz. §9 sayfa envanteri.

---

## 13. Component API + State Matrix

### 13.1 Button

```tsx
type ButtonVariant = "primary" | "secondary" | "outline" | "ghost" | "danger" | "success";
type ButtonSize = "sm" | "md" | "lg";

<Button
  variant="primary"      // gradient indigo→violet, glow
  size="md"              // sm: px-3 py-1.5 text-xs / md: px-4 py-2 text-sm / lg: px-5 py-2.5 text-sm
  asChild={false}        // Slot wrapper (Link için)
  disabled={false}
  loading={false}        // spinner göster
  onClick
/>
```

| State | Görsel |
|---|---|
| default (primary) | `rs-gradient-bg text-white shadow-[rgba(99,102,241,0.28)]` |
| hover | `brightness-110 -translate-y-0.5` |
| active | `scale-[0.98]` |
| focus-visible | `ring-2 ring-[var(--rs-accent)] ring-offset-2` |
| disabled | `opacity-40 cursor-not-allowed pointer-events-none` |
| loading | spinner + text opacity 70 |

### 13.2 Card / GlassCard

```tsx
<Card interactive={false}>...</Card>  // varsayılan glass
```

| State | Görsel |
|---|---|
| default | `rounded-[1.25rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] backdrop-blur-xl shadow-[var(--rs-shadow-card)]` |
| interactive hover | `bg-[var(--rs-glass-strong)] cursor-pointer -translate-y-0.5` |

### 13.3 Badge

```tsx
type BadgeVariant = "default" | "success" | "warning" | "danger" | "info" | "purple" | "orange" | "accent";
<Badge variant="info" />  // mono font, rounded-md
```

### 13.4 StatusBadge (31 durum)

```tsx
<StatusBadge status="PendingApproval" />
```

| Status | Renk | Label |
|---|---|---|
| PendingApproval | warning + pulse | "Onay bekliyor" |
| Confirmed | success | "Onaylandı" |
| Declined | danger | "Reddedildi" |
| Expired | neutral | "Süresi doldu" (üstü çizili) |
| Superseded | neutral | "Başka talep seçildi" |
| CancelledByCustomer | neutral | "Müşteri iptal etti" |
| Completed | success | "Tamamlandı" |
| NoShow | danger | "Gelmedi" |
| Rebooked | neutral | "Yeniden planlandı" |
| Active | success | "Aktif" |
| Suspended | warning | "Askıda" |
| Closed | danger | "Kapalı" |
| High | danger | "Yüksek" |
| Medium | warning | "Orta" |
| Low | success | "Düşük" |
| BusinessOwner | success | "İşletme sahibi" |
| BranchManager | neutral | "Şube yöneticisi" |
| Staff | neutral | "Personel" |
| Healthy | success | "Sağlıklı" |
| Degraded | warning | "Bozulmuş" |
| Critical | danger | "Kritik" |
| PendingReview | warning | "İncelemede" |
| Executing | warning | "İşleniyor" |
| Executed | neutral | "Tamamlandı" |
| Revoked | neutral | "Geri alınmış" |
| Rejected | danger | "Reddedildi" |
| Accepted | success | "Kabul edildi" |
| Approved | success | "Onaylandı" |
| Cancelled | neutral | "İptal edildi" |
| CancelledByAppeal | neutral | "İtirazla kapandı" |
| Critical (risk) | danger | "Kritik" |

### 13.5 Avatar

```tsx
<Avatar name="Ahmet Yılmaz" src="..." size="md" />
// size: xs(6) / sm(7) / md(9) / lg(12)
// src yoksa: rs-gradient-bg + initials "AY"
```

### 13.6 Tabs

```tsx
<Tabs
  items={[{ value: "all", label: "Tümü", badge: 5, disabled: false }]}
  value="all"
  onChange={(v) => ...}
  size="sm" | "md"
/>
```

| State | Görsel |
|---|---|
| selected | `bg-[var(--rs-glass-strong)] text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] backdrop-blur-xl` |
| unselected | `text-[var(--rs-muted)] hover:text-[var(--rs-ink)]` |
| disabled | `opacity-50 pointer-events-none` |
| badge (selected) | `bg-[var(--rs-accent)] text-white` |
| badge (unselected) | `bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]` |

### 13.7 Progress (wizard)

```tsx
<Progress
  steps={[
    { label: "Seçim", state: "complete" | "current" | "upcoming" }
  ]}
/>
```

| State | Görsel |
|---|---|
| complete | numara + check, `bg-[var(--rs-accent)] text-white` |
| current | `border-2 border-[var(--rs-accent)] bg-[var(--rs-accent-soft)] text-[var(--rs-accent-strong)]`, `aria-current="step"` |
| upcoming | `border border-[var(--rs-border)] bg-[var(--rs-glass)] text-[var(--rs-muted)] backdrop-blur-xl` |

### 13.8 Dialog

```tsx
<DialogOverlay onClose={...} onEscapeKeyDown={...}>
  <DialogPanel titleId descriptionId>...</DialogPanel>
  <DialogFormPanel title submitLabel loading onClose onSubmit>...</DialogFormPanel>
  <ConfirmDialog title exactText="SUSPEND" reason onSubmit />
</DialogOverlay>
```

- Overlay: `fixed inset-0 z-40 bg-black/70 backdrop-blur-md grid place-items-center`.
- Panel: `fade-up rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-6 shadow-[var(--rs-shadow-card)] backdrop-blur-xl max-w-2xl`.
- ARIA: `role="dialog"`, `aria-modal="true"`, `aria-labelledby`, `aria-describedby`.
- Escape kapat, overlay click kapat, focus trap.

### 13.9 FormField

```tsx
<FormField label="E-posta" hint="..." error="Geçersiz e-posta" required>
  <TextInput type="email" ... />
</FormField>
```

- Label: `text-sm font-medium text-[var(--rs-ink)]`, required `*` accent.
- Hint: `text-xs text-[var(--rs-muted)]`.
- Error: `text-xs text-[var(--rs-danger)]`.

### 13.10 TextInput

```tsx
<TextInput type="email" placeholder error disabled />
```

| State | Görsel |
|---|---|
| default | `min-h-12 rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-glass)] backdrop-blur-xl px-4 text-sm text-[var(--rs-ink)]` |
| focus | `border-[var(--rs-accent)] ring-4 ring-[rgba(99,102,241,0.18)]` |
| error | `border-[var(--rs-danger)] ring-4 ring-[rgba(239,68,68,0.18)]` |
| disabled | `opacity-60 cursor-not-allowed` |

### 13.11 CalendarGrid

```tsx
<CalendarGrid
  events={[{ id, startUtc, endUtc, title, subtitle, tone: "accent|success|warning|danger|neutral", meta }]}
  view="day" | "week"
  branchTimeZoneId="Europe/Istanbul"
  selectedDateUtc
  workingHourStart={8}
  workingHourEnd={18}
  rowHeightPx={56}
  onEventClick={(event) => ...}
/>
```

- Layout: CSS grid, sol kolon saat (4.5rem), sağ kolon gün(ler).
- Event: absolute positioned, `top = (startH - workingHourStart) × rowHeightPx`, `height = duration × rowHeightPx`.
- Boş saat: dashed border + tıkla "randevu ekle".

### 13.12 EmptyState

```tsx
<EmptyState icon={<Inbox />} title="..." description="..." action={<Button>...</Button>} />
```

### 13.13 Skeleton

```tsx
<CardSkeleton /> <TextSkeleton /> <ButtonSkeleton />
```

### 13.14 TenantSwitcher

```tsx
<TenantSwitcher
  tenants={[{ tenantId, label, branchLabel }]}
  currentTenantId
  onTenantChange={(id) => ...}
/>
```

### 13.15 StepUpIndicator

```tsx
<StepUpIndicator expiresAtUtc="..." />  // pulse badge, tooltip expire
```

### 13.16 TtlBadge

```tsx
<TtlBadge status={{ level: "critical|warning|normal|expired", label: "12 sn kaldı", secondsLeft }} />
```

### 13.17 Toast

```tsx
<Toast variant="success|error|info|warning" message actionLabel onAction />
```

- Pozisyon: `fixed bottom-5 left-1/2 -translate-x-1/2 max-w-xl`.
- Glass pill, 3.2 sn auto-dismiss.

---

## 14. Form Field Library (Field-by-Field Spec)

| Field tipi | Component | Validation | Örnek |
|---|---|---|---|
| E-posta | `TextInput type="email"` | RFC, confirmed (production) | `m@example.com` |
| Parola | `TextInput type="password"` | min 12 karakter, unique 4 chars | `RezSaaS!Local2026` |
| Telefon | `TextInput type="tel"` | E.164 opsiyonel | `+90 555 123 4567` |
| Ad soyad | `TextInput` | min 2 | `Ahmet Yılmaz` |
| Tarih | `<input type="date">` | gelecek değil | `2025-12-01` |
| Saat | custom (slot grid) | şube TZ | `14:30` |
| Datetime-local | `<input type="datetime-local">` | şube TZ parse | `2025-12-01T14:30` |
| Select | `<select>` | required | şube listesi |
| Textarea | `<textarea>` | max length + counter | abuse notu (300 kr) |
| Checkbox | `<input type="checkbox">` | — | çoklu hizmet seçimi |
| Radio group | custom | required | MFA türü |
| Switch | custom | boolean | bildirim aç/kapa |
| Slider | custom | range | fiyat aralığı |
| OTP/MFA | custom 6-kutu | numeric 6 hane | step-up kodu |
| File upload | custom | tür/boyut/isim | profil görseli |
| Rich filter | compound | — | keşif filtreleri |

---

## 15. Dialog / Modal / Notification Taxonomy

### 15.1 Dialog tipleri

| Tip | Kullanım | Örnek |
|---|---|---|
| Form dialog | veri girişi | "Yeni şube", "Hizmet ekle" |
| Confirm dialog | kalıcı aksiyon onayı | "Talebi onayla", "Tenant suspend" |
| Confirm exact | exact text confirmation | "SUSPEND" yaz, "CLOSE" yaz |
| Conflict dialog | çakışma uyarısı | "Aynı personel bu saatte dolu, yine de onayla?" |
| Abuse report dialog | reason code + PII-uyarı | "Spam bildir" |
| Step-up dialog | MFA doğrulama | parola + kod |
| Detail drawer | okuma detayı | randevu detayı |

### 15.2 Notification tipleri

| Tip | Pozisyon | Süre | Kullanım |
|---|---|---|---|
| Toast | bottom-center pill | 3.2 sn auto | "Kaydedildi", "Hata" |
| Banner | sayfa üstü full-width | kalıcı (dismiss ile) | "Bu işletme askıda" |
| Inline alert | form/section içinde | kalıcı | "Bu saat artık uygun değil" |
| Notification dot | bell ikonunda | kalıcı | okunmamış bildirim |
| Pulse indicator | sidebar/header | kalıcı (animasyon) | step-up aktif, TTL kritik |

---

## 16. Etkileşim Akış Diyagramları (Text-Based)

### 16.1 Talep onay akışı

```
Talep inbox'ta (PendingApproval, TTL badge)
  ↓ [Onayla]
Conflict kontrol
  ├─ Çakışma → ConflictDialog → [Vazgeç] / [Yine de onayla]
  └─ Yok → proceed
  ↓
Idempotency-Key oluştur
  ↓
POST /api/business/appointment-requests/{id}/approve (Idempotency-Key)
  ├─ 200 → talep Confirmed, çakışanlar Superseded, fade-out + toast
  ├─ 409 → "Bu personel/kaynak bu saatte dolu"
  ├─ 422 → "Talep artık onaylanamaz" (expired/superseded)
  └─ 429 → rate limit
```

### 16.2 Step-up gate

```
PlatformAdmin login → /platform
  ↓
stepUp.isSatisfied?
  ├─ true → /platform/abuse, sidebar'da "STEP-UP AKTİF" pulse
  └─ false → /platform/adim
              ↓ [Parola] + [MFA] + [Recovery fallback]
              POST /api/session/step-up
              ├─ 200 → router.refresh()
              ├─ 401 → "Parola/MFA yanlış"
              ├─ 422 → "MFA gerekli"
              └─ 429 → "Çok deneme"
```

### 16.3 Rezervasyon wizardu

```
/isletme/[slug]#rezervasyon
  Step 1: Hizmet (≥1 varyant) → Step 2
  Step 2: Personel ("Fark etmez" veya belirli) → Step 3
  Step 3: Tarih + saat (GET /slots → grid) → Step 4
  Step 4: Onay özeti (snapshot) → POST /appointment-requests (Idempotency-Key)
    ├─ 200 → Step 5: Tamamlandı
    ├─ 401 → login returnTo
    ├─ 409/422 → slot temizle, "tekrar ara"
    └─ 429 → rate limit
```

### 16.4 Hesap kapatma (AccountClosureCase)

```
Kullanıcı abuse → risk High + 2 PlatformAdminWithStepUp + 7 gün appeal
  ↓ [Closure proposal]
CustomerNoticeDeliveredAtUtc (SMTP kanıt) → EligibleForExecutionAtUtc
  ↓ Appeal penceresi dolar (itiraz yok)
[Execute closure] (PlatformAdminWithStepUp)
  ├─ Identity kapat → Tenant membership revoke → Admin completion
  ├─ Identity OK + Admin fail → retry (aynı e-posta yeniden gönderilmez)
  └─ Aktif membership var → "Önce üyelikleri revoke et"
```

### 16.5 Customer iptal akışı

```
/hesabim/talepler → PendingApproval talep → [İptal et]
  ↓ Idempotency-Key
POST /api/public/.../cancel
  ├─ 200 → talep CancelledByCustomer, fade-out
  ├─ 409 → "Artık iptal edilemez" (onaylandı/süresi doldu)
  └─ 429 → rate limit
```

---

## 17. User Journey (Her Rol İçin 1 Tam Gün)

### 17.1 Customer — Ayşe'nin günü
1. **09:00** — `/kesfet` açar, "saç bakımı Kadıköy" arar.
2. **09:05** — `/isletme/studio-aura` profili görür, hizmet menüsünü inceler.
3. **09:10** — `#rezervasyon` panelinde 5 adımlı wizard: hizmet + personel + tarih/saat + onay özeti → talep gönderir (`PendingApproval`).
4. **09:30** — `/hesabim/talepler`'de talebini görür (amber "Onay bekliyor" + TTL badge).
5. **11:00** — İşletme onaylar → bildirim → `/hesabim/talepler`'de "Onaylandı" (yeşil).
6. **14:00** — Randevuya gider, hizmet alır.
7. **16:00** — `/hesabim/degerlendirmeler`'den 5 yıldız + yorum yazar.

### 17.2 BusinessOwner — Mehmet'in günü
1. **08:30** — `/panel` açar, tenant context + inbox + schedule görür.
2. **08:35** — Inbox'ta 4 PendingApproval talep, TTL badge'leri (1 kritik, 2 uyarı, 1 nötr).
3. **08:40** — Kritik talebi onaylar (conflict uyarısı + "yine de onayla").
4. **09:00** — `/panel/takvim` gün view, 9 randevu görür, bir rebook yapar.
5. **10:00** — `/panel/subeler` yeni personel ekler, yetkinlik atar.
6. **14:00** — `/panel/hizmetler` fiyat günceller (backend bekler — "yakında").
7. **17:00** — Tamamlanan randevuları `Complete`, gelmeyeni `NoShow`.

### 17.3 BranchManager — Zeynep'in günü
1. **09:00** — `/panel` açar (kendi şube, switcher'da tek şube).
2. **09:05** — Inbox'ta kendi şubesinin talepleri, onaylar.
3. **10:00** — `/panel/personel` kendi personelinin müsaitliğini yönetir.
4. **14:00** — `/panel/kaynaklar` bir koltuğu "out-of-service" yapar.

### 17.4 Staff — Ali'nin günü
1. **09:00** — `/panel/programim` bugünün randevuları saat saat.
2. **09:30** — İlk müşteri geldi, not ekler.
3. **14:00** — `/panel/takvimim` haftalık görünüm.

### 17.5 PlatformAdmin — Can'ın günü
1. **09:00** — `/platform` → step-up expired, `/platform/adim`'a düşer, MFA girer.
2. **09:05** — `/platform/abuse` 3 yeni rapor, 2 itiraz.
3. **09:30** — Bir kullanıcının abuse geçmişini inceler, strike confirm (step-up altında).
4. **10:00** — `/platform/tenantlar` bir tenant suspend (exact "SUSPEND" + reason + audit).
5. **14:00** — `/platform/itirazlar` bir appeal review.
6. **16:00** — `/platform/operasyon` read-only reconciliation (failed notification var).

---

## 18. Veri Görüntüleme Pattern'leri

| Pattern | Kullanım | Detay |
|---|---|---|
| DataTable | tenant listesi, audit log | sorted kolon, cursor pagination, hover satır |
| Card grid | işletmeler, personeller, şubeler | 1-2-3 kolon responsive, fade-up stagger |
| Calendar grid | takvim scheduler | Day/Week, saat kolonu, absolute-height event |
| Timeline | randevu schedule, audit timeline | dikey, saat bazlı, durum renk çizgisi |
| Chart (AreaChart) | gelir, istatistik | gradient fill, custom dark tooltip, tr-TR |
| KPI card | dashboard metrikleri | **gerçek** metrik, trend ok |
| List + filter | talepler, randevular | filter tabs + search + sort |
| Tree view | hizmet → varyant | açılır/kapanır dallar |
| Badge cloud | yetkinlik, etiket | mono badge grid |
| Detail panel | randevu/talep detayı | sol liste + sağ detay (split) |

---

## 19. Erişilebilirlik (WCAG 2.2 AA Full Checklist)

- [ ] **1.1.1** Tüm dekoratif olvan ikonlara `aria-label` veya `aria-hidden`.
- [ ] **1.3.1** Semantic HTML (`<nav>`, `<main>`, `<header>`, `<dialog>`).
- [ ] **1.3.2** Logical reading order (DOM sırası = görsel sıra).
- [ ] **1.4.1** Renk tek başına anlam taşımaz (badge + text + ikon).
- [ ] **1.4.3** Kontrast AA (text ≥4.5:1, large ≥3:1).
- [ ] **1.4.10** Reflow (320px'de horizontal scroll yok).
- [ ] **1.4.11** Non-text kontrast ≥3:1 (border, ikon).
- [ ] **1.4.13** Hover/focus içeriği kapatılabilir, hoverlanabilir, kalıcı.
- [ ] **2.1.1** Tüm fonksiyonlar klavye ile erişilebilir.
- [ ] **2.1.2** Focus trap (modal, drawer).
- [ ] **2.1.2** No keyboard trap.
- [ ] **2.4.1** Skip link (ana içeriğe geç).
- [ ] **2.4.3** Logical focus order.
- [ ] **2.4.7** Focus-visible (ring-2 accent).
- [ ] **2.5.3** Label in name (buton text = accessible name).
- [ ] **3.2.1** On focus no context change.
- [ ] **3.2.2** On input no unexpected context change.
- [ ] **3.3.1** Error identification (inline + ARIA).
- [ ] **3.3.2** Labels or instructions.
- [ ] **3.3.3** Error suggestion.
- [ ] **4.1.2** Name, Role, Value (ARIA).
- [ ] **4.1.3** Status messages (ARIA live).
- [ ] `prefers-reduced-motion` saygılı.
- [ ] `prefers-color-scheme` (ileri faz dark/light toggle).
- [ ] Screen reader test (NVDA, VoiceOver).

---

## 20. Responsive (Breakpoint Bazında Her Sayfa)

**Breakpoint'ler (Tailwind):**
- `sm` 640px (büyük telefon)
- `md` 768px (tablet)
- `lg` 1024px (küçük laptop, sidebar persistent)
- `xl` 1280px (desktop)
- `2xl` 1536px (geniş desktop)

| Sayfa | Mobil (<md) | Tablet (md-lg) | Desktop (lg+) |
|---|---|---|---|
| Landing | tek kolon, hamburger navbar | tek kolon | çok kolon, persistent navbar |
| Keşif | 1 kolon kart, drawer filtre | 2 kolon | 3 kolon |
| İşletme profili | tek kolon, wizard full | tek kolon | split (profil + wizard) |
| Customer panel | alt tab bar, tek kolon | sidebar drawer | sidebar persistent |
| Business panel | drawer sidebar, tek kolon | drawer sidebar | sidebar persistent, çok kolon |
| Platform panel | drawer sidebar, tek kolon | drawer sidebar | sidebar persistent |
| Takvim | gün view zorunlu | gün/week | gün/week |
| Talepler inbox | kart stack | kart stack + filtre üst | split (filtre + liste) |
| Dialog | full-screen modal | modal | modal centered |

---

## 21. Performance Budget

- **First Contentful Paint** < 1.8s (3G hızlı).
- **Largest Contentful Paint** < 2.5s.
- **Cumulative Layout Shift** < 0.1.
- **Total Blocking Time** < 200ms.
- **Bundle JS** < 200KB initial (gzip).
- **Image** lazy load, WebP/AVIF, responsive `srcset`.
- **Font** `display=swap`, preconnect.
- **Animation** GPU-accelerated (transform, opacity), layout/paint avoid.
- **Skeleton** ilk paint'te, içerik yüklenene kadar.

---

## 22. Micro-Interaction Kataloğu

| Element | Trigger | Effect |
|---|---|---|
| Button primary | hover | `brightness-110 -translate-y-0.5` (150ms) |
| Button primary | active | `scale-[0.98]` (100ms) |
| Button primary | focus-visible | `ring-2 ring-[var(--rs-accent)] ring-offset-2` |
| Card | hover | `-translate-y-0.5 shadow-card` (interactive) |
| Glass card | hover | `bg-[var(--rs-glass-strong)]` (200ms) |
| Nav item | hover | `bg-[var(--rs-surface-muted)] text-[var(--rs-ink)]` |
| Nav item active | — | gradient bg persistent |
| Tab selected | click | `bg-[var(--rs-glass-strong)]` slide |
| Tooltip | hover/focus | fade 150ms |
| Dialog | open | overlay fade + panel slide-in/fade-up |
| Drawer | open | overlay fade + drawer slide-in (300ms) |
| Sidebar collapse | toggle | width transition (300ms ease-in-out) |
| Star rating input | hover | star scale + color (100ms) |
| Skeleton | load | pulse (1.5s infinite) |
| TTL critical badge | — | pulse-warning (1.8s infinite) |
| Step-up indicator | — | pulse-success (1.8s infinite) |
| Notification dot | unread | pulse (2s infinite) |
| Talep kart onay | success | fade-out + height collapse (200ms) |
| Wizard step geçiş | next | slide-in (400ms) |
| Orb background | — | orb1/2/3 drift (22s/17s/28s infinite) |
| Form input | focus | border-accent + ring-4 (150ms) |
| Toast | show | slide-up + fade (200ms) |

---

## 23. Lokalizasyon Hazırlığı

- **Varsayılan dil Türkçe**. Tüm UI metinleri Türkçe.
- İleride i18n için: tüm metinler key-based olmalı (örn `t("button.save")`), hardcoded Türkçe string değil.
- **Ancak MVP'de**: hardcoded Türkçe string kabul (i18n altyapısı ertelendi).
- Tarih/saat: `Intl.DateTimeFormat("tr-TR", ...)`.
- Para: `Intl.NumberFormat("tr-TR", { currency: "TRY" })`.
- Sayı: `Intl.NumberFormat("tr-TR")`.
- Çoğul: Türkçe çoğul karmaşık değil, basit "N talep".
- Domain terimleri: başka dile çevrilmez (Talep, Şube, Personel).
- Backend enum adları İngilizce kalır (`PendingApproval`), UI label'a map'lenir.

---

## 24. Güvenlik / PII / Audit UI Yansıması

- **PII maskeleme**: müşteri e-posta/telepon her zaman maskeli (işletme panelinde).
- **"PII gizli" chip**: maskeli veri yanında.
- **Internal reason gizleme**: müşteri kendi abuse kaydında internal reason/admin actor görmez, güvenli `CustomerNotice`.
- **Step-up indicator**: PlatformAdmin yüzeylerde kalıcı.
- **Exact confirmation**: kalıcı aksiyonlarda ("SUSPEND" yaz).
- **Idempotency**: retry güvenli, double-submit önlenir.
- **Audit notu**: mutation dialog'larında "Bu işlem auditable" hatırlatma.
- **Tenant header**: merkezi API client'tan, kullanıcı serbest seçemez.
- **Cookie auth**: browser token storage yok, UI auth state yansır.
- **404 over 403**: tenant dışı kaynak 404 (varlık sızdırmaz).
- **Rate limit UI**: 429 → geri sayım + bekle.
- **Mojibake kontrolü**: her build check'te, UTF-8 without BOM.

---

## 25. Tasarım Dosyası Organizasyonu + Storybook Story Şablonu

### 25.1 Figma dosya yapısı

```
RezSaaS Design System
├── 01 — Tokens (renk, tipografi, spacing, radius, shadow, motion)
├── 02 — Primitives (atomlar: button, card, badge, ...)
├── 03 — Patterns (kompozit: tenant switcher, inbox toolbar, ...)
├── 04 — Templates (sayfa iskeletleri: PublicLayout, CustomerShell, ...)
├── 05 — Pages (tam sayfalar: landing, inbox, takvim, ...)
├── 06 — Roles (her rolün sidebar + sayfaları)
├── 07 — States (loading, empty, error varyantları)
├── 08 — Icons (lucide mapping)
└── 09 — Motion (animasyon örnekleri)
```

### 25.2 Storybook story şablonu

```tsx
// Button.stories.tsx
export default {
  title: "Primitives/Button",
  component: Button,
  parameters: { layout: "centered", a11y: { config: { rules: [{ id: "color-contrast", enabled: true }] } } },
  argTypes: {
    variant: { control: "select", options: ["primary", "secondary", "outline", "ghost", "danger", "success"] },
    size: { control: "select", options: ["sm", "md", "lg"] },
    disabled: { control: "boolean" },
    loading: { control: "boolean" }
  }
};
export const Primary = { args: { variant: "primary", children: "Kaydet" } };
export const AllVariants = () => /* render all variants grid */;
```

---

## 26. Token Dosyası Örneği

### 26.1 CSS (globals.css özeti)

```css
:root {
  color-scheme: dark;
  /* Background */
  --rs-bg: #080c14;
  --rs-bg-deep: #050810;
  --rs-surface: #0d1120;
  --rs-surface-strong: #111626;
  --rs-glass: rgba(255, 255, 255, 0.04);
  --rs-glass-strong: rgba(255, 255, 255, 0.08);
  --rs-surface-muted: rgba(255, 255, 255, 0.06);
  /* Border */
  --rs-border: rgba(255, 255, 255, 0.08);
  --rs-border-strong: rgba(255, 255, 255, 0.16);
  /* Ink */
  --rs-ink: #f0f2fa;
  --rs-ink-soft: #c7cad6;
  --rs-muted: #94a3b8;
  --rs-muted-strong: #cbd5e1;
  /* Accent */
  --rs-accent: #6366f1;
  --rs-accent-strong: #818cf8;
  --rs-accent-soft: rgba(99, 102, 241, 0.16);
  --rs-accent-violet: #8b5cf6;
  --rs-accent-violet-soft: rgba(139, 92, 246, 0.16);
  /* Semantic */
  --rs-success: #10b981; --rs-success-soft: rgba(16, 185, 129, 0.16);
  --rs-warning: #f59e0b; --rs-warning-soft: rgba(245, 158, 11, 0.16);
  --rs-danger: #ef4444; --rs-danger-strong: #dc2626; --rs-danger-soft: rgba(239, 68, 68, 0.16);
  /* Misc */
  --rs-focus: #6366f1;
  --rs-radius: 0.625rem; --rs-radius-lg: 1rem; --rs-radius-xl: 1.5rem;
  --rs-shadow-card: 0 24px 80px rgba(0, 0, 0, 0.45);
  --rs-shadow-soft: 0 4px 30px rgba(0, 0, 0, 0.35);
  --rs-shadow-button: 0 1px 2px rgba(0,0,0,0.4), 0 8px 28px rgba(99,102,241,0.28), inset 0 1px 0 rgba(255,255,255,0.18);
  --rs-font-display: "Plus Jakarta Sans", sans-serif;
  --rs-font-sans: "Inter", sans-serif;
  --rs-font-mono: "JetBrains Mono", monospace;
}
.rs-gradient-text { background: linear-gradient(135deg, var(--rs-accent-strong), var(--rs-accent-violet)); -webkit-background-clip: text; background-clip: text; color: transparent; }
.rs-gradient-bg { background: linear-gradient(135deg, var(--rs-accent), var(--rs-accent-violet)); }
```

### 26.2 JSON (Tailwind config / design tokens)

```json
{
  "color": {
    "bg": { "DEFAULT": "#080c14", "deep": "#050810" },
    "surface": { "DEFAULT": "#0d1120", "strong": "#111626" },
    "glass": { "DEFAULT": "rgba(255,255,255,0.04)", "strong": "rgba(255,255,255,0.08)" },
    "ink": { "DEFAULT": "#f0f2fa", "soft": "#c7cad6" },
    "muted": { "DEFAULT": "#94a3b8", "strong": "#cbd5e1" },
    "accent": { "DEFAULT": "#6366f1", "strong": "#818cf8", "soft": "rgba(99,102,241,0.16)", "violet": "#8b5cf6" },
    "success": { "DEFAULT": "#10b981", "soft": "rgba(16,185,129,0.16)" },
    "warning": { "DEFAULT": "#f59e0b", "soft": "rgba(245,158,11,0.16)" },
    "danger": { "DEFAULT": "#ef4444", "strong": "#dc2626", "soft": "rgba(239,68,68,0.16)" }
  },
  "fontFamily": {
    "display": ["Plus Jakarta Sans", "sans-serif"],
    "sans": ["Inter", "sans-serif"],
    "mono": ["JetBrains Mono", "monospace"]
  },
  "borderRadius": { "sm": "0.375rem", "DEFAULT": "0.625rem", "md": "0.75rem", "lg": "1rem", "xl": "1.5rem" }
}
```

---

## 27. Kısıtlar (Hard Rules — Bozma)

- **Mock veri YOK**: backend bekleyen ekranlar "yakında" placeholder.
- **Token-bazlı renkler**: hard-coded hex yasak, `bg-[var(--rs-*)]`.
- **Türkçe UI metinleri**: tüm etiket/button/empty/error Türkçe.
- **İngilizce kod**: comment, dosya adı, route, CSS class, değişken İngilizce.
- **Tip güvenliği**: OpenAPI'den üretilmiş TS tipleri, elle DTO çoğaltma yok.
- **Cookie auth**: browser token storage yok.
- **AGENTS.md uyumu**: tenant izolasyon, double-booking engel, 24 saat TTL, PII maskeleme, idempotity, resource gizleme, şube saati sadakati.
- **UTF-8 without BOM**: tüm kaynak dosyalar, her build check'te mojibake kontrolü zorunlu (Türkçe karakterler `ı ş ğ ç ö ü İ Ş Ğ Ç Ö Ü` ve U+FFFD).
- **WCAG 2.2 AA**: kontrast, klavye, screen reader, focus-visible.
- **Sahte metrik yok**: KPA'lar gerçek backend'den.
- **Tutarlı Türkçe terim**: domain kavramları tutarlı (Şube her yerde Şube).

---

## 28. Teslimat Beklentisi

Üret:

### 28.1 Sidebar component'leri (her rol)
- Customer, Staff, BranchManager, BusinessOwner, PlatformAdmin, PlatformSupport.
- Mobil drawer varyantı, collapse animasyonu.

### 28.2 Page layout taslakları (her rolün tüm sayfaları)
Sidebar + header + içerik bölütleri.

### 28.3 High-fidelity mockup'lar (8 ana sayfa)
1. Landing
2. Keşif
3. İşletme profili + Rezervasyon wizard (5 adım)
4. Customer Taleplerim
5. Business Talepler inbox (TTL, PII, conflict)
6. Business Takvim (Day/Week scheduler)
7. PlatformAdmin Tenantlar (lifecycle dialog)
8. Step-up gate

### 28.4 Component states
Her component: default / hover / active / focus / disabled / loading / error / empty.

### 28.5 Design tokens
CSS + JSON formatında.

### 28.6 İkon eşlemesi
Lucide-react full mapping.

### 28.7 A11y checklist
WCAG 2.2 AA tüm maddeleri.

### 28.8 Responsive spec
Her sayfa için mobil/tablet/desktop.

### 28.9 Storybook story şablonları
Her primitive için.

### 28.10 User journey görselleştirme
Her rol için 1 gün akışı.

---

## 29. Anti-Pattern Listesi (Yapma)

- ❌ Dashboard'da her şeyi yığma (her işlem ayrı sayfa).
- ❌ Sahte KPA/metrik uydurma.
- ❌ Backend'siz form "başarılı" toast.
- ❌ Hard-coded renk (`bg-indigo-600`).
- ❌ İngilizce UI metni.
- ❌ Müşteri yüzeyinde resource gösterme.
- ❌ PendingApproval'ı Confirmed gibi gösterme.
- ❌ TTL badge olmadan talep listesi.
- ❌ Conflict uyarısız onay akışı.
- ❌ Step-up'suz PlatformAdmin mutation.
- ❌ Körüne tooltip/modal (focus trap yok).
- ❌ Pill buton (referans köşeli rounded-lg).
- ❌ Tasarım token'larını atlayıp keyfi değer.
- ❌ Mojibake'li Türkçe karakter dosyaya yazma.
- ❌ Sahte "Daha fazla" butonu (gerçek pagination yok).
- ❌ Müşteri internal reason/admin actor gösterme.
- ❌ Tarayıcı TZ'ine sessiz saat çevirme.
- ❌ Kalıcı aksiyonda exact confirmation olmadan.
- ❌ Toast'da raw stack trace / internal hata.
- ❌ PII log'a/response'a sızdırma.
- ❌ Mock veriyi production'a taşıma.
- ❌ Backend enum adını kullanıcıya raw gösterme.
- ❌ Çoklu primary CTA (sayfa başına tek gradient).
- ❌ Boş state'de sahte dolgu.
- ❌ Loading'de içerik kayması (CLS).
- ❌ Klavye ile erişilemeyen hover-only fonksiyon.
- ❌ Renk tek başına anlam (sadece kırmızı = hata, text de olmalı).
- ❌ Tablo sayfalama yok infinite scroll (cursor pagination).
- ❌ Form validation sadece submit'te (inline da olmalı).
- ❌ Modal'da Esc çalışmıyor.
- ❌ Drawer'da focus trap yok.
- ❌ animasyon `prefers-reduced-motion`'suz.

---

## 30. Quality Gate Checklist (Teslimat Öncesi)

- [ ] Tüm sayfalar 3 state (loading/empty/error) ile geldi mi?
- [ ] Tüm roller için sidebar ve sayfa envanteri tamam mı?
- [ ] Türkçe metinler tutarlı mı (domain terimleri)?
- [ ] Renkler token-bazlı mı (hard-coded yok)?
- [ ] Tipografi scale uygulandı mı (Plus Jakarta + Inter + JetBrains Mono)?
- [ ] Motion `prefers-reduced-motion` saygılı mı?
- [ ] WCAG 2.2 AA kontrastı geçer mi?
- [ ] Klavye navigasyonu tüm fonksiyonlarda çalışır mı?
- [ ] Focus-visible ring tüm interactive element'lerde mi?
- [ ] Modal/drawer focus trap + Esc mi?
- [ ] PII maskeleme uygulamalı mı?
- [ ] TTL badge'ler doğru renk/pulse mu?
- [ ] Resource müşteri yüzeyinde gizli mi?
- [ ] Şube saati tooltip ile gösteriliyor mu?
- [ ] Step-up indicator PlatformAdmin'de kalıcı mı?
- [ ] Conflict uyarısı onay akışında mı?
- [ ] Exact confirmation kalıcı aksiyonlarda mı?
- [ ] Idempotency copy ("tekrar deneyebilirsin") var mı?
- [ ] Tenant switcher'da serbest GUID yok mu?
- [ ] Sahte metrik/KPA yok mu?
- [ ] Backend bekleyen ekranlar "yakında" mı?
- [ ] Responsive 3 breakpoint'te test edildi mi?
- [ ] Storybook story'leri var mı?
- [ ] Token dosyası (CSS + JSON) hazır mı?
- [ ] İkon eşlemesi tamam mı?
- [ ] User journey her rol için görselleştirildi mi?

---

## 31. Referans İlham Linkleri

**Tasarım dili için ilham (kopya değil, referans):**
- Linear (lineer.app) — dark glass, sidebar-driven SaaS
- Vercel dashboard — dark, minimal, gradient accent
- Stripe dashboard — veri yoğun, sidebar pattern
- Notion — sidebar collapse, drawer
- GitHub (dark mode) — token'lı renk, mono font kullanımı
- Arc browser — glassmorphism, modern sidebar
- Raycast — command palette, dark accent
- Figma — design system organizasyon

**Domain için ilham:**
- Booksy (booksy.com) — salon rezervasyon, müşteri/İşletme ayrımı
- Fresha (fresha.com) — salon marketplace, calendar
- Vagaro — salon operasyon, personel yönetimi

**Türkçe UI için:**
- Trendyol, Getir, Hepsiburada — Türkçe e-ticaret UX pattern'leri
- Insider — Türk B2B SaaS dark tema

---

## 32. Stil Örnekleri (CSS + Tailwind)

```css
/* Glass card */
.glass-card {
  background: rgba(255, 255, 255, 0.04);
  backdrop-filter: blur(16px);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 1rem;
}

/* Gradient primary button */
.btn-primary {
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: white;
  box-shadow: 0 8px 28px rgba(99, 102, 241, 0.28);
  border-radius: 0.75rem;
}
.btn-primary:hover { filter: brightness(1.1); transform: translateY(-2px); }
.btn-primary:active { transform: scale(0.98); }
.btn-primary:focus-visible { outline: 2px solid #6366f1; outline-offset: 2px; }

/* Mono badge */
.badge {
  font-family: "JetBrains Mono", monospace;
  font-size: 11px;
  padding: 2px 8px;
  border-radius: 0.375rem;
  border: 1px solid;
}

/* Pulse warning */
@keyframes pulse-warning {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.45; }
}

/* Animated orbs */
@keyframes orb1 {
  0%, 100% { transform: translate(0, 0) scale(1); }
  33% { transform: translate(40px, -40px) scale(1.1); }
  66% { transform: translate(-25px, 25px) scale(0.92); }
}

/* Sidebar nav active */
.nav-item-active {
  background: linear-gradient(to bottom right, #6366f1, #8b5cf6);
  color: white;
  box-shadow: 0 8px 28px rgba(99, 102, 241, 0.3);
}
```

```tsx
// Sidebar nav item active state
className="bg-gradient-to-br from-indigo-600 to-violet-600 text-white shadow-lg shadow-indigo-600/30"

// Glass section
className="backdrop-blur-xl bg-white/[0.04] border border-white/[0.08] rounded-2xl"

// Mono eyebrow
className="font-mono text-[10px] uppercase tracking-[0.18em] text-white/40"

// Gradient text (hero)
className="bg-gradient-to-r from-indigo-400 to-violet-400 bg-clip-text text-transparent"

// TTL critical pulse badge
className="bg-red-500/15 text-red-400 border border-red-500/25 animate-pulse"

// Step-up active indicator
className="bg-emerald-500/15 text-emerald-400 border border-emerald-500/25 flex items-center gap-2"
// + <span className="h-2 w-2 rounded-full bg-emerald-400 animate-pulse" />

// Glass button (secondary)
className="bg-white/[0.04] border border-white/[0.08] text-white hover:bg-white/[0.08] backdrop-blur-xl rounded-lg"
```

---

**Bu prompt RezSaaS'in AGENTS.md mimari/güvenlik kurallarıyla uyumlu. Tasarım kararları çekirdek domain invariantlarını (onay akışı, 24 saat TTL, PII maskeleme, tenant izolasyon, double-booking engeli, resource gizleme, şube saati sadakati, idempotity, step-up MFA) bozmamalı. Tasarım aracı sahte veri/metrik üretmemeli; backend bekleyen ekranlar "yakında" placeholder olarak kalmalı. UI dili Türkçe; kod dili İngilizce. Her kaynak dosya UTF-8 without BOM ve her build check'te mojibake kontrolü zorunludur.**
