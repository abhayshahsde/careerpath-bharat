# Multi-stage Dockerfile for CareerPath Bharat .NET 8 Backend API

# Step 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["apps/api/CareerPath.Api/CareerPath.Api.csproj", "apps/api/CareerPath.Api/"]
COPY ["apps/api/CareerPath.Application/CareerPath.Application.csproj", "apps/api/CareerPath.Application/"]
COPY ["apps/api/CareerPath.Domain/CareerPath.Domain.csproj", "apps/api/CareerPath.Domain/"]
COPY ["apps/api/CareerPath.Infrastructure/CareerPath.Infrastructure.csproj", "apps/api/CareerPath.Infrastructure/"]
COPY ["apps/api/CareerPath.Contracts/CareerPath.Contracts.csproj", "apps/api/CareerPath.Contracts/"]
COPY ["apps/api/CareerPath.Migrations/CareerPath.Migrations.csproj", "apps/api/CareerPath.Migrations/"]

RUN dotnet restore "apps/api/CareerPath.Api/CareerPath.Api.csproj"

# Copy remaining source code and publish
COPY . .
WORKDIR "/src/apps/api/CareerPath.Api"
RUN dotnet publish "CareerPath.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Step 2: Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CareerPath.Api.dll"]
