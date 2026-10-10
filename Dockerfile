# Keep this in sync with the SDK pinned by global.json.
FROM mcr.microsoft.com/dotnet/sdk:10.0.300 AS build
WORKDIR /src

COPY ["global.json", "Directory.Build.props", "KH2.ManagementSystem.slnx", "./"]
COPY ["src/KH2.ManagementSystem.Api/KH2.ManagementSystem.Api.csproj", "src/KH2.ManagementSystem.Api/"]
COPY ["src/KH2.ManagementSystem.Application/KH2.ManagementSystem.Application.csproj", "src/KH2.ManagementSystem.Application/"]
COPY ["src/KH2.ManagementSystem.Domain/KH2.ManagementSystem.Domain.csproj", "src/KH2.ManagementSystem.Domain/"]
COPY ["src/KH2.ManagementSystem.Infrastructure/KH2.ManagementSystem.Infrastructure.csproj", "src/KH2.ManagementSystem.Infrastructure/"]
COPY ["src/KH2.ManagementSystem.Shared/KH2.ManagementSystem.Shared.csproj", "src/KH2.ManagementSystem.Shared/"]

RUN dotnet restore "src/KH2.ManagementSystem.Api/KH2.ManagementSystem.Api.csproj"

COPY . .
RUN dotnet publish "src/KH2.ManagementSystem.Api/KH2.ManagementSystem.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=true

RUN apt-get update \
    && apt-get install --no-install-recommends -y curl \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --system --gid 10001 kh2backend \
    && useradd --system --uid 10001 --gid kh2backend --home-dir /app --no-create-home --shell /usr/sbin/nologin kh2backend

COPY --from=build /app/publish .
RUN chown -R kh2backend:kh2backend /app

USER kh2backend

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD ["curl", "--fail", "--silent", "--show-error", "http://127.0.0.1:8080/health/live"]

ENTRYPOINT ["dotnet", "KH2.ManagementSystem.Api.dll"]
