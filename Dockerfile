# ── مرحله ۱: build ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /repo

# کپی فایل‌های پروژه برای cache لایه restore
COPY Directory.Build.props .
COPY Shekar.sln .
COPY src/Shekar.Domain/Shekar.Domain.csproj             src/Shekar.Domain/
COPY src/Shekar.Application/Shekar.Application.csproj   src/Shekar.Application/
COPY src/Shekar.Infrastructure/Shekar.Infrastructure.csproj src/Shekar.Infrastructure/
COPY src/Shekar.Mock/Shekar.Mock.csproj                  src/Shekar.Mock/
COPY src/Shekar.Web/Shekar.Web.csproj                    src/Shekar.Web/
COPY src/Shekar.Worker/Shekar.Worker.csproj              src/Shekar.Worker/
COPY tests/Shekar.Application.Tests/Shekar.Application.Tests.csproj tests/Shekar.Application.Tests/

RUN dotnet restore src/Shekar.Web/Shekar.Web.csproj

# کپی همه سورس‌کد و publish
COPY . .
RUN dotnet publish src/Shekar.Web/Shekar.Web.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# ── مرحله ۲: runtime ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Liara پشت reverse proxy کار می‌کند؛ HTTPS داخل کانتینر نیاز نیست
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
# پردازش خودکار هدرهای X-Forwarded-Proto / X-Forwarded-For
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Shekar.Web.dll"]
