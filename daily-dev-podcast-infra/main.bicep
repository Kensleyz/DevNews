targetScope = 'resourceGroup'

@description('Environment name')
@allowed(['dev', 'prod'])
param environmentName string = 'prod'

@description('Azure region for most resources (southafricanorth recommended)')
param location string = resourceGroup().location

@description('Azure region for Azure OpenAI — must support GPT-4o (e.g. eastus, swedencentral)')
param openAiLocation string = 'eastus'

@description('Unique suffix derived from resource group id — keeps storage account name globally unique')
param resourceSuffix string = uniqueString(resourceGroup().id)

// ── Modules ──────────────────────────────────────────────────────────────────

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    environmentName: environmentName
    resourceSuffix: resourceSuffix
  }
}

module openai 'modules/openai.bicep' = {
  name: 'openai'
  params: {
    location: openAiLocation
    environmentName: environmentName
    resourceSuffix: resourceSuffix
  }
}

// cognitive already deployed — reference existing resource to avoid ARM redeploy errors
var cognitiveAccountName = 'cog-devpod-${environmentName}-${resourceSuffix}'

resource existingCognitive 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' existing = {
  name: cognitiveAccountName
}

module functions 'modules/functions.bicep' = {
  name: 'functions'
  params: {
    location: location
    environmentName: environmentName
    resourceSuffix: resourceSuffix
    storageAccountName: storage.outputs.storageAccountName
    storageConnectionString: storage.outputs.connectionString
    episodeContainerName: storage.outputs.episodeContainerName
    episodeTableName: storage.outputs.episodeTableName
    openAiEndpoint: openai.outputs.endpoint
    openAiAccountName: openai.outputs.accountName
    cognitiveAccountName: cognitiveAccountName
    cognitiveEndpoint: 'https://${location}.api.cognitive.microsoft.com/'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output functionAppUrl string = functions.outputs.functionAppUrl
output storageAccountName string = storage.outputs.storageAccountName
output openAiEndpoint string = openai.outputs.endpoint
output cognitiveEndpoint string = existingCognitive.properties.endpoint
