#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
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
ENTRYPOINT ["dotnet", "Roadie.Api.dll"]