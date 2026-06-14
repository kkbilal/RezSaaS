# Faz Bazlı Yol Haritası (Phase 0-5)

Bu yol haritası “kod yazmadan önce doğru parçalama” prensibiyle hazırlanmıştır. Her faz, **teslimat odaklı** ve test edilebilir çıktılara bağlanır.

## Fazlar

- `phase-0-kesif-karar.md`: Ürün tanımı, glossary, yetki matrisi ve açık kararların kapanması
- `phase-1-cekirdek-saas.md`: Çekirdek SaaS iskeleti, tenancy, RBAC, audit ve temel booking
- `phase-2-musteri-kesif-rezervasyon-mvp.md`: Keşif, işletme sayfası, hesap ve işletme onaylı rezervasyon MVP
- `phase-3-guvenlik-operasyon.md`: Güvenlik sertleşmesi, backup/restore ve operasyonel derinleşme
- `phase-4-odeme-gelir-optimizasyonu.md`: Ürün doğrulandıktan sonra opsiyonel ödeme ve gelir optimizasyonu
- `phase-5-platformlastirma-genisleme.md`: API/webhook, çoklu şube gelişmiş, CRM/export, İYS, platformlaşma

## Faz Geçiş İlkesi

- Bir fazın koduna başlamadan önce teslimatları ve kabul kriterleri gözden geçirilir.
- Güvenlik, tenancy ve audit minimumları sonraki fazlara ertelenmez.
- Açık ürün kararları `../12-acik-sorular.md`, alınan kararlar `../06-karar-kaydi.md` içinde izlenir.

## Mevcut Durum

- Phase 1, Phase 2 ve Phase 3 tamamlandı.
- Phase 4 backend hazırlığı başladı: `Payments` modülü default kapalı, provider-agnostic ve read-only readiness yüzeyiyle eklendi.
- Phase 5 backend hazırlığı başladı: `Integrations` modülü default kapalı API/webhook readiness yüzeyiyle eklendi; CRM/export ve delivery mutation'ları henüz yayınlanmaz.
- Paralel uygulama adımı frontend F0/F1 iskeleti ve auth/session UX'idir.

## Frontend Paralel Planı

Frontend; ürün/backend fazlarıyla karışmaması için `F0-F7` olarak ayrı fakat
bağımlılıkları açık bir planla ilerler:

- Mimari ve tasarım kararları: `../23-frontend-mimari-tasarim-kararlari.md`
- Uygulama dilimleri: `../24-frontend-uygulama-plani.md`

Frontend fazı, ihtiyaç duyduğu backend endpoint/authz/tenant sözleşmesi
tamamlanmadan sahte veri veya geçici güvenlik bypass'ı ile kapatılmaz.

