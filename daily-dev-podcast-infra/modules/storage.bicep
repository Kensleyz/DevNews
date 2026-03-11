@description('Azure region')
param location string

@description('Environment name')
param environmentName string

@description('Unique suffix for globally unique storage account name')
param resourceSuffix string

// Storage account names: 3-24 chars, lowercase alphanumeric only
var storageAccountName = 'stdevpod${resourceSuffix}'
var episodeContainerName = 'episodes'
var episodeTableName = 'Episodes'

// ── Storage Account ───────────────────────────────────────────────────────────

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS' // Cheapest — fine for personal use
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
  tags: {
    project: 'daily-dev-podcast'
    environment: environmentName
  }
}

// ── Blob Service + Episodes Container ─────────────────────────────────────────

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource episodesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: episodeContainerName
  properties: {
    publicAccess: 'None'
  }
}

// ── Table Service + Episodes Table ────────────────────────────────────────────

resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource episodesTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  parent: tableService
  name: episodeTableName
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output storageAccountName string = storageAccount.name
output episodeContainerName string = episodeContainerName
output episodeTableName string = episodeTableName

@description('Storage connection string — marked secure so Bicep does not log the value')
@secure()
output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
