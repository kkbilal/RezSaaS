# Yetki Matrisi Taslağı

## Roller

| Rol | Kapsam | Amaç |
| --- | --- | --- |
| `Customer` | Platform hesabı | Keşif, talep oluşturma, kendi taleplerini yönetme |
| `Staff` | Atandığı şubeler | Kendi takvimi ve izin verilen randevu detayları |
| `BranchManager` | Atandığı şubeler | Şube operasyonu, talep onay/ret, staff/resource yönetimi |
| `BusinessOwner` | Tenant | İşletme ayarları, üyelikler, tüm şubeler ve raporlar |
| `PlatformSupport` | Sınırlı platform scope | Destek incelemesi; varsayılan read-only ve gerekçeli erişim |
| `PlatformAdmin` | Platform | Abuse yaptırımı, güvenlik ve operasyon yönetimi |

## Yetki Özeti

| Aksiyon | Customer | Staff | BranchManager | BusinessOwner | PlatformSupport | PlatformAdmin |
| --- | --- | --- | --- | --- | --- | --- |
| İşletme keşfi | Evet | Evet | Evet | Evet | Evet | Evet |
| Rezervasyon isteği oluşturma | Kendi hesabı | Hayır | Hayır | Hayır | Hayır | Hayır |
| Kendi talebini iptal | Evet | Hayır | Hayır | Hayır | Hayır | Hayır |
| Talep onay/ret | Hayır | Politika ile | Atandığı şube | Tenant | Hayır | Acil operasyon ile |
| Staff/resource düzenleme | Hayır | Hayır | Atandığı şube | Tenant | Hayır | Hayır |
| Rol ve üyelik yönetimi | Hayır | Hayır | Hayır | Tenant | Hayır | Acil operasyon ile |
| Abuse işaretleme | Hayır | Politika ile | Atandığı şube | Tenant | İnceleme | Evet |
| Strike/ban uygulama | Hayır | Hayır | Hayır | Hayır | Öneri | Evet |
| Audit log görüntüleme | Kendi olayları | Sınırlı | Şube | Tenant | Gerekçeli | Evet |

## Güvenlik Kuralları

- Varsayılan yaklaşım deny-by-default'tur.
- Yetki rol kadar kapsamı da kontrol eder: `tenant_id`, `branch_id`, gerekirse `staff_member_id`.
- Platform destek erişimi süreli, gerekçeli ve auditli olmalıdır.
- Kritik aksiyonlar MFA ve gerektiğinde step-up doğrulama ister.
