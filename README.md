# CloudFlash API

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat&logo=dotnet)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=flat&logo=docker)
![Azure](https://img.shields.io/badge/Azure-Deployed-0078D4?style=flat&logo=microsoft-azure)

Uma API REST para consulta de disponibilidade de filmes e séries em plataformas de streaming.

## 🎯 Objetivo

Resolver o problema de usuários que precisam pesquisar em múltiplas plataformas para encontrar um título específico. A CloudFlash consolida informações de streaming e automatiza a busca, proporcionando uma experiência mais ágil e eficiente.

## 🚀 Features

- ✅ Busca unificada de filmes e séries
- ✅ Integração com TMDB (The Movie Database)
- ✅ Informações de disponibilidade em streaming
- ✅ API REST com documentação Swagger
- ✅ Containerização com Docker
- ✅ Deploy automatizado no Azure
- ✅ Monitoramento e observabilidade
- ✅ Health checks
- ⚡ **NOVO**: Atualizado para .NET 9

## 🏗️ Arquitetura

A solução utiliza Clean Architecture com os seguintes componentes:

- **API Layer**: ASP.NET Core 9.0 com controllers RESTful
- **Application Layer**: CQRS com MediatR, DTOs e handlers
- **Domain Layer**: Entidades de negócio e interfaces
- **Infrastructure Layer**: Repositórios, serviços externos e data access

### Tecnologias

- **Backend**: C# 13, ASP.NET Core 9.0
- **Banco de Dados**: Azure Cosmos DB (NoSQL)
- **Busca**: Azure Cognitive Search / Elasticsearch
- **Cache**: Redis (futuro)
- **Monitoramento**: Application Insights, Serilog
- **Containerização**: Docker
- **Cloud**: Microsoft Azure
- **CI/CD**: GitHub Actions
- **IaC**: Azure Bicep

## 📖 Endpoints

### Buscar Títulos
```http
GET /api/v1/titles?query=batman&page=1&pageSize=20&platform=netflix&genre=action
```

**Resposta:**
```json
{
  "results": [
    {
      "id": "12345",
      "name": "Batman Begins",
      "originalName": "Batman Begins",
      "overview": "Um jovem Bruce Wayne viaja para o Oriente...",
      "posterPath": "https://image.tmdb.org/t/p/w500/poster.jpg",
      "releaseDate": "2005-06-15T00:00:00Z",
      "voteAverage": 8.2,
      "type": "Movie",
      "streamingAvailabilities": [
        {
          "platform": "Netflix",
          "region": "BR",
          "type": "Subscription",
          "quality": "HD",
          "link": "https://netflix.com/title/12345"
        }
      ]
    }
  ],
  "totalResults": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

### Obter Título por ID
```http
GET /api/v1/titles/{id}
```

### Health Check
```http
GET /api/v1/health
```

## 🚀 Executando Localmente

### Pré-requisitos
- .NET 9.0 SDK
- Docker Desktop
- Conta no TMDB para API key

### 1. Clone o repositório
```bash
git clone https://github.com/heliogatts/cineflash.git
cd cineflash
```

### 2. Configure as variáveis de ambiente
```bash
# Copie o arquivo de configuração
cp src/CloudFlash.API/appsettings.json src/CloudFlash.API/appsettings.Development.json

# Edite o arquivo e adicione sua TMDB API key
```

### 3. Usando Docker Compose (Recomendado)
```bash
# Inicia todos os serviços (API, Elasticsearch, Cosmos DB Emulator)
docker-compose up -d

# A API estará disponível em http://localhost:8080
# Swagger UI em http://localhost:8080
```

### 4. Usando .NET CLI
```bash
# Restaurar dependências
dotnet restore

# Executar a API
dotnet run --project src/CloudFlash.API

# A API estará disponível em https://localhost:7001
```

## 🧪 Testes

```bash
# Executar todos os testes
dotnet test

# Executar com cobertura
dotnet test --collect:"XPlat Code Coverage"
```

## ⚡ Migração para .NET 9

O projeto foi recentemente atualizado para .NET 9, trazendo melhorias de performance e novas funcionalidades.

### O que foi atualizado:
- ✅ Target Framework: `net8.0` → `net9.0`
- ✅ Pacotes NuGet atualizados para versões compatíveis
- ✅ Dockerfile atualizado para imagens .NET 9
- ✅ Todas as dependências compatibilizadas

### Conhecendo problemas pós-migração:
- ⚠️ **Health Check CosmosDB**: Temporariamente desabilitado devido a mudanças na API
- ⚠️ **AutoMapper**: Mantido na versão 12.0.1 para compatibilidade

### Verificação da instalação:
```bash
# Verificar versão do .NET
dotnet --version  # Deve mostrar 9.x.x

# Compilar o projeto
dotnet build

# Executar testes
dotnet test
```

Para detalhes completos da migração, consulte [UPGRADE_NET9.md](UPGRADE_NET9.md).

## 🚀 Deploy

### Azure (Automático via GitHub Actions)

1. **Configure os secrets no GitHub:**
   - `AZURE_CREDENTIALS`: Service Principal do Azure
   - `AZURE_SUBSCRIPTION_ID`: ID da subscription
   - `AZURE_RG`: Nome do Resource Group
   - `TMDB_API_KEY`: Chave da API do TMDB
   - `ACR_USERNAME`: Usuário do Container Registry
   - `ACR_PASSWORD`: Senha do Container Registry

2. **Deploy da infraestrutura:**
   ```bash
   # Commit com a mensagem especial para deploy de infraestrutura
   git commit -m "feat: add new feature [deploy-infra]"
   git push origin main
   ```

3. **Deploy da aplicação:**
   ```bash
   # Push para main automaticamente faz o deploy
   git push origin main
   ```

### Manual

```bash
# 1. Build da imagem Docker
docker build -t cloudflash .

# 2. Deploy para Azure Container Registry
az acr build --registry myregistry --image cloudflash:latest .

# 3. Deploy para App Service
az webapp config container set \
  --name myapp \
  --resource-group myrg \
  --docker-custom-image-name myregistry.azurecr.io/cloudflash:latest
```

## 📊 Monitoramento

### Application Insights
- Métricas de performance
- Logs de aplicação
- Rastreamento de dependências
- Alertas personalizados

### Health Checks
- `/health` - Status geral da aplicação
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

### Logs
```bash
# Visualizar logs da aplicação
docker-compose logs -f cloudflash-api

# Logs do Azure
az webapp log tail --name myapp --resource-group myrg
```

## 🤝 Contribuindo

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/nova-feature`)
3. Commit suas mudanças (`git commit -m 'Add: nova feature'`)
4. Push para a branch (`git push origin feature/nova-feature`)
5. Abra um Pull Request

### Padrões de Commit
- `feat:` Nova funcionalidade
- `fix:` Correção de bug
- `docs:` Documentação
- `style:` Formatação
- `refactor:` Refatoração
- `test:` Testes
- `chore:` Manutenção

## 📝 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para detalhes.

## 📞 Suporte

- **Documentação**: [docs/architecture.md](docs/architecture.md)
- **Issues**: [GitHub Issues](https://github.com/heliogatts/cineflash/issues)
- **Email**: team@cloudflash.com

## 🗺️ Roadmap

### Próximos Passos (Pós .NET 9)
- [ ] Corrigir Health Check do CosmosDB para API v9.0
- [ ] Atualizar AutoMapper para versão mais recente
- [ ] Testes completos de regressão pós-migração
- [ ] Otimizações específicas do .NET 9

### Funcionalidades Futuras
- [ ] Cache inteligente com Redis
- [ ] API de recomendações baseada em ML
- [ ] Integração com mais plataformas (Prime Video, Disney+, etc.)
- [ ] Autenticação de usuários
- [ ] Favoritos e watchlists
- [ ] Notificações de novos conteúdos
- [ ] API GraphQL
- [ ] Mobile SDK

---

Desenvolvido com ❤️ para simplificar a busca por entretenimento!
