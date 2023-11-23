FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["dotBento.Bot/dotBento.Bot.csproj", "dotBento.Bot/"]
RUN dotnet restore "dotBento.Bot/dotBento.Bot.csproj"
COPY . .
WORKDIR "/src/dotBento.Bot"
RUN dotnet build "dotBento.Bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "dotBento.Bot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "dotBento.Bot.dll"]
