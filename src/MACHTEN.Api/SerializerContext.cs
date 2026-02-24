using System.Text.Json.Serialization;
using MACHTEN.Application.DTOs;
using MACHTEN.Application.Features.Products.BulkCreateProducts;
using MACHTEN.Application.Features.Products.CreateProduct;
using MACHTEN.Application.Features.Products.GetProduct;
using MACHTEN.Application.Features.Products.GetProducts;
using Microsoft.AspNetCore.Mvc;

namespace MACHTEN.Api;

[JsonSerializable(typeof(CreateProductCommand))]
[JsonSerializable(typeof(CreateProductResponse))]
[JsonSerializable(typeof(GetProductQuery))]
[JsonSerializable(typeof(GetProductResponse))]
[JsonSerializable(typeof(GetProductsQuery))]
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
