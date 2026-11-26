FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# The final stage runs as the image's non-root user ($APP_UID) and listens on 8080 (ASPNETCORE_HTTP_PORTS).
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy every referenced project file first so the restore layer stays cached until dependencies change
COPY ["Voxa.WebApi/Voxa.WebApi.csproj", "Voxa.WebApi/"]
COPY ["Voxa/Voxa.csproj", "Voxa/"]
COPY ["Voxa.Persistence/Voxa.Persistence.csproj", "Voxa.Persistence/"]
COPY ["Voxa.Abstractions/Voxa.Abstractions.csproj", "Voxa.Abstractions/"]
RUN dotnet restore "Voxa.WebApi/Voxa.WebApi.csproj"
COPY . .
WORKDIR "/src/Voxa.WebApi"

# Publish compiles the project, so a separate build step would compile everything twice
FROM build AS publish
RUN dotnet publish "Voxa.WebApi.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
# The PWA compares this version with its own and shows a maintenance screen when they differ
ARG APP_VERSION=development
ENV APP_VERSION=$APP_VERSION
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Voxa.WebApi.dll"]
