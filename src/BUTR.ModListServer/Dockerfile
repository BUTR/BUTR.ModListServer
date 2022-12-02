FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY ["src/BUTR.ModListServer/BUTR.ModListServer.csproj", "src/BUTR.ModListServer/"]

RUN dotnet restore "src/BUTR.ModListServer/BUTR.ModListServer.csproj"

COPY . .

WORKDIR "/src/BUTR.ModListServer"
RUN dotnet build "src/BUTR.ModListServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/BUTR.ModListServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

LABEL org.opencontainers.image.source="https://github.com/BUTR/BUTR.ModListServer"
EXPOSE 8080/tcp
ENTRYPOINT ["dotnet", "BUTR.ModListServer.dll"]