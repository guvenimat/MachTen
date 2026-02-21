using System.Text.Json.Serialization;
using MACHTEN.Api.Features.CreateProduct;

namespace MACHTEN.Api;

[JsonSerializable(typeof(CreateProductRequest))]
[JsonSerializable(typeof(CreateProductResponse))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class AppSerializerContext : JsonSerializerContext;
