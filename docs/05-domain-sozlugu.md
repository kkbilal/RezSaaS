# Domain Sözlüğü

Bu sözlük ürün, tasarım, backend ve frontend ekiplerinin aynı terimi aynı anlamda kullanması için normatiftir.

| Terim | Anlam | Not |
| --- | --- | --- |
| `Tenant` | RezSaaS üzerinde veri izolasyonu sınırı olan müşteri organizasyonu | İlk sürümde bir tenant genellikle bir işletme hesabıdır |
| `TenantStatus` | Tenant yaşam durumunu belirleyen durum | `Suspended` auditli reactivation ile `Active` olabilir; `Closed` terminaldir |
| `TenantMembership` | Platform-global kullanıcı hesabının bir tenant içindeki üyelik kaydı | Rol ve isteğe bağlı branch scope taşır |
| `UserAccount` | Platform-global kimlik hesabı | Tenant üyeliğinden ayrıdır |
| `PlatformRole` | Platform operasyon yetkisi | Yalnızca `PlatformAdmin`, `PlatformSupport` |
| `TenantMembershipRole` | İşletme kapsamlı yetki | `BusinessOwner`, `BranchManager`, `Staff` |
| `TenantAuditLogEntry` | Tenant yönetimiyle ilgili denetlenebilir olay kaydı | Üyelik/ayar değişiklikleri gibi kritik işlemler için append-only tutulur |
| `Control-plane` | Platform operasyonunun tenant ve güvenlik yönetim yüzeyi | Tenant self-service işletme panelinden ayrıdır; `PlatformAdminWithStepUp` ister |
| `Business` | Müşteriye açık marka/işletme profili | Tek domain altında `/isletme/{businessSlug}` ile paylaşılır; public slug global benzersizdir |
| `BusinessGalleryImage` | Public business profilinde yayınlanan galeri görseli | Upload/storage kuralları ayrı dosya yükleme fazında netleşir |
| `PublicStaffDisplayPolicy` | Public profilde staff isimlerinin nasıl gösterileceğini belirleyen işletme ayarı | Slot kapasite hesabını değil yalnızca public gösterimi etkiler |
| `Branch` | İşletmenin fiziksel hizmet noktası | Timezone, şehir/ilçe/adres ve çalışma saatleri şube bazlıdır |
| `StaffMember` | Hizmeti gerçekleştirebilen işletme üyesi | Login hesabı olmak zorunda değildir |
| `StaffSkill` | Staff ile skill arasındaki tenant-scoped ilişki | Bookability hesaplamasında kullanılır |
| `ResourceType` | Fiziksel kapasitenin tipi | Koltuk, oda, yatak, istasyon, cihaz |
| `Resource` | Rezervasyonda kullanılan fiziksel kapasite örneği | MVP'de her randevuda tam olarak bir tane zorunludur |
| `ResourceBlock` | Resource'un belirli zaman aralığında kullanılamama kaydı | Bakım/arıza gibi |
| `Skill` | Staff'ın sunabileceği yetkinlik | Unvan ile karıştırılmaz |
| `Service` | Müşteriye sunulan hizmet ailesi | Saç kesimi gibi |
| `ServiceVariant` | Süre, fiyat ve gereksinimleri belirleyen seçilebilir hizmet varyantı | Uzun saç kesimi gibi |
| `ServiceRequiredSkill` | Hizmet varyantının gerektirdiği skill | Modüller arası doğrudan reference değil GUID kontrat kullanır |
| `BranchWorkingHours` | Şubenin haftalık çalışma saatleri | Branch timezone ile yorumlanır |
| `StaffUnavailableTime` | Staff'ın belirli UTC aralıkta çalışamama kaydı | İzin/kapalı zaman |
| `BookableSlot` | Public yüzeyde gösterilen, talep gönderilebilir zaman aralığı | `PendingApproval` ile bloklanmaz; confirmed appointment, staff unavailable ve resource block ile filtrelenir |
| `AppointmentRequest` | Müşterinin işletmeye gönderdiği rezervasyon talebi | Slotu bloklamaz; `PendingApproval`, `Approved`, `Declined`, `Expired` veya `Superseded` ile kapanır |
| `AppointmentRequestLine` | Talep içindeki hizmet satırı | Süre ve fiyat snapshot içerir |
| `Appointment` | İşletme tarafından onaylanmış kesin rezervasyon | Staff ve resource zamanını bloklar |
| `AppointmentLine` | Kesin rezervasyon içindeki hizmet satırı | Talep satırından snapshot taşır |
| `BookingIdempotencyRecord` | Booking komut tekrarlarını tenant+actor+operation kapsamında takip eden teknik kayıt | Raw idempotency key saklanmaz; hash ve response özeti tutulur |
| `TransactionalMessage` | Rezervasyon gibi mevcut işlemle ilgili operasyonel bildirim | Pazarlama mesajından ayrıdır |
| `CommercialMessage` | Kampanya, yeniden aktivasyon veya satış amaçlı ileti | İzin ve İYS değerlendirmesi gerektirir |
| `AbuseEvent` | Abuse şüphesi veya doğrulanmış ihlal olayı | Otomatik veya manuel kaynaktan gelebilir |
| `UserSanction` | Kullanıcıya uygulanan uyarı, cooldown veya ban | Süreli, gerekçeli, auditlenebilir ve geçmiş kaydı silinmeden revoke edilebilir |
| `AdminAuditLogEntry` | Platform operasyon aksiyon kaydı | Append-only tutulur |
| `IdentityAuditLogEntry` | Identity ve privileged bootstrap aksiyon kaydı | Append-only tutulur |

## Dil Kuralları

- UI ve kodda `resource` yerine yalnızca `chair` kullanma.
- `StaffMember` ile login olabilen `User` kavramını birleştirme.
- Tenant rollerini global platform rolleriyle birleştirme.
- `TenantMembership` ile `UserAccount`'ı tek tabloya sıkıştırma; global kimlik ve tenant yetkisi ayrı kalır.
- `AppointmentRequest` ile `Appointment` kavramını tek tablo veya tek durum listesine sıkıştırma; yaşam döngüleri ayrıdır.
- Unvan (`Title`) bookability belirlemez; `Skill` ve hizmet uygunluğu belirler.
