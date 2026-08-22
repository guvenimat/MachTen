# MachTen

[![CI](https://github.com/guvenimat/MachTen/actions/workflows/ci.yml/badge.svg)](https://github.com/guvenimat/MachTen/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)

.NET 10 üzerinde kurulmuş, performans odaklı bir API template'i ve referans uygulaması.

## Bu repo iki işe yarar

**1. Yeni bir servise sıfırdan başlamak.** `dotnet new` template'i olarak kurulur, isimleri kendi projene göre yeniden yazar:

```bash
dotnet new install ./MachTen
dotnet new machten -n KendiServisim
```

**2. Tek tek yapıları başka projelere taşımak.** Asıl tasarım hedefi bu. Her altyapı parçası — cache, arka plan işleri, auth, outbox, observability — **kendi başına sökülüp taşınabilecek** şekilde ayrılmış ve aşağıdaki [Yapı kataloğu](#yapı-kataloğu)'nda dosya yollarıyla, tercih gerekçesiyle ve karşılaştığım tuzaklarla belgelenmiş.

> Mevcut bir projene bir yapı eklemek istiyorsan doğrudan kataloğa git, ilgili bloğun dosyalarını kopyala. Bloklar birbirine bağımlı değildir.

Kütüphane seçimlerinde ortak ölçüt, işi **derleme zamanında** yapabilmek: kaynak üretimi kullanan, açılışta reflection taraması yapmayan ve çalışma zamanında kod üretmeyen araçlar tercih edildi.

---

## Hızlı başlangıç

```bash
cp .env.example .env
docker compose up --build
```

Ayağa kalkınca: **http://localhost:5000/scalar**

```bash
# Token al, korumalı endpoint'i çağır
curl -X POST http://localhost:5000/connect/token \
  -d "grant_type=client_credentials&client_id=machten-sample-client&client_secret=machten-sample-secret&scope=api"

curl http://localhost:5000/api/v1/me -H "Authorization: Bearer <access_token>"

# Sipariş oluştur ve oku (ikinci okuma cache'ten gelir)
curl -X POST http://localhost:5000/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{"customerReference":"acme-42","amount":19.99,"currency":"try"}'
```

---

## Yapı kataloğu

Her blok bağımsızdır. "Şu yapıyı şu projeme uygula" demek için ihtiyacın olan her şey burada.

### 🗄️ İki katmanlı cache (L1 + L2)

Süreç içi bellek + dağıtık cache'i tek arayüzün arkasında birleştirir. Okuma önce RAM'e, sonra Garnet'e, en son veritabanına gider.

| | |
|---|---|
| **Dosyalar** | `Application/Contracts/ICacheStore.cs`, `Application/Contracts/CacheKeys.cs`, `Infrastructure/Caching/CacheStore.cs` |
| **Kullanım örneği** | `Application/Features/Orders/GetOrder/GetOrderHandler.cs` |
| **Paketler** | `Microsoft.Extensions.Caching.Hybrid`, `Microsoft.Extensions.Caching.StackExchangeRedis` |
| **Altyapı** | Garnet (Redis protokolü uyumlu) — `docker-compose.yml` |

**Neden Garnet:** Microsoft'un Redis protokolüyle uyumlu sunucusu; istemci kütüphaneleri aynen çalışır, tek satır kod değişmeden Redis'le yer değiştirebilir.

**Neden `ICacheStore` soyutlaması:** `HybridCache` doğrudan Application katmanında kullanılsaydı, use case'ler bir altyapı tipine bağlanırdı. Arayüz sayesinde Application katmanı cache'in nerede olduğunu bilmez.

> **Tuzak — cache anahtarları tek yerde.** `CacheKeys.cs` var çünkü yazma yolunun invalidate ettiği anahtarla okuma yolunun ürettiği anahtar farklı yazılırsa, hata sessizdir: cache asla temizlenmez, veri bayat kalır ve hiçbir test patlamaz.

> **Tuzak — `GetOrCreateAsync`'e state'i açıkça geçir.** Handler'daki factory `static` lambda; closure yakalamadığı için çağrı başına allocation yapmaz. Closure kullanırsan her istekte bir nesne ayrılır.

---

### 🔐 Auth: OpenIddict + Identity + JWT Bearer

Token üreten yetkilendirme sunucusu (`client_credentials`, `password`, `refresh_token`) ve token doğrulayan resource server, tek uygulamada.

| | |
|---|---|
| **Dosyalar** | `Api/Features/Auth/TokenEndpoint.cs`, `Api/Features/Auth/MeEndpoint.cs`, `Api/Infrastructure/Auth/AuthSeeder.cs`, `Infrastructure/Identity/ApplicationUser.cs` |
| **Yapılandırma** | `Api/Program.cs` → Identity / OpenIddict / JWT Bearer blokları |
| **Paketler** | `OpenIddict.AspNetCore`, `OpenIddict.EntityFrameworkCore`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Authentication.JwtBearer` |

**Neden OpenIddict:** Standartlara uygun tam bir OAuth2/OIDC sunucusu; kendi kullanıcı deponla ve ASP.NET Core Identity ile birlikte çalışıyor.

Bu blok en çok tuzak barındıran yer — dördü de gerçekten başıma geldi ve testler yakaladı:

> **Tuzak 1 — OpenIddict simetrik imzalama anahtarını reddeder.** JWT üretmek için **asimetrik** anahtar şart. `SymmetricSecurityKey` ile açılışta `InvalidOperationException: At least one asymmetric signing key must be registered` alırsın. Çözüm: RSA anahtarı (`Program.cs` içinde config'ten okunuyor).

> **Tuzak 2 — `AddIdentity` challenge şemasını cookie'ye çevirir.** Sonuç: token'sız istek 401 yerine, olmayan bir login sayfasına yönlenip **404** döner. Sessiz ve kafa karıştırıcı. Çözüm: `AddAuthentication` içinde `DefaultScheme`, `DefaultAuthenticateScheme` ve `DefaultChallengeScheme`'in üçünü birden JWT Bearer'a sabitle.

> **Tuzak 3 — JWT Bearer claim adlarını yeniden yazar.** `sub` ve `name`, eski WS-Federation URI'lerine map'lenir; `User.FindFirst("sub")` boş döner. Çözüm: `opts.MapInboundClaims = false`.

> **Tuzak 4 — access token şifreliyse JWT Bearer okuyamaz.** `DisableAccessTokenEncryption()` gerekir; refresh token'lar şifreli kalır.

**Anahtar yönetimi:** İmzalama anahtarı config'ten geliyor, development sertifikasından değil. Böylece her instance aynı materyalle imzalar ve doğrulama metadata discovery turu gerektirmez — bu aynı zamanda `WebApplicationFactory` altındaki testleri çalışır kılan şey.

⚠️ `appsettings.json`'daki anahtarlar ve demo şifreler **yalnızca geliştirme içindir.** Üretimde `Auth__SigningKey` / `Auth__EncryptionKey` ortam değişkeniyle ezilmeli ve `AuthSeeder` devre dışı bırakılmalı.

---

### 📤 Transactional outbox → Kafka

Domain event'i, onu doğuran veritabanı işlemiyle **aynı transaction içinde** yazar. Sipariş geri alınırsa mesaj asla çıkmaz; commit olduysa mesaj kaybolmaz.

| | |
|---|---|
| **Dosyalar** | `Domain/Events/IDomainEvent.cs`, `Domain/Events/OrderPlaced.cs`, `Domain/Entities/Order.cs` (event kaydı), `Api/Program.cs` → Wolverine bloğu |
| **Paketler** | `WolverineFx`, `WolverineFx.SqlServer`, `WolverineFx.EntityFrameworkCore`, `WolverineFx.Kafka` |

**Neden Wolverine:** Handler'ları derleme zamanında bağlıyor ve dayanıklı outbox'ı kutudan çıkıyor — ayrı bir mesajlaşma altyapısı kurmaya gerek kalmıyor.

**Neden outbox:** "Kaydet, sonra mesaj yayınla" yaklaşımında iki adım arasında süreç ölürse mesaj kaybolur; sıra ters çevrilirse geri alınan bir işlem için mesaj yayınlanır. Outbox ikisini tek transaction'a alır.

> **Tuzak — Wolverine 6 gerekiyor.** Kafka ve EF Core outbox paketleri yalnızca 6.x'te var. Ayrıca 6.0'da Roslyn bağımlılığı çekirdekten çıkarıldı, bu da AOT yolunu açıyor (`codegen write` + `TypeLoadMode.Static`).

---

### 🧭 Observability: OpenTelemetry → Prometheus → Grafana

| | |
|---|---|
| **Dosyalar** | `Api/Program.cs` → OpenTelemetry bloğu, `Api/Infrastructure/Observability/CorrelationIdMiddleware.cs`, `Api/Infrastructure/Logging/LoggerMessageDefinitions.cs` |
| **Altyapı** | `infra/prometheus/prometheus.yml`, `infra/grafana/provisioning/` (datasource + hazır dashboard) |

Dashboard hazır geliyor: throughput, p50/p95/p99 gecikme, hata oranı, status kodu dağılımı, runtime bellek.

**Correlation ID:** Pipeline'ın en başında çalışır, gelen `X-Correlation-ID` başlığını korur, yoksa trace id'den üretir ve logging scope'una koyar — böylece bir isteğin tüm log satırları birbirine bağlanır.

**Logging:** `LoggerMessageDefinitions.cs` source-generated logging kullanır (`[LoggerMessage]`). Interpolated string ile loglamaya göre allocation yapmaz ve log seviyesi kapalıyken hiç çalışmaz.

> **Tuzak — panel eklemek metrik toplamak demek değil.** Grafana'ya bellek paneli koydum ama runtime metriklerini toplayan bir şey yoktu; panel sonsuza kadar boş kalacaktı. `OpenTelemetry.Instrumentation.Runtime` eklenmesi gerekti.

> **Tuzak — provisioned dashboard datasource'a uid ile bağlanır.** `datasource.yml`'de sabit `uid: prometheus` yoksa dashboard'lar bağlanacak bir şey bulamaz ve paneller "datasource not found" der.

---

### 🧱 Domain modelleme: value object + domain event

| | |
|---|---|
| **Dosyalar** | `Domain/ValueObjects/Money.cs`, `Domain/Entities/Order.cs`, `Domain/Exceptions/` |
| **Kalıcılık eşlemesi** | `Infrastructure/Persistence/Configurations/OrderConfiguration.cs` |

`Money` kendi kurallarını kendi korur (negatif olamaz, 3 harfli ISO para birimi, otomatik büyük harfe çevirir). `ComplexProperty` ile `Order` satırına düzleşir — ayrı tablo yok, türetilmiş `Formatted` alanı yazılmaz.

> **Tuzak — kullanıcıya/tele giden formatlama `InvariantCulture` olmalı.** `Money.Formatted` başta makinenin culture'ını kullanıyordu; Türkçe locale'de API `19,99 TRY` döndürüyordu. Yani yanıt sunucunun bulunduğu makineye göre değişiyordu. Testin yakaladığı gerçek bir hataydı.

> **Tuzak — UUIDv7 sıralaması yalnızca milisaniyeler arasında garanti.** `Guid.CreateVersion7()` clustered index'e sıralı yazım için kullanılıyor, ama aynı milisaniyede üretilen iki id'nin sırası garanti değil. Ayrıca sıralama **byte düzeyinde**; `Guid.CompareTo` .NET'in kendi alan sırasını kullandığı için bunu göstermez.

---

### ✅ Test stratejisi

| Katman | Proje | Kapsam | Docker |
|---|---|---|---|
| Domain | `MACHTEN.Domain.Tests` | Value object ve entity kuralları (16 test) | Hayır |
| Application | `MACHTEN.Application.Tests` | Mapping (1 test) | Hayır |
| Mimari | `MACHTEN.ArchitectureTests` | Katman kuralları (5 test) | Hayır |
| Entegrasyon | `MACHTEN.Api.IntegrationTests` | Uçtan uca, gerçek SQL Server + Redis (9 test) | Evet |

**Mimari testler** (`NetArchTest`) Clean Architecture iddiasını derleme zamanında zorunlu kılar: Domain başka katmana veya EF Core/ASP.NET Core'a bağımlı olamaz.

> **Tuzak — geçen test, ihlali yakalayabildiği anlamına gelmez.** Testleri kasten kırdım (Domain'e EF Core ekleyip `DbContext` kullandım) ve iki şey öğrendim: katmandan katmana kurallar zaten **yapısal olarak ihlal edilemez** (proje grafiği döngüsel olur, build daha erken patlar); asıl ağırlığı taşıyanlar **paket kuralları** — Domain'e EF Core eklemeyi hiçbir şey engellemiyor ve o testler bunu gerçekten yakalıyor.

**Cache testi**, satırı uygulamanın arkasından `UPDATE` edip endpoint'in hâlâ eski değeri döndürdüğünü doğrular. Yalnızca 200 kontrol eden bir test, cache tamamen kapalıyken de geçerdi.

---

## Mimari

Katmanlı (Clean Architecture) + özellik bazlı (vertical slice) hibrit. Bağımlılıklar içe doğru akar: `Domain` hiçbir şeye bağımlı değil, `Application` yalnızca `Domain`'i tanır. `Infrastructure`, `Application`'daki arayüzleri implemente eder — bağımlılık oku dışarıdan içeri döner. `Api` iş mantığı barındırmaz; katmanları bağlar ve HTTP yüzeyini açar.

```
src/
  MACHTEN.Domain          → Entity, value object, domain event (framework'süz)
  MACHTEN.Application     → Command/Query + Handler, mapping, arayüzler
  MACHTEN.Infrastructure  → EF Core, cache, Identity
  MACHTEN.Api             → Endpoint'ler, arka plan işleri, Program.cs
tests/                    → Domain / Application / Mimari / Entegrasyon
benchmarks/               → BenchmarkDotNet
```

Yeni özellik eklerken `Features/<Ad>/` desenini koru. Referans: `Features/Orders` (validasyon + domain + cache + outbox bir arada).

---

## Ölçümler

`dotnet run -c Release --project benchmarks/MACHTEN.Benchmarks -- --filter *`

Ryzen 7 7800X3D, .NET 10, ShortRun:

| | Kaynak-üretimli | Reflection | Fark |
|---|---|---|---|
| Nesne eşleme (Mapperly) | **66 ns / 160 B** | 410 ns / 800 B | 6.18× hızlı, 5× az allocation |
| JSON serileştirme (STJ) | **185 ns / 424 B** | 234 ns / 424 B | 1.26× hızlı, allocation aynı |

JSON sonucunu abartmıyorum: fark mütevazı ve allocation eşit. Source generation'ın asıl gerekçesi trimming ve AOT'u mümkün kılması, throughput zaferi değil.

> ShortRun (3 iterasyon) ile ölçüldü, hata payları geniş. Kesin sayı için `--job default` ile çalıştır.

---

## 🔬 Native AOT — araştırma sonuçları

**Henüz uygulanmadı.** Aşağıdakiler gerçek AOT binary'si üretilerek ölçüldü; göç planlanıyor.

Boş bir API 15 MB, tüm yığın eklenince 29 MB native binary olarak derlendi ve çalıştı.

| Bileşen | Durum |
|---|---|
| Dapper.AOT | ✅ 0 uyarı — interception reflection yolunu tamamen siliyor |
| JWT Bearer, OpenTelemetry, Mapperly, StackExchange.Redis | ✅ Temiz |
| HybridCache | ✅ Çalışıyor (kaynak-üretimli serializer ile) |
| Microsoft.Data.SqlClient | ✅ Çalışıyor — uyarılar kullanmadığımız opsiyonel yollarda |
| **EF Core** | ❌ **Tek sert engel** — resmî olarak "highly experimental, not suited for production" |

Çalışan AOT binary'de **hiç** `MissingMetadata` / `TypeLoad` / `PlatformNotSupported` hatası görülmedi. SqlClient normal bir `SqlException` üretti (sunucu yoktu), yani sürücü AOT altında sağlam.

> **Tuzak — Dapper.AOT üç şey olmadan sessizce devre dışı kalır:** `Dapper` + `Dapper.AOT` paketleri birlikte, using'lerden *sonra* `[module: DapperAot]`, ve `<InterceptorsNamespaces>$(InterceptorsNamespaces);Dapper.AOT</InterceptorsNamespaces>`. Eksikse `DAP005` uyarısı verir ve reflection yolu sessizce çalışmaya devam eder.

> **Tuzak — AOT altında anonim tipler serialize edilemez.** `new { ok = true }` döndüren endpoint 500 verir. Her yanıt tipi `JsonSerializerContext`'e kayıtlı somut bir tip olmalı.

> **Düzeltme:** Bu README daha önce "FastEndpoints reflection yüzünden AOT'ta çalışmıyor" diyordu. Çökme gerçekti ama teşhis eksikti — sebep `FastEndpoints.Generator` paketinin kurulu olmamasıydı, kütüphanenin doğasında bir kısıt değil.

---

## Veritabanı

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add <Ad> --project src/MACHTEN.Infrastructure --startup-project src/MACHTEN.Api --context MachtenDbContext -o Persistence/Migrations
```

Development'ta `AuthSeeder` açılışta migration'ları kendisi uygular.

> **Tuzak — iki kütüphane aynı anda `IModelCustomizer` kurarsa biri diğerini ezer.** Bu projede gerçekten yaşandı: kendi customizer'ını kuran bir kütüphane OpenIddict'inkini devre dışı bıraktı ve OpenIddict'in **dört tablosu migration'dan sessizce düştü** — hata yok, sadece eksik tablo. Bu yüzden OpenIddict entity'leri `DbContextOptions.UseOpenIddict()` yerine `OnModelCreating` içinde açıkça kaydediliyor (`MachtenDbContext.cs`).

---

## Bilinen sınırlar

- **Native AOT kapalı** — EF Core engeli yüzünden; yukarıdaki araştırma bölümüne bak.
- **Kafka broker'ı compose'da tanımlı** ve outbox ona yayın yapıyor; tüketici tarafı yazılmadı.
- **FastEndpoints** 2026'da "yalnızca hata düzeltme" moduna geçebileceğini duyurdu. Uzun ömür kritikse Minimal API + RDG'ye geçiş düşünülmeli.
- `/health` SQL Server'ı `master` üzerinden kontrol eder; uygulama veritabanı henüz oluşmamışsa bu kontrolü etkilemez.

## Katkı

Bkz. [CONTRIBUTING.md](CONTRIBUTING.md).

## Lisans

[MIT](LICENSE)
