# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ZoiNailAcademy.slnx ./
COPY Directory.Build.props ./
COPY src/ZoiNailAcademy.Domain/ZoiNailAcademy.Domain.csproj src/ZoiNailAcademy.Domain/
COPY src/ZoiNailAcademy.Application/ZoiNailAcademy.Application.csproj src/ZoiNailAcademy.Application/
COPY src/ZoiNailAcademy.Infrastructure/ZoiNailAcademy.Infrastructure.csproj src/ZoiNailAcademy.Infrastructure/
COPY src/ZoiNailAcademy.Host/ZoiNailAcademy.Host.csproj src/ZoiNailAcademy.Host/

RUN dotnet restore src/ZoiNailAcademy.Host/ZoiNailAcademy.Host.csproj

COPY src/ src/
RUN dotnet publish src/ZoiNailAcademy.Host/ZoiNailAcademy.Host.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "ZoiNailAcademy.Host.dll"]
