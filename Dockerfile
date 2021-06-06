FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
 
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["DockerExplained/DockerExplained.csproj", "DockerExplained/"]
RUN dotnet restore "DockerExplained/DockerExplained.csproj"
COPY . .
WORKDIR "/src/DockerExplained"
RUN dotnet build "DockerExplained.csproj" -c Release -o /app/build
 
FROM build AS publish
RUN dotnet publish "DockerExplained.csproj" -c Release -o /app/publish
 
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DockerExplained.dll"]
