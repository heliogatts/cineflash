param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory = $false)]
    [string]$ResourceGroupName = "rg-cloudflash-$Environment",
    
    [Parameter(Mandatory = $false)]
    [string]$ImageTag = "latest"
)

# Verificar se Docker está rodando
$dockerStatus = docker info 2>$null
if (-not $dockerStatus) {
    Write-Error "Docker não está rodando. Inicie o Docker Desktop."
    exit 1
}

# Carregar outputs da infraestrutura
$outputFile = "$PSScriptRoot/../.azure-outputs-$Environment.json"
if (-not (Test-Path $outputFile)) {
    Write-Error "Arquivo de outputs não encontrado: $outputFile"
    Write-Host "Execute primeiro: ./deploy-infrastructure.ps1 -Environment $Environment" -ForegroundColor Yellow
    exit 1
}

$infrastructureOutputs = Get-Content $outputFile | ConvertFrom-Json
$outputs = $infrastructureOutputs.properties.outputs

$registryServer = $outputs.containerRegistryLoginServer.value
$registryName = $registryServer.Split('.')[0]
$appServiceName = "app-cloudflash-$Environment"

Write-Host "Iniciando deploy da aplicação..." -ForegroundColor Green
Write-Host "Container Registry: $registryServer" -ForegroundColor Cyan
Write-Host "App Service: $appServiceName" -ForegroundColor Cyan

# Fazer login no Container Registry
Write-Host "Fazendo login no Azure Container Registry..." -ForegroundColor Yellow
az acr login --name $registryName

if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha no login do Container Registry"
    exit 1
}

# Build e push da imagem Docker
Write-Host "Fazendo build da imagem Docker..." -ForegroundColor Yellow
$imageName = "cloudflash"
$fullImageName = "$registryServer/${imageName}:$ImageTag"

# Navegar para o diretório raiz do projeto
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot

try {
    # Build da imagem
    docker build -t $fullImageName .
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha no build da imagem Docker"
        exit 1
    }
    
    # Push da imagem
    Write-Host "Fazendo push da imagem para o Container Registry..." -ForegroundColor Yellow
    docker push $fullImageName
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha no push da imagem Docker"
        exit 1
    }
    
    # Deploy para App Service
    Write-Host "Fazendo deploy para o App Service..." -ForegroundColor Yellow
    az webapp config container set `
        --name $appServiceName `
        --resource-group $ResourceGroupName `
        --docker-custom-image-name $fullImageName `
        --docker-registry-server-url "https://$registryServer"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha no deploy para App Service"
        exit 1
    }
    
    # Restart do App Service para garantir que a nova imagem seja carregada
    Write-Host "Reiniciando App Service..." -ForegroundColor Yellow
    az webapp restart --name $appServiceName --resource-group $ResourceGroupName
    
    Write-Host "Deploy da aplicação concluído com sucesso!" -ForegroundColor Green
    
    # Obter URL do App Service
    $appUrl = $outputs.appServiceUrl.value
    Write-Host "Aplicação disponível em: $appUrl" -ForegroundColor Cyan
    
    # Aguardar um pouco e fazer health check
    Write-Host "Aguardando aplicação inicializar..." -ForegroundColor Yellow
    Start-Sleep -Seconds 30
    
    try {
        $healthResponse = Invoke-RestMethod -Uri "$appUrl/api/v1/health" -Method Get -TimeoutSec 10
        Write-Host "Health Check: ✅ SUCESSO" -ForegroundColor Green
        Write-Host "Status: $($healthResponse.status)" -ForegroundColor Green
        Write-Host "Versão: $($healthResponse.version)" -ForegroundColor Green
    }
    catch {
        Write-Warning "Health Check falhou - a aplicação pode ainda estar inicializando"
        Write-Host "Verifique manualmente em: $appUrl/api/v1/health" -ForegroundColor Yellow
    }
    
    Write-Host "`nPróximos passos:" -ForegroundColor Yellow
    Write-Host "1. Acesse a documentação da API: $appUrl" -ForegroundColor White
    Write-Host "2. Teste os endpoints: $appUrl/api/v1/titles?query=batman" -ForegroundColor White
    Write-Host "3. Configure monitoramento no Application Insights" -ForegroundColor White
    
}
finally {
    Pop-Location
}
