using FastEndpoints;
using FastEndpoints.Swagger;
using MACHTEN.Api;
using MACHTEN.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextPool<MachtenDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "MACHTEN API";
        s.Version = "v1";
    };
});

builder.Host.UseWolverine();

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
