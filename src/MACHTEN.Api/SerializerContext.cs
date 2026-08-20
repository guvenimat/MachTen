using System.Text.Json.Serialization;
using MACHTEN.Api.Features.Ping;
using MACHTEN.Application.Features.Money;
using Microsoft.AspNetCore.Mvc;

namespace MACHTEN.Api;

[JsonSerializable(typeof(PingResponse))]
[JsonSerializable(typeof(FormatMoneyCommand))]
[JsonSerializable(typeof(MoneyDto))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class AppSerializerContext : JsonSerializerContext;
