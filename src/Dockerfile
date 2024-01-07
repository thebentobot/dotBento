FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/dotBento.Bot", "dotBento.Bot/"]
COPY ["src/dotBento.Domain", "dotBento.Domain/"]
COPY ["src/dotBento.EntityFramework", "dotBento.EntityFramework/"]
RUN dotnet restore "dotBento.Bot/dotBento.Bot.csproj"
COPY . .
WORKDIR "/src/dotBento.Bot"
RUN dotnet build "dotBento.Bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "dotBento.Bot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "dotBento.Bot.dll"]

HEALTHCHECK --interval=20s --timeout=20s --retries=90 CMD find healthcheck -mmin -1