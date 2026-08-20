# MachTen

[![CI](https://github.com/guvenimat/MachTen/actions/workflows/ci.yml/badge.svg)](https://github.com/guvenimat/MachTen/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)

Tamamen ücretsiz ve açık kaynak kütüphanelerden kurulmuş, performans odaklı bir **.NET 10 API starter template**'i. Amaç, ücretli lisans gerektirmeyen (Redis yerine Garnet, MediatR yerine Wolverine, IdentityServer yerine OpenIddict gibi) en hızlı araçları Clean Architecture + Vertical Slice yapısında bir araya getirip yeni servisler için sağlam bir başlangıç noktası oluşturmak.

## Template olarak kullanım

Bu repo `dotnet new` template'i olarak kurulabilir — proje ve namespace isimleri otomatik değişir:

```bash
dotnet new install ./MachTen
dotnet new machten -n MachTen
```

Verdiğiniz isim her yere uygulanır: `MachTen.sln`, `src/MachTen.Api`, `MachTenDbContext` … Üretilen çözüm doğrudan derlenir ve testleri geçer.

## Mimari

Katmanlı (Clean Architecture) + özellik bazlı (vertical slice) hibrit yapı. Bağımlılıklar daima içe doğru akar: `Domain` hiçbir şeye bağımlı değildir, `Application` yalnızca `Domain`'i tanır. `Infrastructure`, `Application`'daki arayüzleri (`ICacheStore`, `IApplicationDbContext`) implemente eder — yani bağımlılık oku dışarıdan içeri döner (dependency inversion). `Api` ise iş mantığı barındırmaz; yalnızca composition root olarak katmanları birbirine bağlar ve HTTP yüzeyini açar.

```
src/
  MACHTEN.Domain          → Entity, value object, domain kuralları (framework'ten bağımsız)
  MACHTEN.Application     → Use case'ler (Command/Query + Handler), DTO, mapping, arayüzler
  MACHTEN.Infrastructure  → Arayüzlerin implementasyonu: EF Core, cache, Identity
  MACHTEN.Api             → FastEndpoints endpoint'leri, arka plan işleri, Program.cs
tests/
  MACHTEN.Domain.Tests           → Domain birim testleri
  MACHTEN.Application.Tests      → Handler/mapping birim testleri
  MACHTEN.Api.IntegrationTests   → Testcontainers (gerçek SQL Server + Redis) ile uçtan uca testler
```

Yeni bir özellik eklerken ilgili katmanlarda `Features/<ÖzellikAdı>/` klasör desenini koruyun. Örnekler: `Features/Ping` (en basit hâli), `Features/Money` (validasyon + domain value object + mapping), `Features/Auth` (korumalı endpoint). Detaylar için [CONTRIBUTING.md](CONTRIBUTING.md).

## Kullanılan kütüphaneler ve neden

| Amaç | Kütüphane | Neden |
|---|---|---|
| HTTP endpoint'leri | [FastEndpoints](https://fast-endpoints.com/) | Controller/MVC'ye göre çok daha düşük overhead, REPR pattern |
| İstek validasyonu | FluentValidation (FastEndpoints entegrasyonu) | Endpoint'e otomatik bağlanan, test edilebilir validator'lar |
| Mediator / mesajlaşma | [WolverineFx](https://wolverinefx.net/) | Kaynak üretimli, MediatR'dan hızlı — ve MediatR'ın aksine ücretsiz |
| Nesne eşleme | [Mapperly](https://mapperly.riok.app/) | Derleme zamanında kod üretir; reflection yok, AutoMapper'ın aksine ücretsiz |
| Veritabanı | EF Core + SQL Server | Connection pooling (`AddDbContextPool`) ile optimize |
| Cache (L1+L2) | `Caching.Hybrid` + [Garnet](https://microsoft.github.io/garnet/) | Garnet, Redis protokolüyle uyumlu MIT lisanslı alternatif; Redis 2024'te açık kaynaklıktan çıktı |
| Kimlik doğrulama | [OpenIddict](https://documentation.openiddict.com/) + ASP.NET Core Identity + JWT Bearer | OpenIddict sınırsız ücretsiz; Duende IdentityServer gelir eşiği üstünde paralı |
| Arka plan işleri | [TickerQ](https://tickerq.net/) | Source generator ile kayıt, EF Core kalıcılık; Hangfire'ın ücretsiz katmanı sınırlı |
| Observability | OpenTelemetry → Prometheus + Grafana | Açık standart, vendor lock-in yok |
| Health checks | AspNetCore.HealthChecks | `/health` üzerinden SQL Server ve cache durumu |
| Hızlı JSON | `System.Text.Json` source-generator | Reflection'sız serileştirme, `PublishReadyToRun` + `TieredPGO` ile hızlı soğuk başlangıç |
| Test | xUnit + Testcontainers | Gerçek bağımlılıklara karşı izole, tekrarlanabilir testler |
| CI | GitHub Actions | Her push/PR'da build, güvenlik taraması, test ve Docker image doğrulaması |

## Hızlı başlangıç

```bash
cp .env.example .env
docker compose up --build
```

- API: http://localhost:5000/api/v1/ping
- Swagger: http://localhost:5000/swagger
- Health check: http://localhost:5000/health
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin / `.env`'deki `GRAFANA_ADMIN_PASSWORD`)

`.env` git'e dahil değildir; gerçek şifreleri asla commit etmeyin.

## Kimlik doğrulama

`/connect/token` OpenIddict tarafından sunulur; `client_credentials`, `password` ve `refresh_token` akışlarını destekler. Development ortamında demo bir client ve kullanıcı otomatik oluşturulur (`appsettings.json` → `Auth:Seed`).

```bash
# Token al
curl -X POST http://localhost:5000/connect/token \
  -d "grant_type=client_credentials&client_id=machten-sample-client&client_secret=machten-sample-secret&scope=api"

# Korumalı endpoint'i çağır
curl http://localhost:5000/api/v1/me -H "Authorization: Bearer <access_token>"
```

Erişim token'ları RSA ile imzalanır ve şifrelenmez, böylece JWT Bearer handler'ı doğrulayabilir; refresh token'lar şifreli kalır. **Production'a çıkmadan önce** `Auth__SigningKey` ve `Auth__EncryptionKey` değerlerini ortam değişkeniyle geçersiz kılın ve `AuthSeeder`'ı devre dışı bırakın.

## Yerelde çalıştırma (Docker olmadan)

SQL Server ve Garnet/Redis'in ayrıca ayakta olması gerekir (örn. `docker compose up sqlserver garnet`).

```bash
dotnet run --project src/MACHTEN.Api
```

## Veritabanı migration'ları

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add <MigrationAdi> --project src/MACHTEN.Infrastructure --startup-project src/MACHTEN.Api --context MachtenDbContext -o Persistence/Migrations
dotnet ef database update --project src/MACHTEN.Infrastructure --startup-project src/MACHTEN.Api
```

`InitialCreate` migration'ı Identity, OpenIddict ve TickerQ şemalarını oluşturur. Development'ta `AuthSeeder` açılışta migration'ları kendisi uygular.

## Test

```bash
dotnet test tests/MACHTEN.Domain.Tests           # hızlı, izole
dotnet test tests/MACHTEN.Application.Tests      # hızlı, izole
dotnet test tests/MACHTEN.Api.IntegrationTests   # Docker gerektirir (Testcontainers)
```

Entegrasyon testleri gerçek SQL Server ve Redis container'ları başlatır, migration'ları uygular ve token alma dahil tüm akışı uçtan uca doğrular.

## API versiyonlama

FastEndpoints'in yerleşik versiyonlama desteği kullanılıyor. Yeni bir endpoint eklerken `Configure()` içinde `Version(1)` çağırın; route otomatik olarak `api/v{version}/...` şeklinde oluşur.

## Notlar / bilinen kısıtlar

- **Native AOT (`PublishAot`) kasıtlı olarak kapalı.** FastEndpoints endpoint keşfini reflection ile yapıyor; ILC trimmer bu sınıfları "ölü kod" sayıp siliyor ve uygulama `FastEndpoints was unable to find any endpoint declarations!` hatasıyla açılışta çöküyor (Docker'da doğrulandı). Bunun yerine `PublishReadyToRun` + `TieredPGO` tercih edildi.
- OpenIddict entity'leri `DbContextOptions.UseOpenIddict()` yerine `OnModelCreating` içinde kaydediliyor: TickerQ kendi `IModelCustomizer`'ını kuruyor ve aksi hâlde OpenIddict'in tablolarını migration'dan sessizce düşürüyor.
- `WolverineFx` 5.16.2'de sabit tutuldu (6.x mevcut). Yükseltmeden önce breaking change notlarını kontrol edin.
- `docker-compose.yml` bir Kafka broker'ı da ayağa kaldırıyor ancak henüz hiçbir kod ona bağlanmıyor — event-driven bir özellik eklemeyi düşünmüyorsanız bu servisi silebilirsiniz.
- `/health`, SQL Server'ı `master` üzerinden kontrol eder; uygulama veritabanı henüz oluşmamışsa bu kontrolü etkilemez.

## Lisans

[MIT](LICENSE)
