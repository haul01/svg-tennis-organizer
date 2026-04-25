# Build stage: SDK image, restore + publish.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["TennisClub.Api.csproj", "./"]
RUN dotnet restore "TennisClub.Api.csproj"

COPY . .
RUN dotnet publish "TennisClub.Api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app \
    /p:UseAppHost=false

# Runtime stage: ASP.NET image only, no SDK.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_PRINT_TELEMETRY_MESSAGE=false

COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "TennisClub.Api.dll"]
