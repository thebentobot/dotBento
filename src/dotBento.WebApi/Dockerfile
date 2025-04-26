FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/dotBento.WebApi/dotBento.WebApi.csproj", "dotBento.WebApi/"]
COPY ["src/dotBento.EntityFramework/dotBento.EntityFramework.csproj", "dotBento.EntityFramework/"]
RUN dotnet restore "dotBento.WebApi/dotBento.WebApi.csproj"
COPY . .
WORKDIR "/src/dotBento.WebApi"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "dotBento.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "dotBento.WebApi.dll"]
