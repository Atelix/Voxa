FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS base
WORKDIR /app
# The final stage runs as the image's non-root user ($APP_UID) and listens on 8080 (ASPNETCORE_HTTP_PORTS).
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src
# Copy every referenced project file first so the restore layer stays cached until dependencies change
COPY ["Starter.WebApi/Starter.WebApi.csproj", "Starter.WebApi/"]
COPY ["Starter/Starter.csproj", "Starter/"]
COPY ["Starter.Persistence/Starter.Persistence.csproj", "Starter.Persistence/"]
COPY ["Starter.Abstractions/Starter.Abstractions.csproj", "Starter.Abstractions/"]
RUN dotnet restore "Starter.WebApi/Starter.WebApi.csproj"
COPY . .
WORKDIR "/src/Starter.WebApi"

# Publish compiles the project, so a separate build step would compile everything twice
FROM build AS publish
RUN dotnet publish "Starter.WebApi.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
# The PWA compares this version with its own and shows a maintenance screen when they differ
ARG APP_VERSION=development
ENV APP_VERSION=$APP_VERSION
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Starter.WebApi.dll"]
