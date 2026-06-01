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

