FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS restore
ARG TARGETARCH
WORKDIR /build

COPY ["src/BUTR.ModListServer/BUTR.ModListServer.csproj", "src/BUTR.ModListServer/"]
#COPY ["src/nuget.config", "src/"]

RUN dotnet restore "src/BUTR.ModListServer/BUTR.ModListServer.csproj" -a $TARGETARCH;

COPY ["src/BUTR.ModListServer/", "src/BUTR.ModListServer/"]

#COPY [".git/HEAD", ".git/HEAD"]
#COPY [".git/config", ".git/config"]
#COPY [".git/refs/heads/", ".git/refs/heads/"]


FROM restore AS publish
ARG TARGETARCH
WORKDIR /build

RUN dotnet publish "src/BUTR.ModListServer/BUTR.ModListServer.csproj" -c Release -a $TARGETARCH -o /app/publish;


# Chiseled: no shell or package manager. -extra keeps ICU/tzdata, because the project does not set
# InvariantGlobalization. If the app needs no culture data, set it and use the plain chiseled image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Disable the diagnostics IPC pipe in production.
ENV DOTNET_EnableDiagnostics=0

LABEL org.opencontainers.image.source="https://github.com/BUTR/BUTR.ModListServer"
# Run as the image's pre-created non-root user (UID 1654).
USER $APP_UID
EXPOSE 8080/tcp
ENTRYPOINT ["dotnet", "BUTR.ModListServer.dll"]
