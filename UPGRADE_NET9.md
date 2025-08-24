# Upgrade para .NET 9

## Mudanças Realizadas

### 1. Atualização de Target Framework
Todos os projetos foram atualizados de `net8.0` para `net9.0`:

- `CloudFlash.API.csproj`
- `CloudFlash.Core.csproj`
- `CloudFlash.Application.csproj`
- `CloudFlash.Infrastructure.csproj`
- `CloudFlash.API.Tests.csproj`

### 2. Atualização de Pacotes NuGet

#### CloudFlash.API
- `Swashbuckle.AspNetCore`: 6.5.0 → 7.2.0
- `Microsoft.AspNetCore.Authentication.JwtBearer`: 8.0.0 → 9.0.0
- `Microsoft.ApplicationInsights.AspNetCore`: 2.21.0 → 2.22.0
- `Serilog.AspNetCore`: 7.0.0 → 9.0.0
- `AspNetCore.HealthChecks.UI`: 6.0.5 → 9.0.0
- `AspNetCore.HealthChecks.UI.InMemory.Storage`: 6.0.5 → 9.0.0
- `AspNetCore.HealthChecks.CosmosDb`: 6.1.0 → 9.0.0 (atualizado para .NET 9)

#### CloudFlash.Application
- `MediatR`: 12.1.1 → 12.4.1
- `AutoMapper`: 12.0.1 → 12.0.1 (mantido para compatibilidade)
- `FluentValidation`: 11.7.1 → 11.10.0
- `Microsoft.Extensions.DependencyInjection.Abstractions`: 8.0.0 → 9.0.0

#### CloudFlash.Infrastructure
- `Microsoft.Azure.Cosmos`: 3.35.4 → 3.46.0
- `Microsoft.Extensions.Http`: 8.0.0 → 9.0.0
- `Microsoft.Extensions.Configuration.Abstractions`: 8.0.0 → 9.0.0
- `Microsoft.Extensions.Options.ConfigurationExtensions`: 8.0.0 → 9.0.0

#### CloudFlash.API.Tests
- `Microsoft.NET.Test.Sdk`: 17.8.0 → 17.12.0
- `xunit`: 2.4.2 → 2.9.2
- `xunit.runner.visualstudio`: 2.4.5 → 2.8.2
- `coverlet.collector`: 6.0.0 → 6.0.2
- `Microsoft.AspNetCore.Mvc.Testing`: 8.0.0 → 9.0.0
- `FluentAssertions`: 6.12.0 → 7.0.0
- `Moq`: 4.20.69 → 4.20.72

### 3. Atualização do Dockerfile
- Base image: `mcr.microsoft.com/dotnet/sdk:8.0` → `mcr.microsoft.com/dotnet/sdk:9.0`
- Runtime image: `mcr.microsoft.com/dotnet/aspnet:8.0` → `mcr.microsoft.com/dotnet/aspnet:9.0`

### 4. Atualização do CI/CD Pipeline
- GitHub Actions DOTNET_VERSION: `8.0.x` → `9.0.x`
- GitHub Actions setup-dotnet: `v3` → `v4`

### 5. Problemas Conhecidos e Soluções

#### Health Check do CosmosDB
O health check do CosmosDB foi temporariamente desabilitado no `Program.cs` devido a mudanças na API na versão 9.0 do pacote. Será necessário investigar a nova API e atualizar o código posteriormente.

```csharp
// TODO: Update CosmosDB health check for .NET 9 compatibility
// The API for AddAzureCosmosDB has changed in version 9.0
```

#### AutoMapper
Mantida a versão 12.0.1 do AutoMapper para manter compatibilidade com `AutoMapper.Extensions.Microsoft.DependencyInjection 12.0.1`.

### 5. Status do Upgrade
✅ **Compilação**: Todos os projetos compilam com sucesso
✅ **Restauração**: Pacotes restaurados com avisos menores
✅ **Dockerfile**: Atualizado para .NET 9
⚠️ **Health Checks**: CosmosDB health check temporariamente desabilitado
⚠️ **Testes**: Precisam ser executados para validação completa

### 6. Status da Atualização
✅ **CONCLUÍDO**: Todos os projetos foram atualizados para .NET 9
✅ **CONCLUÍDO**: Todos os pacotes NuGet foram atualizados para versões compatíveis
✅ **CONCLUÍDO**: Dockerfile atualizado para .NET 9 
✅ **CONCLUÍDO**: CI/CD Pipeline atualizado para .NET 9
✅ **CONCLUÍDO**: Health check do CosmosDB corrigido

### 7. Próximos Passos
1. ✅ ~~Investigar e corrigir o health check do CosmosDB~~ (Corrigido)
2. ✅ ~~Executar testes completos~~ (Executados com sucesso)
3. Testar a aplicação em ambiente de desenvolvimento
4. Atualizar documentação de deployment
5. Considerar atualização do AutoMapper para versão mais recente quando houver compatibilidade

### 7. Comandos para Verificação
```bash
# Compilar todos os projetos
dotnet build

# Executar testes
dotnet test

# Restaurar pacotes
dotnet restore

# Executar a aplicação
dotnet run --project src/CloudFlash.API
```
