# Domain Sözlüğü

Bu sözlük ürün, tasarım, backend ve frontend ekiplerinin aynı terimi aynı anlamda kullanması için normatiftir.

| Terim | Anlam | Not |
| --- | --- | --- |
| `Tenant` | RezSaaS üzerinde veri izolasyonu sınırı olan müşteri organizasyonu | İlk sürümde bir tenant genellikle bir işletme hesabıdır |
| `TenantStatus` | Tenant yaşam durumunu belirleyen durum | `Active`, `Suspended`, `Closed` |
| `TenantMembership` | Platform-global kullanıcı hesabının bir tenant içindeki üyelik kaydı | Rol ve isteğe bağlı branch scope taşır |
| `UserAccount` | Platform-global kimlik hesabı | Tenant üyeliğinden ayrıdır |
| `PlatformRole` | Platform operasyon yetkisi | Yalnızca `PlatformAdmin`, `PlatformSupport` |
| `TenantMembershipRole` | İşletme kapsamlı yetki | `BusinessOwner`, `BranchManager`, `Staff` |
| `TenantAuditLogEntry` | Tenant yönetimiyle ilgili denetlenebilir olay kaydı | Üyelik/ayar değişiklikleri gibi kritik işlemler için append-only tutulur |
| `Business` | Müşteriye açık marka/işletme profili | Tek domain altında paylaşılabilir sayfası vardır |
| `Branch` | İşletmenin fiziksel hizmet noktası | Timezone ve çalışma saatleri şube bazlıdır |
| `StaffMember` | Hizmeti gerçekleştirebilen işletme üyesi | Login hesabı olmak zorunda değildir |
| `ResourceType` | Fiziksel kapasitenin tipi | Koltuk, oda, yatak, istasyon, cihaz |
| `Resource` | Rezervasyonda kullanılan fiziksel kapasite örneği | MVP'de her randevuda tam olarak bir tane zorunludur |
| `Skill` | Staff'ın sunabileceği yetkinlik | Unvan ile karıştırılmaz |
| `Service` | Müşteriye sunulan hizmet ailesi | Saç kesimi gibi |
| `ServiceVariant` | Süre, fiyat ve gereksinimleri belirleyen seçilebilir hizmet varyantı | Uzun saç kesimi gibi |
| `AvailabilityRule` | Uygunluk hesaplamasına katılan çalışma/engel kuralı | Çalışma saati, izin, kullanım dışı resource |
| `AppointmentRequest` | Müşterinin işletmeye gönderdiği rezervasyon talebi | Slotu bloklamaz, en fazla 24 saat açık kalır |
| `AppointmentRequestLine` | Talep içindeki hizmet satırı | Süre ve fiyat snapshot içerir |
| `Appointment` | İşletme tarafından onaylanmış kesin rezervasyon | Staff ve resource zamanını bloklar |
| `AppointmentLine` | Kesin rezervasyon içindeki hizmet satırı | Talep satırından snapshot taşır |
| `TransactionalMessage` | Rezervasyon gibi mevcut işlemle ilgili operasyonel bildirim | Pazarlama mesajından ayrıdır |
| `CommercialMessage` | Kampanya, yeniden aktivasyon veya satış amaçlı ileti | İzin ve İYS değerlendirmesi gerektirir |
| `Strike` | Abuse şüphesi veya doğrulanmış ihlal kaydı | Otomatik veya manuel kaynaktan gelebilir |
| `Sanction` | Kullanıcıya uygulanan kısıt veya ban | Süreli, gerekçeli ve auditlenebilir |

## Dil Kuralları

- UI ve kodda `resource` yerine yalnızca `chair` kullanma.
- `StaffMember` ile login olabilen `User` kavramını birleştirme.
- Tenant rollerini global platform rolleriyle birleştirme.
- `TenantMembership` ile `UserAccount`'ı tek tabloya sıkıştırma; global kimlik ve tenant yetkisi ayrı kalır.
- `AppointmentRequest` ile `Appointment` kavramını tek tablo veya tek durum listesine sıkıştırma; yaşam döngüleri ayrıdır.
- Unvan (`Title`) bookability belirlemez; `Skill` ve hizmet uygunluğu belirler.
