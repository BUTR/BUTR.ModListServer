FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS restore
ARG TARGETARCH
WORKDIR /build

COPY ["src/BUTR.ModListServer/BUTR.ModListServer.csproj", "BUTR.ModListServer/"]
COPY ["src/nuget.config", "src/"]

RUN dotnet restore "BUTR.ModListServer/BUTR.ModListServer.csproj" -a $TARGETARCH;

COPY ["src/BUTR.ModListServer/", "BUTR.ModListServer/"]

#COPY [".git/HEAD", ".git/HEAD"]
#COPY [".git/config", ".git/config"]
#COPY [".git/refs/heads/", ".git/refs/heads/"]


FROM restore AS publish
ARG TARGETARCH
WORKDIR /build

RUN dotnet publish "src/BUTR.ModListServer/BUTR.ModListServer.csproj" -c Release -a $TARGETARCH -o /app/publish;


FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS final
WORKDIR /app
COPY --from=publish /app/publish .

LABEL org.opencontainers.image.source="https://github.com/BUTR/BUTR.ModListServer"
EXPOSE 8080/tcp
ENTRYPOINT ["dotnet", "BUTR.ModListServer.dll"]