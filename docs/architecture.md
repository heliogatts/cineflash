# CloudFlash API - Documentação da Arquitetura

## Visão Geral

A CloudFlash é uma API REST desenvolvida em C# com ASP.NET Core 8.0, projetada para resolver o problema de busca de filmes e séries em múltiplas plataformas de streaming. A arquitetura segue os princípios de Clean Architecture e Domain-Driven Design (DDD), implementada como uma solução cloud-native no Microsoft Azure.

## Arquitetura da Solução

### Modelo C4

#### Nível 1 - Context Diagram
```
[Usuário] → [CloudFlash API] → [TMDB API]
                ↓
        [Azure Cosmos DB]
        [Azure Search]
        [Application Insights]
```

#### Nível 2 - Container Diagram
```
[Web Browser/Mobile App]
         ↓
[API Gateway (Azure App Gateway)]
         ↓
[CloudFlash API Container]
    ↓         ↓         ↓
[Cosmos DB] [Search] [External APIs]
```

#### Nível 3 - Component Diagram
```
CloudFlash API Container:
┌─────────────────────────────────────┐
│  Controllers Layer                  │
│  - TitlesController                 │
│  - HealthController                 │
├─────────────────────────────────────┤
│  Application Layer                  │
│  - Query Handlers                   │
│  - DTOs                            │
│  - AutoMapper Profiles             │
├─────────────────────────────────────┤
│  Domain Layer (Core)               │
│  - Entities                        │
│  - Interfaces                      │
│  - Business Rules                  │
├─────────────────────────────────────┤
│  Infrastructure Layer              │
│  - Repositories                    │
│  - External Services               │
│  - Data Access                     │
└─────────────────────────────────────┘
```

### Tecnologias Utilizadas

#### Backend
- **Framework**: ASP.NET Core 8.0
- **Linguagem**: C# 12
- **Arquitetura**: Clean Architecture com CQRS pattern
- **Mediação**: MediatR para implementação do CQRS
- **Mapeamento**: AutoMapper para transformação de objetos
- **Validação**: FluentValidation

#### Banco de Dados
- **Principal**: Azure Cosmos DB (NoSQL)
  - Database: CloudFlashDB
  - Container: Titles
  - Partition Key: /id
  - Consistency Level: Session

#### Busca e Indexação
- **Motor de Busca**: Azure Cognitive Search
  - Índices otimizados para busca de texto
  - Suporte a busca fuzzy
  - Filtros por gênero, plataforma e tipo

#### Integração Externa
- **APIs de Terceiros**:
  - TMDB (The Movie Database) API
  - JustWatch API (futuro)
  - Streaming Platform APIs (futuro)

#### Observabilidade
- **Logs**: Serilog com Application Insights
- **Métricas**: Application Insights
- **Health Checks**: ASP.NET Core Health Checks
- **Monitoramento**: Azure Monitor

#### DevOps e Infraestrutura
- **Containerização**: Docker
- **Orquestração**: Azure Container Instances / App Service
- **CI/CD**: GitHub Actions
- **IaC**: Azure Bicep
- **Registry**: Azure Container Registry

## Estrutura do Projeto

```
CloudFlash/
├── src/
│   ├── CloudFlash.API/              # Camada de apresentação
│   │   ├── Controllers/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── CloudFlash.Application/      # Camada de aplicação
│   │   ├── DTOs/
│   │   ├── Handlers/
│   │   ├── Queries/
│   │   └── Mappings/
│   ├── CloudFlash.Core/            # Camada de domínio
│   │   ├── Entities/
│   │   └── Interfaces/
│   └── CloudFlash.Infrastructure/  # Camada de infraestrutura
│       ├── Repositories/
│       └── Services/
├── tests/                         # Testes
├── docs/                         # Documentação
├── infra/                       # Infraestrutura como código
│   └── bicep/
├── .github/workflows/          # Pipelines CI/CD
├── docker-compose.yml         # Desenvolvimento local
├── Dockerfile                # Container da aplicação
└── CloudFlash.sln           # Solution file
```

## Fluxo de Dados

### Busca de Títulos
1. **Requisição**: Cliente faz GET para `/api/v1/titles?query=batman`
2. **Validação**: Controller valida parâmetros de entrada
3. **Query**: MediatR envia SearchTitlesQuery para handler
4. **Busca Local**: Handler consulta Azure Search primeiro
5. **Busca Externa**: Se resultados insuficientes, consulta TMDB API
6. **Persistência**: Novos títulos são salvos no Cosmos DB
7. **Indexação**: Títulos são indexados no Azure Search
8. **Resposta**: Retorna lista paginada de títulos com disponibilidade

### Armazenamento de Dados
- **Cosmos DB**: Armazena dados mestres dos títulos
- **Azure Search**: Índice otimizado para busca textual
- **Cache**: Application Insights para métricas e telemetria

## Segurança

### Autenticação e Autorização
- API Keys para acesso externo
- Azure AD integration (futuro)
- Rate limiting por IP/usuário

### Proteção de Dados
- HTTPS obrigatório
- Validação de entrada rigorosa
- Sanitização de queries
- Secrets gerenciados pelo Azure Key Vault

### Compliance
- LGPD/GDPR compliance
- Logs de auditoria
- Monitoramento de segurança

## Escalabilidade

### Horizontal
- Azure App Service com auto-scaling
- Cosmos DB com particionamento eficiente
- Azure Search com réplicas

### Vertical
- Planos de App Service escalonáveis
- RU/s dinâmicas no Cosmos DB
- Otimização de consultas

## Monitoramento e Observabilidade

### Métricas
- Latência de resposta da API
- Taxa de erro por endpoint
- Utilização de recursos
- Performance do banco de dados

### Logs
- Logs estruturados com Serilog
- Correlação de requisições
- Logs de erro e exceções
- Auditoria de acesso

### Alertas
- Falhas de saúde da aplicação
- Limite de RU/s do Cosmos DB
- Erros HTTP 5xx
- Latência elevada

## Disaster Recovery

### Backup
- Cosmos DB: Backup automático a cada 4 horas
- Código: Versionado no GitHub
- Configurações: Armazenadas no Azure DevOps

### Alta Disponibilidade
- Multi-region deployment (futuro)
- Failover automático
- Health checks contínuos

## Performance

### Otimizações
- Cache de resultados frequentes
- Paginação eficiente
- Compressão de resposta
- CDN para assets estáticos

### Benchmarks
- Tempo de resposta < 200ms (95th percentile)
- Throughput > 1000 req/s
- Disponibilidade > 99.9%

## Roadmap

### Fase 1 (Atual)
- ✅ API básica de busca
- ✅ Integração com TMDB
- ✅ Deploy no Azure
- ✅ CI/CD básico

### Fase 2
- [ ] Cache inteligente
- [ ] API de recomendações
- [ ] Integração com mais plataformas
- [ ] Autenticação de usuários

### Fase 3
- [ ] Machine Learning para recomendações
- [ ] Multi-region deployment
- [ ] API GraphQL
- [ ] Mobile SDK

## Custos Estimados (Mensal)

### Desenvolvimento
- App Service (B1): ~$15
- Cosmos DB (Serverless): ~$25
- Azure Search (Free): $0
- Container Registry: ~$5
- **Total**: ~$45/mês

### Produção
- App Service (P1v3): ~$75
- Cosmos DB (Provisioned): ~$100
- Azure Search (Standard): ~$250
- Application Gateway: ~$25
- **Total**: ~$450/mês
