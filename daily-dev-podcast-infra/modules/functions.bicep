@description('Azure region')
param location string

@description('Environment name')
param environmentName string

@description('Unique suffix — same value used across all modules')
param resourceSuffix string

// Storage
param storageAccountName string
@secure()
param storageConnectionString string
param episodeContainerName string
param episodeTableName string

// OpenAI
param openAiEndpoint string
param openAiAccountName string

// Cognitive Services
param cognitiveAccountName string
param cognitiveEndpoint string

// App Service Plan name doesn't need global uniqueness — scoped to RG
var appServicePlanName = 'asp-devpod-${environmentName}'
// Function App names must be globally unique across Azure
var functionAppName = 'func-devpod-${environmentName}-${resourceSuffix}'

// ── Resolve existing resources to call listKeys ───────────────────────────────

resource openAiAccount 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' existing = {
  name: openAiAccountName
}

resource cognitiveAccount 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' existing = {
  name: cognitiveAccountName
}

// ── Consumption App Service Plan ──────────────────────────────────────────────

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'Y1'  // Consumption plan — free tier: 1M executions/month
    tier: 'Dynamic'
  }
  properties: {}
  tags: {
    project: 'daily-dev-podcast'
    environment: environmentName
  }
}

// ── Function App ──────────────────────────────────────────────────────────────

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      use32BitWorkerProcess: false
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        // Runtime
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        // Storage — used by Functions runtime + podcast storage
        {
          name: 'AzureWebJobsStorage'
          value: storageConnectionString
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storageConnectionString
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName)
        }
        {
          name: 'PodcastStorage__AccountName'
          value: storageAccountName
        }
        {
          name: 'PodcastStorage__EpisodeContainer'
          value: episodeContainerName
        }
        {
          name: 'PodcastStorage__EpisodeTable'
          value: episodeTableName
        }
        // Azure OpenAI
        {
          name: 'AzureOpenAI__Endpoint'
          value: openAiEndpoint
        }
        {
          name: 'AzureOpenAI__Key'
          value: openAiAccount.listKeys().key1
        }
        {
          name: 'AzureOpenAI__DeploymentName'
          value: 'gpt-4o'
        }
        // Azure Cognitive Services (TTS)
        {
          name: 'AzureSpeech__Endpoint'
          value: cognitiveEndpoint
        }
        {
          name: 'AzureSpeech__Key'
          value: cognitiveAccount.listKeys().key1
        }
        {
          name: 'AzureSpeech__Region'
          value: location
        }
        // Nightly timer — 02:00 SAST = 00:00 UTC
        {
          name: 'NightlyJob__CronSchedule'
          value: '0 0 0 * * *'
        }
      ]
    }
  }
  tags: {
    project: 'daily-dev-podcast'
    environment: environmentName
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output functionAppUrl string = 'https://${functionApp.properties.defaultHostName}'
output functionAppName string = functionApp.name
