# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Create 'app' user and group
RUN addgroup --system app && adduser --system --ingroup app app

# Switch to the 'app' user
USER app

WORKDIR /app
EXPOSE 8080
EXPOSE 8081



# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RecipeAI/RecipeAI/RecipeAI.csproj", "RecipeAI/RecipeAI/"]
COPY ["RecipeAI/RecipeAI.Client/RecipeAI.Client.csproj", "RecipeAI/RecipeAI.Client/"]
RUN dotnet restore "./RecipeAI/RecipeAI/RecipeAI.csproj"
COPY . .
WORKDIR "/src/RecipeAI/RecipeAI"
RUN dotnet build "./RecipeAI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RecipeAI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RecipeAI.dll"]