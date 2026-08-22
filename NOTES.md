# Tuzaklar

Bu projeyi kurarken çarptığım ve hiçbiri kütüphane dokümanından anlaşılmayan şeyler. Neredeyse hepsinin ortak yanı: **hata vermeden yanlış çalışmak.**

## Auth

**OpenIddict simetrik imzalama anahtarını reddeder.** JWT üretmek için asimetrik anahtar şart; `SymmetricSecurityKey` ile açılışta `At least one asymmetric signing key must be registered` alırsın.

**`AddIdentity` challenge şemasını cookie'ye çevirir.** Token'sız istek 401 yerine olmayan bir login sayfasına yönlenip **404** döner. `AddAuthentication` içinde `DefaultScheme`, `DefaultAuthenticateScheme` ve `DefaultChallengeScheme`'in üçünü birden JWT Bearer'a sabitle.

**JWT Bearer claim adlarını yeniden yazar.** `sub` ve `name` eski WS-Federation URI'lerine map'lenir, `User.FindFirst("sub")` boş döner. `MapInboundClaims = false`.

**Access token şifreliyse JWT Bearer okuyamaz.** `DisableAccessTokenEncryption()` gerekir; refresh token'lar şifreli kalır.

## Cache

**Serializer sessizce JSON'a düşer.** Bir tipi sahiplenen serializer yoksa HybridCache kendi reflection tabanlı JSON'ına döner ve hiçbir şey hata vermez — "cache çalışıyor" testleri iki durumda da geçer. Önce `IHybridCacheSerializerFactory` yazdım; `MemoryPackFormatterProvider.IsRegistered<T>()` tip ilk kez serileştirilene kadar `false` döndüğü için factory her tipi reddetti. Serializer'lar artık `Program.cs`'de tip tip açıkça kayıtlı.

**Cache anahtarları tek yerde durmalı.** Yazma yolunun invalidate ettiği anahtarla okuma yolunun ürettiği anahtar farklı yazılırsa hata sessizdir: cache temizlenmez, veri bayat kalır, test patlamaz.

## Outbox

**Event kaydetmek yayınlamak değildir.** `Order.Place()` `OrderPlaced`'ı aggregate'e kaydediyordu, handler onu düşürüyordu. Outbox yapılandırması uzun süre dekoratif kaldı ve fark edilmedi — çünkü yayın yaptığı broker da hiç ayağa kalkmamıştı.

**Wolverine 6 çekirdeği Roslyn'siz gelir.** `TypeLoadMode.Dynamic` varsayılan ama derleyici yok; uygulama açılışta çöker. `WolverineFx.RuntimeCompilation` eklenecek ya da `codegen write` + `TypeLoadMode.Static` (AOT için ikincisi şart).

**Wolverine yalnızca giriş assembly'sini tarar.** Handler'lar başka projedeyse `Discovery.IncludeAssembly` gerekir, yoksa "found no handlers" deyip her istek 500 döner.

## EF Core

**İki kütüphane aynı anda `IModelCustomizer` kurarsa biri diğerini ezer.** Kendi customizer'ını kuran bir kütüphane OpenIddict'inkini devre dışı bıraktı ve OpenIddict'in dört tablosu migration'dan sessizce düştü. Bu yüzden OpenIddict entity'leri `OnModelCreating` içinde açıkça kaydediliyor.

## Test

**Geçen test, ihlali yakalayabildiği anlamına gelmez.** Mimari testleri kasten kırdım: katmandan katmana kurallar zaten yapısal olarak ihlal edilemiyor (proje grafiği döngüsel olur, build daha erken patlar). Asıl ağırlığı taşıyanlar paket kuralları — Domain'e EF Core eklemeyi hiçbir şey engellemiyor.

**Paralellik test izolasyonunu bozar.** TUnit testleri paralel koşar; outbox testinin izleme oturumu diğer testlerin mesajlarını da gördü ve `SingleMessage` 103 mesaj buldu. Çözüm testi sıralamak değil, her testin kendi verisini benzersiz üretmesi.

**`depends_on` hazır olmayı beklemez.** SQL Server ~30 sn'de bağlantı kabul ediyor; healthcheck + `condition: service_healthy` olmadan API açılışta ölüyor.

## Diğer

**Kullanıcıya giden formatlama `InvariantCulture` olmalı.** `Money.Formatted` makine culture'ını kullanıyordu; Türkçe locale'de API `19,99 TRY` dönüyordu.

**UUIDv7 sıralaması yalnızca milisaniyeler arasında garanti.** Aynı milisaniyede üretilen iki id'nin sırası garanti değil, ayrıca sıralama byte düzeyinde — `Guid.CompareTo` bunu göstermez.

**ZLogger'da `IncludeScopes` tek başına yetmez.** Düz metin formatlayıcı scope değerlerini basmıyor; correlation id yanıt başlığında görünüp loglarda hiç çıkmıyordu. JSON formatlayıcı gerekiyor.

**FastEndpoints versiyonlu endpoint'leri dokümandan filtreler.** `Version(n)` kullanıyorsan `SwaggerDocument`'ta `MaxEndpointVersion` yoksa doküman şemalarla dolu ama **sıfır endpoint**'le üretilir.

## Native AOT araştırması

Gerçek AOT binary'si üretilerek ölçüldü (boş API 15 MB, tüm yığınla 29 MB; çalıştı). Hiç `MissingMetadata`/`TypeLoad` hatası görülmedi.

| Bileşen | Durum |
|---|---|
| Dapper.AOT, JWT Bearer, OpenTelemetry, Mapperly, Redis | ✅ Temiz |
| HybridCache, Microsoft.Data.SqlClient | ✅ Çalışıyor (uyarılar kullanılmayan opsiyonel yollarda) |
| **EF Core** | ❌ Tek sert engel — "highly experimental, not suited for production" |

**Dapper.AOT üç şey olmadan sessizce devre dışı kalır:** `Dapper` + `Dapper.AOT` paketleri birlikte, using'lerden *sonra* `[module: DapperAot]`, ve `<InterceptorsNamespaces>$(InterceptorsNamespaces);Dapper.AOT</InterceptorsNamespaces>`. Eksikse `DAP005` verir ve reflection yolu çalışmaya devam eder.

**AOT altında anonim tipler serialize edilemez.** `new { ok = true }` döndüren endpoint 500 verir; her yanıt tipi `JsonSerializerContext`'e kayıtlı somut bir tip olmalı.
