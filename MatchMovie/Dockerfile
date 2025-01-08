# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar os arquivos do projeto
COPY ["MatchMovie.csproj", "./"]
RUN dotnet restore

# Copiar todo o código fonte
COPY . .
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Configurar variáveis de ambiente do Redis
ENV Redis__ConnectionString=localhost:6379
ENV Redis__InternalUrl=redis://localhost:6379
ENV Redis__DatabaseId=0
ENV Redis__DefaultExpirationInHours=1

# Configurar variáveis de ambiente da aplicação
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Expor a porta 80
EXPOSE 80

# Iniciar a aplicação
ENTRYPOINT ["dotnet", "MatchMovie.dll"]