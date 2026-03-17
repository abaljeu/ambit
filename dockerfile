# Build stage - compile both Fable and server
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy project files for restore layer caching
COPY *.sln ./
COPY src/Server/*.fsproj ./src/Server/
COPY src/Client/*.fsproj ./src/Client/
COPY src/Shared/*.fsproj ./src/Shared/
COPY tests/Shared.Tests/*.fsproj ./tests/Shared.Tests/
COPY tests/Server.Tests/*.fsproj ./tests/Server.Tests/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . ./

# Build Fable client (output to wwwroot)
WORKDIR /app/src/Client
RUN dotnet tool restore
RUN dotnet fable . -o ../Server/wwwroot

# Build and publish server (includes wwwroot)
WORKDIR /app/src/Server
RUN dotnet publish -c Release -o /app/publish

# Runtime stage - smaller image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# Fly.io sets PORT, we'll use it
ENV ASPNETCORE_URLS=http://+:8080
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Ambit.Server.dll"]