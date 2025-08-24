# CloudFlash API - Scripts de Deploy

Este diretório contém scripts para automatizar o deploy da aplicação CloudFlash no Azure.

## Scripts Disponíveis

### 1. deploy-infrastructure.ps1
Deploy da infraestrutura Azure usando Bicep templates.

```powershell
./scripts/deploy-infrastructure.ps1 -Environment "prod" -Location "brazilsouth" -TmdbApiKey "your-api-key"
```

### 2. deploy-application.ps1
Deploy da aplicação para Azure App Service.

```powershell
./scripts/deploy-application.ps1 -Environment "prod"
```

### 3. setup-development.ps1
Configuração do ambiente de desenvolvimento local.

```powershell
./scripts/setup-development.ps1
```

## Parâmetros

- **Environment**: dev, staging, prod
- **Location**: Região do Azure (brazilsouth, eastus2, etc.)
- **TmdbApiKey**: Chave da API do TMDB

## Pré-requisitos

- Azure CLI instalado e autenticado
- PowerShell 7.0+
- Docker Desktop (para desenvolvimento local)
- .NET 8.0 SDK
