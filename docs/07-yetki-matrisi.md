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
- Public rezervasyon isteği oluşturma yalnızca authenticated platform hesabıyla yapılır; global `Customer` rol kaydı aranmaz, aktif hesap müşteri bağlamı kabul edilir.
- İşletme onay paneli `BusinessOwner` için tenant-wide; `BranchManager` için branch scope kontrollüdür. `Staff` varsayılan olarak onay/ret veremez.
- İşletme panelinde müşteri e-posta/telefon bilgisi yalnızca maskelenmiş döner; raw PII panel response kontratına eklenmez.
- Müşteri kendi talep listesi/detayı için yalnızca kendi `UserAccount` kapsamında veri görebilir; başka kullanıcının talebi `404` kabul edilir.

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
| Abuse işaretleme | Hayır | Politika ile | Atandığı şube | Tenant | İnceleme | Evet |
| Strike/ban uygulama | Hayır | Hayır | Hayır | Hayır | Öneri | Evet |
| Audit log görüntüleme | Kendi olayları | Sınırlı | Şube | Tenant | Gerekçeli | Evet |

## Güvenlik Kuralları

- Varsayılan yaklaşım deny-by-default'tur.
- Yetki rol kadar kapsamı da kontrol eder: `tenant_id`, `branch_id`, gerekirse `staff_member_id`.
- Platform destek erişimi süreli, gerekçeli ve auditli olmalıdır.
- Kritik aksiyonlar MFA ve gerektiğinde step-up doğrulama ister.
