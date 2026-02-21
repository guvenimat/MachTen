using FastEndpoints;
using FastEndpoints.Swagger;
using MACHTEN.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "MACHTEN API";
        s.Version = "v1";
    };
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
    c.Serializer.Options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    c.Serializer.Options.TypeInfoResolverChain.Insert(0, AppSerializerContext.Default);
});
app.UseSwaggerGen();

app.Run();
