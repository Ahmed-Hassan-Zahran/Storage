FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Yoxel.Storage.Core/Yoxel.Storage.Core.csproj                     src/Yoxel.Storage.Core/
COPY src/Yoxel.Storage.Infrastructure/Yoxel.Storage.Infrastructure.csproj src/Yoxel.Storage.Infrastructure/
COPY src/Yoxel.Storage.Api/Yoxel.Storage.Api.csproj                       src/Yoxel.Storage.Api/
RUN dotnet restore src/Yoxel.Storage.Api/Yoxel.Storage.Api.csproj

COPY . .
RUN dotnet publish src/Yoxel.Storage.Api/Yoxel.Storage.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Yoxel.Storage.Api.dll"]
