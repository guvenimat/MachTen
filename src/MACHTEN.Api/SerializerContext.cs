using System.Text.Json.Serialization;
using MACHTEN.Api.Features.Auth;
using MACHTEN.Api.Features.Ping;
using MACHTEN.Application.Features.Orders.GetOrder;
using MACHTEN.Application.Features.Orders.PlaceOrder;
using Microsoft.AspNetCore.Mvc;

namespace MACHTEN.Api;

[JsonSerializable(typeof(PingResponse))]
[JsonSerializable(typeof(MeResponse))]
[JsonSerializable(typeof(PlaceOrderCommand))]
[JsonSerializable(typeof(PlaceOrderResponse))]
[JsonSerializable(typeof(OrderDto))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class AppSerializerContext : JsonSerializerContext;
