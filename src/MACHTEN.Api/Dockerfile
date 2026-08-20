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
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "MACHTEN.Api.dll"]
