# Setup do ambiente de desenvolvimento CloudFlash

Write-Host "🚀 Configurando ambiente de desenvolvimento CloudFlash..." -ForegroundColor Green

# Executar validação do ambiente primeiro
Write-Host "`n🔍 Executando validação do ambiente..." -ForegroundColor Yellow
$validationScript = Join-Path $PSScriptRoot "validate-environment.ps1"
if (Test-Path $validationScript) {
    & $validationScript -SkipOptional
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Validação do ambiente falhou. Corrija os erros antes de continuar."
        exit 1
    }
} else {
    Write-Warning "Script de validação não encontrado. Continuando com verificação básica..."
}

# Verificar pré-requisitos
Write-Host "`n📋 Verificando pré-requisitos..." -ForegroundColor Yellow

# .NET SDK
$dotnetVersion = dotnet --version 2>$null
if ($dotnetVersion) {
    Write-Host "✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
    # Verificar se é .NET 9 ou superior
    $requiredVersion = [Version]"9.0.0"
    $currentVersion = [Version]($dotnetVersion.Split('-')[0])
    if ($currentVersion -lt $requiredVersion) {
        Write-Host "⚠️  .NET 9.0 ou superior é requerido. Versão atual: $dotnetVersion" -ForegroundColor Yellow
        Write-Host "   Baixe .NET 9: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
    }
}
else {
    Write-Host "❌ .NET SDK não encontrado" -ForegroundColor Red
    Write-Host "   Baixe .NET 9: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
    exit 1
}

# Docker
$dockerVersion = docker --version 2>$null
if ($dockerVersion) {
    Write-Host "✅ Docker: $dockerVersion" -ForegroundColor Green
    # Verificar se Docker está rodando
    $dockerInfo = docker info 2>$null
    if (-not $dockerInfo) {
        Write-Host "⚠️  Docker está instalado mas não está rodando" -ForegroundColor Yellow
        Write-Host "   Inicie o Docker Desktop primeiro" -ForegroundColor Yellow
    }
}
else {
    Write-Host "❌ Docker não encontrado" -ForegroundColor Red
    Write-Host "   Baixe em: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
    exit 1
}

# Azure CLI
$azVersion = az --version 2>$null
if ($azVersion) {
    Write-Host "✅ Azure CLI instalado" -ForegroundColor Green
}
else {
    Write-Host "⚠️  Azure CLI não encontrado (opcional para desenvolvimento local)" -ForegroundColor Yellow
    Write-Host "   Baixe em: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" -ForegroundColor Yellow
}

# Navegar para o diretório do projeto
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot

try {
    # Restaurar dependências
    Write-Host "`n📦 Restaurando dependências..." -ForegroundColor Yellow
    dotnet restore CloudFlash.sln
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha ao restaurar dependências"
        exit 1
    }
    
    # Build do projeto
    Write-Host "`n🔨 Fazendo build do projeto..." -ForegroundColor Yellow
    dotnet build CloudFlash.sln --configuration Debug

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha ao fazer build do projeto"
        exit 1
    }

    # Executar testes para verificar se tudo está funcionando
    Write-Host "`n🧪 Executando testes..." -ForegroundColor Yellow
    dotnet test CloudFlash.sln --configuration Debug --verbosity minimal

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Todos os testes passaram!" -ForegroundColor Green
    }
    else {
        Write-Warning "Alguns testes falharam. Verifique os logs acima."
    }
    
    # Verificar se arquivo de configuração existe
    $configFile = "src/CloudFlash.API/appsettings.Development.json"
    if (-not (Test-Path $configFile)) {
        Write-Host "`n⚙️  Criando arquivo de configuração local..." -ForegroundColor Yellow
        
        $devConfig = @{
            "Logging"             = @{
                "LogLevel" = @{
                    "Default"              = "Information"
                    "Microsoft.AspNetCore" = "Warning"
                }
            }
            "AllowedHosts"        = "*"
            "ConnectionStrings"   = @{
                "CosmosDb"      = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
                "Elasticsearch" = "http://localhost:9200"
            }
            "CosmosDb"            = @{
                "DatabaseName"  = "CloudFlashDB"
                "ContainerName" = "Titles"
            }
            "TmdbApiKey"          = "YOUR_TMDB_API_KEY_HERE"
            "ApplicationInsights" = @{
                "ConnectionString" = ""
            }
        }
        
        $devConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configFile -Encoding UTF8
        Write-Host "✅ Arquivo criado: $configFile" -ForegroundColor Green
        Write-Host "⚠️  IMPORTANTE: Configure sua TMDB API Key no arquivo!" -ForegroundColor Yellow
    }
    
    # Verificar se docker-compose existe
    if (Test-Path "docker-compose.yml") {
        Write-Host "`n🐳 Iniciando serviços com Docker Compose..." -ForegroundColor Yellow
        
        # Verificar se Docker está rodando
        $dockerInfo = docker info 2>$null
        if ($dockerInfo) {
            docker-compose up -d elasticsearch cosmos-emulator
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Serviços iniciados com sucesso!" -ForegroundColor Green
                Write-Host "   - Elasticsearch: http://localhost:9200" -ForegroundColor Cyan
                Write-Host "   - Cosmos DB Emulator: https://localhost:8081" -ForegroundColor Cyan
                Write-Host "   - Kibana: http://localhost:5601" -ForegroundColor Cyan
            }
            else {
                Write-Warning "Falha ao iniciar alguns serviços com Docker Compose"
            }
        }
        else {
            Write-Warning "Docker não está rodando. Inicie o Docker Desktop primeiro."
        }
    }
    
    Write-Host "`n✅ Setup concluído com sucesso!" -ForegroundColor Green
    Write-Host "`n📝 Próximos passos:" -ForegroundColor Yellow
    Write-Host "1. Configure sua TMDB API Key em: $configFile" -ForegroundColor White
    Write-Host "2. Execute a aplicação: dotnet run --project src/CloudFlash.API" -ForegroundColor White
    Write-Host "3. Acesse: https://localhost:7001 (Swagger UI)" -ForegroundColor White
    Write-Host "4. Ou use Docker: docker-compose up -d" -ForegroundColor White
    
    Write-Host "`n🔗 Links úteis:" -ForegroundColor Yellow
    Write-Host "- TMDB API Key: https://www.themoviedb.org/settings/api" -ForegroundColor Cyan
    Write-Host "- Documentação: docs/architecture.md" -ForegroundColor Cyan
    Write-Host "- Issues: https://github.com/heliogatts/cineflash/issues" -ForegroundColor Cyan
    
}
finally {
    Pop-Location
}
