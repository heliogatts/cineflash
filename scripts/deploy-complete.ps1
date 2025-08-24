# Script de deploy completo CloudFlash
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory = $true)]
    [string]$TmdbApiKey,
    
    [Parameter(Mandatory = $false)]
    [string]$Location = "brazilsouth",
    
    [Parameter(Mandatory = $false)]
    [string]$ResourceGroupName = "rg-cloudflash-$Environment",
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory = $false)]
    [string]$ImageTag = "latest",
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipInfrastructure,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipApplication,
    
    [Parameter(Mandatory = $false)]
    [switch]$DryRun,

    [Parameter(Mandatory = $false)]
    [string]$HealthCheckPath = "/api/v1/health"
)

Write-Host "🚀 CloudFlash - Deploy Completo" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host "Ambiente: $Environment" -ForegroundColor Cyan
Write-Host "Região: $Location" -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "🔍 MODO DRY RUN - Apenas validações serão executadas" -ForegroundColor Yellow
}

# Função para logging
function Write-Step {
    param([string]$message, [string]$color = "Yellow")
    Write-Host "`n🔄 $message" -ForegroundColor $color
}

function Write-Success {
    param([string]$message)
    Write-Host "✅ $message" -ForegroundColor Green
}

function Write-Failure {
    param([string]$message)
    Write-Host "❌ $message" -ForegroundColor Red
}

# Validar pré-requisitos
Write-Step "Validando pré-requisitos"
$validationScript = Join-Path $PSScriptRoot "validate-environment.ps1"
if (Test-Path $validationScript) {
    & $validationScript
    if ($LASTEXITCODE -ne 0) {
        Write-Failure "Validação do ambiente falhou"
        exit 1
    }
} else {
    Write-Failure "Script de validação não encontrado"
    exit 1
}

Write-Success "Pré-requisitos validados"

# Estimativa de tempo
$estimatedTime = switch ($Environment) {
    "dev" { "15-20 minutos" }
    "staging" { "20-25 minutos" }
    "prod" { "25-30 minutos" }
}

Write-Host "`n⏱️  Tempo estimado: $estimatedTime" -ForegroundColor Cyan

if (-not $DryRun) {
    Write-Host "Pressione qualquer tecla para continuar ou Ctrl+C para cancelar..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

# Deploy da infraestrutura
if (-not $SkipInfrastructure) {
    Write-Step "Executando deploy da infraestrutura"
    
    $infraParams = @(
        "-Environment", $Environment,
        "-Location", $Location,
        "-TmdbApiKey", $TmdbApiKey,
        "-ResourceGroupName", $ResourceGroupName
    )
    
    if ($SubscriptionId) {
        $infraParams += "-SubscriptionId", $SubscriptionId
    }
    
    if ($DryRun) {
        Write-Host "DRY RUN: Executaria deploy-infrastructure.ps1 com parâmetros: $($infraParams -join ' ')" -ForegroundColor Yellow
    } else {
        $infraScript = Join-Path $PSScriptRoot "deploy-infrastructure.ps1"
        & $infraScript @infraParams
        
        if ($LASTEXITCODE -ne 0) {
            Write-Failure "Deploy da infraestrutura falhou"
            exit 1
        }
    }
    
    Write-Success "Infraestrutura deployada com sucesso"
} else {
    Write-Host "⏭️  Pulando deploy da infraestrutura" -ForegroundColor Gray
}

# Deploy da aplicação
if (-not $SkipApplication) {
    Write-Step "Executando deploy da aplicação"
    
    $appParams = @(
        "-Environment", $Environment,
        "-ResourceGroupName", $ResourceGroupName,
        "-ImageTag", $ImageTag
    )
    
    if ($DryRun) {
        Write-Host "DRY RUN: Executaria deploy-application.ps1 com parâmetros: $($appParams -join ' ')" -ForegroundColor Yellow
    } else {
        $appScript = Join-Path $PSScriptRoot "deploy-application.ps1"
        & $appScript @appParams
        
        if ($LASTEXITCODE -ne 0) {
            Write-Failure "Deploy da aplicação falhou"
            exit 1
        }
    }
    
    Write-Success "Aplicação deployada com sucesso"
} else {
    Write-Host "⏭️  Pulando deploy da aplicação" -ForegroundColor Gray
}

# Validação final
if (-not $DryRun) {
    Write-Step "Executando validação final"
    
    # Carregar outputs
    $outputFile = "$PSScriptRoot/../.azure-outputs-$Environment.json"
    if (Test-Path $outputFile) {
        $outputs = Get-Content $outputFile | ConvertFrom-Json | Select-Object -ExpandProperty properties | Select-Object -ExpandProperty outputs
        $appUrl = $outputs.appServiceUrl.value
        
        Write-Host "🔗 URL da aplicação: $appUrl" -ForegroundColor Cyan
        
        # Health check
        try {
            $healthUrl = "$appUrl$HealthCheckPath"
            $healthResponse = Invoke-RestMethod -Uri $healthUrl -Method Get -TimeoutSec 10
            Write-Success "Health check passou: $($healthResponse.status)"
        } catch {
            Write-Failure "Health check falhou: $($_.Exception.Message)"
        }
    }
}

# Relatório final
Write-Host "`n📊 Relatório do Deploy" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host "Ambiente: $Environment" -ForegroundColor White
Write-Host "Região: $Location" -ForegroundColor White
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor White

if (-not $DryRun) {
    Write-Host "`n🎉 Deploy concluído com sucesso!" -ForegroundColor Green
    
    Write-Host "`n📝 Próximos passos:" -ForegroundColor Yellow
    Write-Host "1. Teste a aplicação nos endpoints" -ForegroundColor White
    Write-Host "2. Configure monitoramento adicional se necessário" -ForegroundColor White
    Write-Host "3. Configure CI/CD pipeline se não foi feito ainda" -ForegroundColor White
    Write-Host "4. Configure backup e disaster recovery para produção" -ForegroundColor White
    
    if ($Environment -eq "prod") {
        Write-Host "`n🚨 Lembrete para Produção:" -ForegroundColor Red
        Write-Host "• Configure alertas de monitoramento" -ForegroundColor Yellow
        Write-Host "• Verifique política de backup" -ForegroundColor Yellow
        Write-Host "• Configure SSL/TLS se necessário" -ForegroundColor Yellow
        Write-Host "• Revise configurações de segurança" -ForegroundColor Yellow
    }
} else {
    Write-Host "`n✅ Validação DRY RUN concluída com sucesso!" -ForegroundColor Green
    Write-Host "Execute novamente sem -DryRun para realizar o deploy" -ForegroundColor Yellow
}
