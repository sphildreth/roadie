#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk AS build
WORKDIR /src
COPY ["Roadie.Api/Roadie.Api.csproj", "Roadie.Api/"]
COPY ["Roadie.Api.Services/Roadie.Api.Services.csproj", "Roadie.Api.Services/"]
COPY ["Roadie.Api.Library/Roadie.Library.csproj", "Roadie.Api.Library/"]
COPY ["Roadie.Dlna/Roadie.Dlna.csproj", "Roadie.Dlna/"]
COPY ["Roadie.Api.Hubs/Roadie.Api.Hubs.csproj", "Roadie.Api.Hubs/"]
COPY ["Roadie.Dlna.Services/Roadie.Dlna.Services.csproj", "Roadie.Dlna.Services/"]
RUN dotnet restore "Roadie.Api/Roadie.Api.csproj"
COPY . .
WORKDIR "/src/Roadie.Api"
RUN dotnet build "Roadie.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Roadie.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
ENV ASPNETCORE_URLS=http://*:80
ENV RoadieSettings:SiteName="Roadie"
ENV RoadieSettings:DbContextToUse="SQLite"
ENV RoadieSettings:LibraryFolder="library"
ENV RoadieSettings:InboundFolder="inbound"
ENV RoadieSettings:FileDatabaseOptions:DatabaseFolder="data"
ENTRYPOINT ["dotnet", "Roadie.Api.dll"]