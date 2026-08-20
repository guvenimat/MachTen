# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MACHTEN.sln .
COPY src/MACHTEN.Domain/MACHTEN.Domain.csproj src/MACHTEN.Domain/
COPY src/MACHTEN.Application/MACHTEN.Application.csproj src/MACHTEN.Application/
COPY src/MACHTEN.Infrastructure/MACHTEN.Infrastructure.csproj src/MACHTEN.Infrastructure/
COPY src/MACHTEN.Api/MACHTEN.Api.csproj src/MACHTEN.Api/
RUN dotnet restore src/MACHTEN.Api/MACHTEN.Api.csproj -r linux-x64

COPY src/ src/
RUN dotnet publish src/MACHTEN.Api/MACHTEN.Api.csproj -c Release -r linux-x64 --self-contained false -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The base image ships a non-root "app" user (uid 1654). Running as it means a
# container breakout starts with no privileges worth having.
COPY --from=build --chown=app:app /app/publish .
USER app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=1

EXPOSE 8080

# Lets the orchestrator see the difference between "process alive" and
# "dependencies reachable".
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD ["dotnet", "MACHTEN.Api.dll", "--healthcheck"]

ENTRYPOINT ["dotnet", "MACHTEN.Api.dll"]
