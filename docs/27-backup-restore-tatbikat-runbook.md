# Backup / Restore Tatbikat Runbook

Bu runbook Phase 3 güvenlik ve operasyon kapanış kriteri olan PostgreSQL
backup/restore doğrulamasını tanımlar.

## Amaç

- Production verisinin kaybı veya yanlış migration durumunda geri dönüş yolunu
  kanıtlamak.
- Backup dosyasının yalnız oluşturulduğunu değil, ayrı bir veritabanına
  restore edilebildiğini doğrulamak.
- Tatbikat çıktısını operasyon kaydı olarak saklamak; backup dosyasını repo'ya
  commit etmemek.

## Yerel Tatbikat Komutları

Docker Desktop ve `compose.yaml` içindeki PostgreSQL servisi ayaktayken:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Backup-Postgres.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Verify-PostgresRestore.ps1
```

`Verify-PostgresRestore.ps1` backup üretir, geçici
`rezsaas_restore_verify_*` veritabanına restore eder, user table sayısını
okur ve geçici veritabanını siler.

## Saklama ve Güvenlik

- Backup çıktıları `artifacts/backups/` altındadır ve `.gitignore` ile
  commit dışıdır.
- Backup dosyaları PII ve operasyon verisi taşıyabilir; issue, PR, chat veya
  log içine eklenmez.
- Production backup secret'ları repo veya workflow içine gömülmez; secret
  manager üzerinden sağlanır.
- Restore tatbikatı production veritabanına değil, izole staging/verification
  veritabanına yapılır.

## Kabul Kriteri

- Backup komutu hatasız tamamlanır.
- Restore komutu `ON_ERROR_STOP=1` ile hatasız tamamlanır.
- Restore sonrası schema/table metadata okunabilir.
- Tatbikat tarihi, ortam, backup dosya adı, restore sonucu ve varsa aksiyon
  maddeleri operasyon kaydına işlenir.

## Production Notu

Production backup/restore için managed PostgreSQL sağlayıcısının point-in-time
recovery, encryption-at-rest, backup retention ve restore drill özellikleri
ayrıca etkinleştirilmelidir. Yerel scriptler production güvenlik modelinin
yerine geçmez; runbook semantiğini ve geliştirici/staging tatbikatını sağlar.
