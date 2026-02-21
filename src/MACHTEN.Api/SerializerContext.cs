using System.Text.Json.Serialization;
using MACHTEN.Api.Features.Products.CreateProduct;

namespace MACHTEN.Api;

[JsonSerializable(typeof(CreateProductCommand))]
[JsonSerializable(typeof(CreateProductResponse))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class AppSerializerContext : JsonSerializerContext;
