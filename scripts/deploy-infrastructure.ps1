param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory = $false)]
    [string]$Location = "brazilsouth",
    
    [Parameter(Mandatory = $true)]
    [string]$TmdbApiKey,
    
    [Parameter(Mandatory = $false)]
    [string]$ResourceGroupName = "rg-cloudflash-$Environment",
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId
)

# Verificar se Azure CLI está instalado e autenticado
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI não está instalado. Instale em: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
}

# Verificar versão mínima do Azure CLI
$azVersion = az version | ConvertFrom-Json
$minVersion = [Version]"2.60.0"
$currentVersion = [Version]$azVersion.'azure-cli'
if ($currentVersion -lt $minVersion) {
    Write-Warning "Azure CLI versão $($azVersion.'azure-cli') detectada. Recomendado: $minVersion ou superior"
    Write-Host "   Execute: az upgrade" -ForegroundColor Yellow
}

# Login no Azure se necessário
$loginStatus = az account show --query "id" -o tsv 2>$null
if (-not $loginStatus) {
    Write-Host "Fazendo login no Azure..." -ForegroundColor Yellow
    az login
}

# Definir subscription se fornecida
if ($SubscriptionId) {
    Write-Host "Definindo subscription: $SubscriptionId" -ForegroundColor Green
    az account set --subscription $SubscriptionId
}

# Verificar se o resource group existe
$rgExists = az group exists --name $ResourceGroupName
if ($rgExists -eq "false") {
    Write-Host "Criando Resource Group: $ResourceGroupName" -ForegroundColor Green
    az group create --name $ResourceGroupName --location $Location --tags "project=CloudFlash" "environment=$Environment"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha ao criar Resource Group"
        exit 1
    }
}

# Deploy da infraestrutura usando Bicep
Write-Host "Iniciando deploy da infraestrutura..." -ForegroundColor Green

$bicepFile = "$PSScriptRoot/../infra/bicep/main.bicep"
$deploymentName = "cloudflash-infra-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

# Determinar SKU baseado no ambiente
$appServiceSku = switch ($Environment) {
    "dev" { "B1" }
    "staging" { "S1" }
    "prod" { "P1v3" }
}

# Validar se o arquivo Bicep existe
if (-not (Test-Path $bicepFile)) {
    Write-Error "Arquivo Bicep não encontrado: $bicepFile"
    exit 1
}

Write-Host "Validando template Bicep..." -ForegroundColor Yellow
az deployment group validate `
    --resource-group $ResourceGroupName `
    --template-file $bicepFile `
    --parameters `
    baseName="cloudflash" `
    environment=$Environment `
    location=$Location `
    appServicePlanSku=$appServiceSku `
    tmdbApiKey=$TmdbApiKey

if ($LASTEXITCODE -ne 0) {
    Write-Error "Validação do template Bicep falhou"
    exit 1
}

Write-Host "✅ Template Bicep validado com sucesso!" -ForegroundColor Green

$deploymentResult = az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file $bicepFile `
    --name $deploymentName `
    --parameters `
    baseName="cloudflash" `
    environment=$Environment `
    location=$Location `
    appServicePlanSku=$appServiceSku `
    tmdbApiKey=$TmdbApiKey `
    --output json

if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha no deploy da infraestrutura"
    exit 1
}

# Parse dos outputs
$outputs = $deploymentResult | ConvertFrom-Json | Select-Object -ExpandProperty properties | Select-Object -ExpandProperty outputs

Write-Host "Deploy da infraestrutura concluído com sucesso!" -ForegroundColor Green
Write-Host "Outputs:" -ForegroundColor Yellow
Write-Host "  App Service URL: $($outputs.appServiceUrl.value)" -ForegroundColor Cyan
Write-Host "  Cosmos DB Endpoint: $($outputs.cosmosDbEndpoint.value)" -ForegroundColor Cyan
Write-Host "  Container Registry: $($outputs.containerRegistryLoginServer.value)" -ForegroundColor Cyan
Write-Host "  Search Service: $($outputs.searchServiceEndpoint.value)" -ForegroundColor Cyan

# Salvar outputs em arquivo para uso posterior
$outputFile = "$PSScriptRoot/../.azure-outputs-$Environment.json"
$deploymentResult | Out-File -FilePath $outputFile -Encoding UTF8
Write-Host "Outputs salvos em: $outputFile" -ForegroundColor Green

Write-Host "`nPróximos passos:" -ForegroundColor Yellow
Write-Host "1. Execute: ./deploy-application.ps1 -Environment $Environment" -ForegroundColor White
Write-Host "2. Configure os secrets do GitHub Actions se necessário" -ForegroundColor White
Write-Host "3. Teste a aplicação em: $($outputs.appServiceUrl.value)" -ForegroundColor White
