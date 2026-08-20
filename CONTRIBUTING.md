# Katkı Rehberi

## Geliştirme akışı

1. `main`'den bir feature branch açın: `feature/<kısa-açıklama>`
2. Değişikliği yapın, ilgili katmanda test ekleyin (bkz. aşağıdaki test piramidi)
3. Yerelde doğrulayın:
   ```bash
   dotnet build MACHTEN.sln
   dotnet test MACHTEN.sln
   ```
4. PR açın — CI (`.github/workflows/ci.yml`) build + test'i otomatik çalıştırır

## Test piramidi

| Katman | Proje | Ne test edilir | Docker gerekir mi |
|---|---|---|---|
| Domain | `tests/MACHTEN.Domain.Tests` | Value object'ler, entity kuralları | Hayır |
| Application | `tests/MACHTEN.Application.Tests` | Handler, validator mantığı | Hayır |
| API | `tests/MACHTEN.Api.IntegrationTests` | Endpoint'ler uçtan uca (Testcontainers ile gerçek SQL Server + Redis) | Evet |

Yeni bir feature eklerken en azından Domain/Application seviyesinde birim test bekleniyor; endpoint'i dışa açıyorsanız bir integration test de ekleyin.

## Kod stili

`.editorconfig` içindeki kurallara (file-scoped namespace, performans analyzer'ları vb.) uyun — çoğu `dotnet format` ile otomatik düzeltilebilir.

## Yeni bir feature eklerken

`Features/<ÖzellikAdı>/` klasör deseni 4 katmanda da korunmalı:

```
MACHTEN.Domain/…              (varsa yeni entity/value object)
MACHTEN.Application/Features/<Ad>/  (Command veya Query + Handler + Response)
MACHTEN.Api/Features/<Ad>/          (Endpoint + Validator)
```

`Features/Money` klasörü referans alınabilir: validasyon, domain value object ve Mapperly ile eşleme bir arada.
