# Yetki Matrisi Taslağı

## Roller

| Rol | Kapsam | Amaç |
| --- | --- | --- |
| `Customer` | Aktif platform hesabı | Keşif, talep oluşturma, kendi taleplerini yönetme; global Identity rolü değildir |
| `Staff` | Atandığı şubeler | Kendi takvimi ve izin verilen randevu detayları |
| `BranchManager` | Atandığı şubeler | Şube operasyonu, talep onay/ret, staff/resource yönetimi |
| `BusinessOwner` | Tenant | İşletme ayarları, üyelikler, tüm şubeler ve raporlar |
| `PlatformSupport` | Sınırlı platform scope | Destek incelemesi; varsayılan read-only ve gerekçeli erişim |
| `PlatformAdmin` | Platform | Abuse yaptırımı, güvenlik ve operasyon yönetimi |

## Rol Sınırı

- Global Identity rolleri: `PlatformAdmin`, `PlatformSupport`
- Tenant membership rolleri: `BusinessOwner`, `BranchManager`, `Staff`
- `Customer`: aktif/doğrulanmış platform hesabının varsayılan kullanım bağlamı
- Tenant rolleri global `AspNetRoles` tablosuna eklenmez.
- `BusinessOwner` tenant kapsamlıdır ve branch scope alamaz.
- `BranchManager` ve `Staff` üyelikleri branch scope ile sınırlandırılabilir.
- Tenant/işletme yönetim endpoint'leri privileged MFA/step-up ve ilk `PlatformAdmin` bootstrap prosedürü tamamlanmadan yayınlanmaz.
- Platform control-plane tenant liste/detay/provisioning ve membership add/suspend/revoke aksiyonları `PlatformAdminWithStepUp`, aktif `UserAccount` doğrulaması ve audit ister.
- `Revoked` membership terminaldir; yeniden `Suspended` veya aktif duruma çevrilmez.
- Son aktif `BusinessOwner` membership'i suspend/revoke edilemez.
- Tenant lifecycle suspend/reactivate/close yalnızca `PlatformAdminWithStepUp` ile yürütülür; `Closed` terminaldir.
- `Suspended` ve `Closed` tenant public discovery, yeni rezervasyon isteği ve işletme operasyonlarına kapalıdır; müşteri kendi mevcut taleplerini görmeye ve uygun durumda iptal etmeye devam eder.
- Abuse event inceleme ve sanction apply/revoke yalnızca `PlatformAdminWithStepUp` ile yürütülür; kalıcı hesap kapatma sanction endpoint'inden yapılamaz.
- İşletme abuse raporu `BusinessOwner` için tenant-wide, `BranchManager` için branch-scoped çalışır; bildirim yalnızca inceleme sinyalidir ve strike/sanction üretme yetkisi vermez.
- Abuse raporu confirm/dismiss ve strike revoke yalnızca `PlatformAdminWithStepUp` ile yürütülür.
- Müşteri yalnızca kendi strike, aktif bloklayıcı sanction ve uygun closure case kayıtlarını görebilir/itiraz edebilir; başka kullanıcı hedefi `404` kabul edilir.
- Closure proposal/review/execute yalnızca `PlatformAdminWithStepUp` ile yürütülür; proposal ve approval iki farklı admin ister.
- Platform rolü veya aktif tenant membership taşıyan kullanıcı için kalıcı hesap kapatma proposal/execute reddedilir.
- Aktif closure case taşıyan kullanıcıya yeni tenant owner/membership ataması control-plane seviyesinde reddedilir.
- Public rezervasyon isteği oluşturma yalnızca authenticated platform hesabıyla yapılır; global `Customer` rol kaydı aranmaz, aktif hesap müşteri bağlamı kabul edilir.
- İşletme onay paneli `BusinessOwner` için tenant-wide; `BranchManager` için branch scope kontrollüdür. `Staff` varsayılan olarak onay/ret veremez.
- İşletme frontend bağlamı yalnızca authenticated kullanıcının aktif tenant membership'lerinden üretilir; `GET /api/business/context` tenant header istemez ve serbest tenant GUID seçimine izin vermez.
- Business context capability'leri endpoint authz yerine geçmez; her işletme operasyonu yine tenant header, membership scope ve ilgili application service kontrolünden geçer.
- İşletme panelinde müşteri e-posta/telefon bilgisi yalnızca maskelenmiş döner; raw PII panel response kontratına eklenmez.
- Public/customer booking response'ları internal resource GUID veya resource görünen adı taşımaz; resource yalnızca işletme operasyon panelinde label olarak döner.
- Müşteri kendi talep listesi/detayı için yalnızca kendi `UserAccount` kapsamında veri görebilir; başka kullanıcının talebi `404` kabul edilir.
- Müşteri global appointment history endpoint'i slug bilmeden tüm tenant'lar üzerinde yalnızca kendi request/confirmed appointment kayıtlarını görebilir.

## Yetki Özeti

| Aksiyon | Customer | Staff | BranchManager | BusinessOwner | PlatformSupport | PlatformAdmin |
| --- | --- | --- | --- | --- | --- | --- |
| İşletme keşfi | Evet | Evet | Evet | Evet | Evet | Evet |
| Rezervasyon isteği oluşturma | Kendi hesabı | Hayır | Hayır | Hayır | Hayır | Hayır |
| Kendi talebini iptal | Evet | Hayır | Hayır | Hayır | Hayır | Hayır |
| Talep onay/ret | Hayır | Politika ile | Atandığı şube | Tenant | Hayır | Acil operasyon ile |
| Staff/resource düzenleme | Hayır | Hayır | Atandığı şube | Tenant | Hayır | Hayır |
| Rol ve üyelik yönetimi | Hayır | Hayır | Hayır | Tenant | Hayır | Step-up operasyon ile |
| Tenant suspend/reactivate/close | Hayır | Hayır | Hayır | Hayır | Hayır | Step-up operasyon ile |
| Abuse işaretleme | Hayır | Hayır | Atandığı şube | Tenant | İnceleme | Evet |
| Strike/ban uygulama | Hayır | Hayır | Hayır | Hayır | Öneri | Step-up operasyon ile |
| Kendi yaptırımına itiraz | Evet | Kendi hesabı | Kendi hesabı | Kendi hesabı | Hayır | İnceleme |
| Kalıcı hesap kapatma | Hayır | Hayır | Hayır | Hayır | Hayır | İki farklı step-up admin |
| Audit log görüntüleme | Kendi olayları | Sınırlı | Şube | Tenant | Gerekçeli | Evet |

## Güvenlik Kuralları

- Varsayılan yaklaşım deny-by-default'tur.
- Yetki rol kadar kapsamı da kontrol eder: `tenant_id`, `branch_id`, gerekirse `staff_member_id`.
- Platform destek erişimi süreli, gerekçeli ve auditli olmalıdır.
- Kritik aksiyonlar MFA ve gerektiğinde step-up doğrulama ister.
