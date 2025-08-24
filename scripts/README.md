# CloudFlash API - Scripts de Deploy e Configuração

![PowerShell](https://img.shields.io/badge/PowerShell-7.0+-5391FE?style=flat&logo=powershell)
![Azure](https://img.shields.io/badge/Azure-CLI-0078D4?style=flat&logo=microsoft-azure)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=flat&logo=docker)

Este diretório contém scripts PowerShell para automatizar o deploy e configuração da aplicação CloudFlash no Azure.

## 📋 Scripts Disponíveis

### 🚀 deploy-complete.ps1
Script de deploy completo que executa tanto infraestrutura quanto aplicação em sequência. Ideal para deploy automatizado.

**Funcionalidades:**
- Deploy completo de infraestrutura e aplicação
- Validação de pré-requisitos
- Estimativa de tempo de deploy
- Validação final com health checks
- Suporte a modo dry-run para testes
- Relatório detalhado de deploy

```powershell
# Deploy completo para produção
./scripts/deploy-complete.ps1 -Environment "prod" -TmdbApiKey "your-api-key"

# Deploy apenas da aplicação (infraestrutura já existe)
./scripts/deploy-complete.ps1 -Environment "dev" -TmdbApiKey "your-key" -SkipInfrastructure

# Dry run para validar configurações
./scripts/deploy-complete.ps1 -Environment "staging" -TmdbApiKey "your-key" -DryRun
```

### 🏗️ deploy-infrastructure.ps1
Deploy completo da infraestrutura Azure usando Bicep templates. Cria todos os recursos necessários para executar a aplicação CloudFlash.

**Recursos criados:**
- Azure Container Registry
- Azure App Service Plan
- Azure Web App
- Application Insights
- Key Vault (para secrets)
- Storage Account (se necessário)

```powershell
# Deploy em ambiente de produção
./scripts/deploy-infrastructure.ps1 -Environment "prod" -Location "brazilsouth" -TmdbApiKey "your-api-key"

# Deploy com subscription específica
./scripts/deploy-infrastructure.ps1 -Environment "dev" -Location "eastus2" -TmdbApiKey "your-api-key" -SubscriptionId "your-subscription-id"

# Deploy com Resource Group customizado
./scripts/deploy-infrastructure.ps1 -Environment "staging" -ResourceGroupName "my-custom-rg" -TmdbApiKey "your-api-key"
```

### 🚀 deploy-application.ps1
Deploy da aplicação containerizada para Azure App Service. Constrói a imagem Docker e faz o deploy para o Azure.

**Funcionalidades:**
- Build automático da imagem Docker
- Push para Azure Container Registry
- Deploy para Azure Web App
- Configuração de variáveis de ambiente
- Health check pós-deploy

```powershell
# Deploy básico para produção
./scripts/deploy-application.ps1 -Environment "prod"

# Deploy com tag específica
./scripts/deploy-application.ps1 -Environment "staging" -ImageTag "v1.2.0"

# Deploy com Resource Group customizado
./scripts/deploy-application.ps1 -Environment "dev" -ResourceGroupName "my-custom-rg"
```

### 🛠️ setup-development.ps1
Configuração completa do ambiente de desenvolvimento local. Verifica pré-requisitos, instala dependências e configura o ambiente.

**Funcionalidades:**
- Verificação de pré-requisitos (.NET 9+, Docker, Azure CLI)
- Restauração de dependências NuGet
- Build e testes do projeto
- Configuração de user secrets
- Setup do banco de dados local (se aplicável)
- Configuração de variáveis de ambiente
- Inicialização de containers Docker para desenvolvimento

```powershell
# Configuração completa do ambiente
./scripts/setup-development.ps1
```

### 🔍 validate-environment.ps1
Script de validação do ambiente de desenvolvimento. Verifica se todos os pré-requisitos estão instalados e configurados corretamente.

**Funcionalidades:**
- Verificação de versões de ferramentas
- Validação da estrutura do projeto
- Teste de build e dependências
- Verificação de conectividade (opcional)
- Relatório detalhado de status

```powershell
# Validação completa do ambiente
./scripts/validate-environment.ps1

# Pular verificações opcionais
./scripts/validate-environment.ps1 -SkipOptional
```

## 🔧 Parâmetros Detalhados

### Ambientes Suportados
- **dev**: Desenvolvimento com recursos mínimos
- **staging**: Ambiente de testes com configuração similar à produção
- **prod**: Produção com alta disponibilidade e performance

### Regiões Azure Recomendadas
- **brazilsouth**: Região principal do Brasil (São Paulo)
- **eastus2**: Estados Unidos (Virginia) - alternativa
- **westeurope**: Europa Ocidental - alternativa
- **southeastasia**: Ásia Sudeste - alternativa

### Parâmetros Comuns
- **Environment**: Ambiente de deploy (obrigatório)
- **Location**: Região do Azure (padrão: brazilsouth)
- **TmdbApiKey**: Chave da API do TMDB (obrigatório para infra)
- **ResourceGroupName**: Nome customizado do Resource Group
- **SubscriptionId**: ID da subscription Azure específica
- **ImageTag**: Tag da imagem Docker (padrão: latest)

## 📋 Pré-requisitos

### Ferramentas Obrigatórias
- **.NET SDK** 9.0+
  ```powershell
  # Verificar versão
  dotnet --version
  
  # Baixar: https://dotnet.microsoft.com/download/dotnet/9.0
  ```

- **Azure CLI** 2.60.0+
  ```powershell
  # Instalar via winget
  winget install Microsoft.AzureCLI
  
  # Verificar versão
  az --version
  ```

- **PowerShell** 7.0+
  ```powershell
  # Verificar versão
  $PSVersionTable.PSVersion
  ```

- **Docker Desktop** 4.0+
  ```powershell
  # Verificar se está rodando
  docker info
  ```

- **.NET SDK** 9.0+
  ```powershell
  # Verificar versão instalada
  dotnet --version
  
  # Listar SDKs instalados
  dotnet --list-sdks
  ```

### Configurações Necessárias

#### 1. Autenticação Azure
```powershell
# Login interativo
az login

# Login com service principal (CI/CD)
az login --service-principal -u $clientId -p $clientSecret --tenant $tenantId

# Verificar conta ativa
az account show
```

#### 2. Variáveis de Ambiente (Desenvolvimento)
```powershell
# TMDB API Key
$env:TMDB_API_KEY = "your-tmdb-api-key"

# Azure Subscription (opcional)
$env:AZURE_SUBSCRIPTION_ID = "your-subscription-id"
```

#### 3. User Secrets (Desenvolvimento Local)
```powershell
# Navegar para o projeto API
cd src/CloudFlash.API

# Configurar secrets
dotnet user-secrets set "TmdbApiSettings:ApiKey" "your-tmdb-api-key"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
```

## 🔄 Fluxo de Deploy Completo

### 1. Primeiro Deploy (Infraestrutura)
```powershell
# 1. Deploy da infraestrutura
./scripts/deploy-infrastructure.ps1 -Environment "prod" -TmdbApiKey "your-api-key"

# 2. Deploy da aplicação
./scripts/deploy-application.ps1 -Environment "prod"
```

### 2. Deploy Incremental (Apenas Aplicação)
```powershell
# Para atualizações de código
./scripts/deploy-application.ps1 -Environment "prod" -ImageTag "v1.2.0"
```

### 3. Desenvolvimento Local
```powershell
# Configurar ambiente uma vez
./scripts/setup-development.ps1

# Executar aplicação
cd src/CloudFlash.API
dotnet run

# Ou via Docker
docker-compose up -d
```

## 🐛 Troubleshooting

### Problemas Comuns

#### 1. Erro de Autenticação Azure
```powershell
# Re-fazer login
az logout
az login
az account set --subscription "your-subscription-id"
```

#### 2. Docker não está rodando
```powershell
# Verificar status
docker info

# Iniciar Docker Desktop manualmente
# Ou via PowerShell (se configurado)
Start-Service docker
```

#### 3. .NET SDK não encontrado
```powershell
# Verificar PATH
$env:PATH -split ';' | Where-Object { $_ -like "*dotnet*" }

# Instalar .NET 9.0
winget install Microsoft.DotNet.SDK.9
```

#### 4. Falha no Deploy Bicep
```powershell
# Verificar sintaxe do template
az bicep build --file ./infra/bicep/main.bicep

# Deploy com logs detalhados
az deployment group create --resource-group "rg-name" --template-file "./infra/bicep/main.bicep" --verbose
```

## 📚 Links Úteis

- [Azure CLI Documentation](https://docs.microsoft.com/en-us/cli/azure/)
- [PowerShell 7 Documentation](https://docs.microsoft.com/en-us/powershell/)
- [Docker Documentation](https://docs.docker.com/)
- [.NET 9.0 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Azure Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [TMDB API Documentation](https://developers.themoviedb.org/3)
