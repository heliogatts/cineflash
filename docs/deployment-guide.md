# Guia de Deploy - CloudFlash API

Este documento descreve o processo completo para deploy da API CloudFlash no Microsoft Azure, desde a configuração inicial até o monitoramento em produção.

## 📋 Pré-requisitos

### Ferramentas Necessárias
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) 2.50+
- [PowerShell](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell) 7.0+
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Git](https://git-scm.com/)

### Contas e Credenciais
- Conta Azure ativa
- Subscription do Azure com permissions de Contributor
- [TMDB API Key](https://www.themoviedb.org/settings/api) (gratuita)

## 🚀 Deploy Rápido

### 1. Configuração Inicial
```powershell
# Clone o repositório
git clone https://github.com/heliogatts/cineflash.git
cd cineflash

# Execute o setup de desenvolvimento
./scripts/setup-development.ps1
```

### 2. Deploy da Infraestrutura
```powershell
# Login no Azure
az login

# Deploy da infraestrutura
./scripts/deploy-infrastructure.ps1 -Environment "prod" -TmdbApiKey "YOUR_TMDB_API_KEY"
```

### 3. Deploy da Aplicação
```powershell
# Build e deploy da aplicação
./scripts/deploy-application.ps1 -Environment "prod"
```

## 🏗️ Deploy Manual Detalhado

### Passo 1: Preparação do Ambiente

#### 1.1 Verificar Azure CLI
```bash
az --version
az login
az account show
```

#### 1.2 Configurar Subscription
```bash
az account set --subscription "YOUR_SUBSCRIPTION_ID"
```

#### 1.3 Criar Resource Group
```bash
az group create --name "rg-cloudflash-prod" --location "brazilsouth"
```

### Passo 2: Deploy da Infraestrutura

#### 2.1 Validar Template Bicep
```bash
az deployment group validate \
  --resource-group "rg-cloudflash-prod" \
  --template-file "./infra/bicep/main.bicep" \
  --parameters baseName="cloudflash" environment="prod" tmdbApiKey="YOUR_API_KEY"
```

#### 2.2 Deploy dos Recursos
```bash
az deployment group create \
  --resource-group "rg-cloudflash-prod" \
  --template-file "./infra/bicep/main.bicep" \
  --parameters baseName="cloudflash" environment="prod" tmdbApiKey="YOUR_API_KEY" \
  --name "cloudflash-infra-deployment"
```

#### 2.3 Verificar Recursos Criados
```bash
az resource list --resource-group "rg-cloudflash-prod" --output table
```

### Passo 3: Configuração de Aplicação

#### 3.1 Build da Imagem Docker
```bash
# Na raiz do projeto
docker build -t cloudflash:latest .
```

#### 3.2 Login no Container Registry
```bash
az acr login --name "acrcloudflashprod"
```

#### 3.3 Tag e Push da Imagem
```bash
docker tag cloudflash:latest acrcloudflashprod.azurecr.io/cloudflash:latest
docker push acrcloudflashprod.azurecr.io/cloudflash:latest
```

#### 3.4 Configurar App Service
```bash
az webapp config container set \
  --name "app-cloudflash-prod" \
  --resource-group "rg-cloudflash-prod" \
  --docker-custom-image-name "acrcloudflashprod.azurecr.io/cloudflash:latest"
```

### Passo 4: Configuração do CI/CD

#### 4.1 Criar Service Principal
```bash
az ad sp create-for-rbac --name "sp-cloudflash-github" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/rg-cloudflash-prod \
  --sdk-auth
```

#### 4.2 Configurar GitHub Secrets
No GitHub, vá para Settings > Secrets and Variables > Actions e adicione:

```
AZURE_CREDENTIALS: {output do comando acima}
AZURE_SUBSCRIPTION_ID: {sua subscription id}
AZURE_RG: rg-cloudflash-prod
TMDB_API_KEY: {sua chave da api tmdb}
ACR_USERNAME: acrcloudflashprod
ACR_PASSWORD: {senha do acr}
```

#### 4.3 Ativar Workflow
```bash
# Commit e push para ativar o workflow
git add .
git commit -m "ci: setup azure deployment"
git push origin main
```

## 🔧 Configurações Específicas

### Cosmos DB
- **Database**: CloudFlashDB
- **Container**: Titles
- **Partition Key**: /id
- **Consistency**: Session
- **Mode**: Serverless (dev), Provisioned (prod)

### App Service
- **Runtime**: Docker Container
- **OS**: Linux
- **SKU**: B1 (dev), P1v3 (prod)
- **Region**: Brazil South

### Application Insights
- **Telemetry**: Enabled
- **Sampling**: 100% (dev), 10% (prod)
- **Retention**: 30 days (dev), 90 days (prod)

### Azure Search
- **Tier**: Free (dev), Standard (prod)
- **Replicas**: 1 (dev), 2 (prod)
- **Partitions**: 1

## 📊 Monitoramento

### Application Insights Queries

#### Performance Monitoring
```kql
requests
| where timestamp > ago(1h)
| summarize avg(duration), percentile(duration, 95) by name
| order by avg_duration desc
```

#### Error Analysis
```kql
exceptions
| where timestamp > ago(24h)
| summarize count() by type, outerMessage
| order by count_ desc
```

#### Dependency Tracking
```kql
dependencies
| where timestamp > ago(1h)
| where success == false
| summarize count() by name, resultCode
```

### Health Checks
- **Endpoint**: `/api/v1/health`
- **Frequency**: 30 segundos
- **Timeout**: 10 segundos
- **Retries**: 3

### Alertas Recomendados

#### 1. Alta Taxa de Erro
```kql
requests
| where timestamp > ago(5m)
| where success == false
| summarize errorRate = count() * 100.0 / (count() + countif(success))
| where errorRate > 5
```

#### 2. Latência Alta
```kql
requests
| where timestamp > ago(5m)
| summarize p95 = percentile(duration, 95)
| where p95 > 2000  // 2 segundos
```

#### 3. Uso de RU Cosmos DB
```kql
customMetrics
| where name == "cosmosdb_ru_usage"
| where timestamp > ago(5m)
| summarize avg(value)
| where avg_value > 80  // 80% do limite
```

## 🔒 Segurança

### Configurações de Segurança
- HTTPS obrigatório
- Managed Identity habilitada
- Key Vault para secrets (recomendado)
- Network Security Groups (produção)
- Private Endpoints (produção)

### Backup e Disaster Recovery
- Cosmos DB: Backup automático a cada 4 horas
- App Service: Deployment slots para zero-downtime
- Container Images: Versionamento com tags
- Infrastructure: Código versionado no Git

## 💰 Estimativas de Custo

### Ambiente de Desenvolvimento
| Serviço | SKU | Custo Mensal (USD) |
|---------|-----|-------------------|
| App Service | B1 | $15 |
| Cosmos DB | Serverless | $25 |
| Azure Search | Free | $0 |
| Container Registry | Basic | $5 |
| Application Insights | - | $10 |
| **Total** | | **~$55** |

### Ambiente de Produção
| Serviço | SKU | Custo Mensal (USD) |
|---------|-----|-------------------|
| App Service | P1v3 | $75 |
| Cosmos DB | 400 RU/s | $100 |
| Azure Search | Standard | $250 |
| Container Registry | Standard | $20 |
| Application Insights | - | $50 |
| Application Gateway | Standard | $25 |
| **Total** | | **~$520** |

## 🔧 Troubleshooting

### Problemas Comuns

#### 1. Falha no Deploy do Container
```bash
# Verificar logs do App Service
az webapp log tail --name "app-cloudflash-prod" --resource-group "rg-cloudflash-prod"

# Verificar configuração do container
az webapp config container show --name "app-cloudflash-prod" --resource-group "rg-cloudflash-prod"
```

#### 2. Erro de Conexão com Cosmos DB
```bash
# Verificar connection string
az webapp config appsettings list --name "app-cloudflash-prod" --resource-group "rg-cloudflash-prod"

# Testar conectividade
az cosmosdb check-name-exists --name "cosmos-cloudflash-prod"
```

#### 3. Problemas de Performance
```bash
# Verificar métricas do App Service
az monitor metrics list --resource "/subscriptions/{sub}/resourceGroups/rg-cloudflash-prod/providers/Microsoft.Web/sites/app-cloudflash-prod" --metric "CpuPercentage,MemoryPercentage"
```

### Logs Importantes
- Application Insights: Telemetria completa
- App Service: Stdout, stderr, IIS logs
- Cosmos DB: Request metrics, RU consumption
- Azure Search: Query logs, indexing status

## 📞 Suporte

### Contatos
- **Technical Lead**: team@cloudflash.com
- **DevOps**: devops@cloudflash.com
- **Incidentes**: incidents@cloudflash.com

### Recursos Úteis
- [Documentação Azure](https://docs.microsoft.com/azure/)
- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core/)
- [Cosmos DB Best Practices](https://docs.microsoft.com/azure/cosmos-db/best-practice-dotnet)
- [Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)

---

**Última atualização**: Agosto 2025
**Versão**: 1.0.0
