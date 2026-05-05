FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Diguifi.sln ./
COPY src/Diguifi.Api/Diguifi.Api.csproj src/Diguifi.Api/
COPY src/Diguifi.Application/Diguifi.Application.csproj src/Diguifi.Application/
COPY src/Diguifi.Domain/Diguifi.Domain.csproj src/Diguifi.Domain/
COPY src/Diguifi.Infrastructure/Diguifi.Infrastructure.csproj src/Diguifi.Infrastructure/

RUN dotnet restore src/Diguifi.Api/Diguifi.Api.csproj

COPY . .
RUN dotnet publish src/Diguifi.Api/Diguifi.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-10000} dotnet Diguifi.Api.dll"]
