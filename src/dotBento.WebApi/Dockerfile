FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY src/dotBento.WebApi/ dotBento.WebApi/
COPY src/dotBento.EntityFramework/ dotBento.EntityFramework/
COPY src/dotBento.Infrastructure/ dotBento.Infrastructure/
COPY src/dotBento.Domain/ dotBento.Domain/

WORKDIR /src/dotBento.WebApi

RUN dotnet restore "dotBento.WebApi.csproj"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "dotBento.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "dotBento.WebApi.dll"]
