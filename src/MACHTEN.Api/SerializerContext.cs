using System.Text.Json.Serialization;
using MACHTEN.Api.Features.Products.BulkCreateProducts;
using MACHTEN.Api.Features.Products.CreateProduct;
using MACHTEN.Api.Features.Products.GetProduct;
using MACHTEN.Api.Features.Products.GetProducts;
using MACHTEN.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MACHTEN.Api;

[JsonSerializable(typeof(CreateProductCommand))]
[JsonSerializable(typeof(CreateProductResponse))]
[JsonSerializable(typeof(GetProductRequest))]
[JsonSerializable(typeof(GetProductResponse))]
[JsonSerializable(typeof(GetProductsResponse))]
[JsonSerializable(typeof(List<GetProductsResponse>))]
[JsonSerializable(typeof(BulkCreateProductsCommand))]
[JsonSerializable(typeof(BulkCreateProductsResponse))]
[JsonSerializable(typeof(BulkProductInsertDto))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class AppSerializerContext : JsonSerializerContext;
