FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TaskManagement.sln* .
COPY src/TaskManagement.API/*.csproj src/TaskManagement.API/
COPY src/TaskManagement.Application/*.csproj src/TaskManagement.Application/
COPY src/TaskManagement.Domain/*.csproj src/TaskManagement.Domain/
COPY src/TaskManagement.Infrastructure/*.csproj src/TaskManagement.Infrastructure/
RUN dotnet restore src/TaskManagement.API/TaskManagement.API.csproj

COPY . .
WORKDIR /src/src/TaskManagement.API
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends libkrb5-3 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskManagement.API.dll"]
