@description('Nome base para os recursos')
param baseName string = 'cloudflash'

@description('Localização dos recursos')
param location string = resourceGroup().location

@description('Ambiente (dev, staging, prod)')
param environment string = 'dev'

@description('SKU do App Service Plan')
param appServicePlanSku string = 'B1'

@description('Chave da API do TMDB')
@secure()
param tmdbApiKey string

// Variables
var resourceSuffix = '${baseName}-${environment}'
var appServicePlanName = 'asp-${resourceSuffix}'
var appServiceName = 'app-${resourceSuffix}'
var cosmosDbAccountName = 'cosmos-${resourceSuffix}'
var logAnalyticsWorkspaceName = 'law-${resourceSuffix}'
var applicationInsightsName = 'ai-${resourceSuffix}'
var containerRegistryName = 'acr${replace(resourceSuffix, '-', '')}'
var searchServiceName = 'search-${resourceSuffix}'

// Resource Group tags
var tags = {
  environment: environment
  project: 'CloudFlash'
  'cost-center': 'engineering'
}

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      searchVersion: 1
    }
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Container Registry
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: containerRegistryName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
    policies: {
      quarantinePolicy: {
        status: 'disabled'
      }
      trustPolicy: {
        type: 'Notary'
        status: 'disabled'
      }
      retentionPolicy: {
        days: 7
        status: 'disabled'
      }
    }
  }
}

// Cosmos DB Account
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmosDbAccountName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    databaseAccountOfferType: 'Standard'
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    backupPolicy: {
      type: 'Periodic'
      periodicModeProperties: {
        backupIntervalInMinutes: 240
        backupRetentionIntervalInHours: 8
        backupStorageRedundancy: 'Local'
      }
    }
  }
}

// Cosmos DB Database
resource cosmosDbDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  parent: cosmosDbAccount
  name: 'CloudFlashDB'
  properties: {
    resource: {
      id: 'CloudFlashDB'
    }
  }
}

// Cosmos DB Container
resource cosmosDbContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: cosmosDbDatabase
  name: 'Titles'
  properties: {
    resource: {
      id: 'Titles'
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
    }
  }
}

// Azure Search Service
resource searchService 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchServiceName
  location: location
  tags: tags
  sku: {
    name: 'free'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
    networkRuleSet: {
      ipRules: []
    }
  }
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: appServicePlanSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: appServiceName
  location: location
  tags: tags
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOCKER|${containerRegistry.name}.azurecr.io/cloudflash:latest'
      appCommandLine: ''
      appSettings: [
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: 'https://${containerRegistry.name}.azurecr.io'
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_USERNAME'
          value: containerRegistry.name
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_PASSWORD'
          value: containerRegistry.listCredentials().passwords[0].value
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'prod' ? 'Production' : 'Development'
        }
        {
          name: 'ConnectionStrings__CosmosDb'
          value: cosmosDbAccount.listConnectionStrings().connectionStrings[0].connectionString
        }
        {
          name: 'ConnectionStrings__Elasticsearch'
          value: 'https://${searchService.name}.search.windows.net'
        }
        {
          name: 'CosmosDb__DatabaseName'
          value: 'CloudFlashDB'
        }
        {
          name: 'CosmosDb__ContainerName'
          value: 'Titles'
        }
        {
          name: 'TmdbApiKey'
          value: tmdbApiKey
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
      ]
      connectionStrings: []
      metadata: [
        {
          name: 'CURRENT_STACK'
          value: 'dotnetcore'
        }
      ]
      alwaysOn: environment == 'prod'
      http20Enabled: true
      functionAppScaleLimit: 0
      minimumElasticInstanceCount: 0
    }
    httpsOnly: true
    redundancyMode: 'None'
    storageAccountRequired: false
    keyVaultReferenceIdentity: 'SystemAssigned'
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Outputs
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output cosmosDbEndpoint string = cosmosDbAccount.properties.documentEndpoint
output applicationInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
output containerRegistryLoginServer string = containerRegistry.properties.loginServer
output searchServiceEndpoint string = 'https://${searchService.name}.search.windows.net'
