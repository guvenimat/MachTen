# MachTen

[![CI](https://github.com/guvenimat/MachTen/actions/workflows/ci.yml/badge.svg)](https://github.com/guvenimat/MachTen/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)

.NET 10 API template'i ve referans uygulaması. Kütüphane seçimlerinde tek ölçüt var: işi **derleme zamanında** yapmak — kaynak üretimi kullanan, açılışta reflection taraması yapmayan araçlar.

İki şekilde kullanılır: yeni bir servise sıfırdan başlamak, ya da tek tek yapıları söküp mevcut projelere taşımak.

```bash
cp .env.example .env
docker compose up --build
```

→ **http://localhost:5000/scalar**

```bash
dotnet new install ./MachTen
dotnet new machten -n KendiServisim
```

## Ne var

| | |
|---|---|
| **HTTP** | FastEndpoints + FluentValidation, `api/v1/...` versiyonlama, Scalar |
| **Veri** | Dapper.AOT + SQL Server (EF yalnızca migration üretiyor), Mapperly ile eşleme |
| **Cache** | HybridCache (L1 bellek + L2 Garnet), MemoryPack ile ikili serileştirme |
| **Auth** | Kendi token üretimi (RS256) + JWT Bearer doğrulama, PBKDF2 şifre hash'i |
| **Mesajlaşma** | Wolverine, SQL Server üzerinde transactional outbox |
| **Observability** | OpenTelemetry → Prometheus, ZLogger (JSON), correlation ID |
| **Test** | TUnit, Testcontainers, NetArchTest — 36 test |

## Mimari

Clean Architecture + vertical slice. Bağımlılıklar içe akar: `Domain` hiçbir şeye bağımlı değil, `Infrastructure` `Application`'ın arayüzlerini implemente eder, `Api` yalnızca bağlar.

```
src/Domain          → Entity, value object, domain event (framework'süz)
src/Application     → Command/Query + Handler, mapping, arayüzler
src/Infrastructure  → EF Core, cache, Identity
src/Api             → Endpoint'ler, Program.cs
```

Yeni özellik eklerken `Features/<Ad>/` desenini koru. Referans dilim `Features/Orders` — validasyon, domain kuralı, cache ve outbox bir arada.

**Yapı taşımak için** ilgili klasörü kopyala; bloklar birbirine bağımlı değil. Cache için örnek: `Application/Contracts/ICacheStore.cs` + `Contracts/CacheKeys.cs` + `Infrastructure/Caching/`.

## Test

```bash
dotnet run --project tests/MACHTEN.Domain.Tests          # hızlı
dotnet run --project tests/MACHTEN.ArchitectureTests     # katman kuralları
dotnet run --project tests/MACHTEN.Api.IntegrationTests  # Docker gerekir
```

TUnit her projeyi kendi çalıştırılabiliri olarak derler, o yüzden `dotnet test` değil `dotnet run`.

Testler davranışı doğrular, varlığı değil: cache testi satırı uygulamanın arkasından değiştirip bayat değerin döndüğünü kontrol eder, outbox testi mesajın gerçekten teslim edilmesini bekler, mimari testler Domain'e EF Core eklenirse kırılır.

## Ölçümler

`dotnet run -c Release --project benchmarks/MACHTEN.Benchmarks -- --filter *`

| | Kaynak-üretimli | Reflection |
|---|---|---|
| Eşleme (Mapperly) | **66 ns / 160 B** | 410 ns / 800 B |
| JSON (System.Text.Json) | **185 ns / 424 B** | 234 ns / 424 B |

JSON farkı mütevazı ve allocation eşit — source generation'ın asıl gerekçesi throughput değil, trimming ve AOT'u mümkün kılması.

## Bilinen sınırlar

- **Native AOT kapalı** — geriye tek engel kaldı: EF Core hâlâ şemayı ve migration'ları üretiyor. Ölçüm sonuçları [NOTES.md](NOTES.md)'de.
- **Refresh token ve OIDC discovery yok** — OpenIddict çıkarıldığında bilinçli olarak bırakıldı; token'lar 1 saatlik ve yenilenmiyor.
- **Harici broker yok** — outbox dayanıklı yerel kuyruğa teslim ediyor; Kafka/RabbitMQ/NATS eklemek tek satırlık transport değişikliği.
- **FastEndpoints** 2026'da bakım moduna geçebileceğini duyurdu; uzun ömür kritikse Minimal API + RDG.
- `appsettings.json`'daki anahtarlar ve demo şifreler yalnızca geliştirme içindir.

---

📓 **[NOTES.md](NOTES.md)** — kurarken çarptığım tuzaklar. Neredeyse hepsi hata vermeden yanlış çalışan şeyler.

🤝 **[CONTRIBUTING.md](CONTRIBUTING.md)**
