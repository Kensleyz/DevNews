@description('Azure region — must support Azure OpenAI + GPT-4o (e.g. eastus, swedencentral)')
param location string

@description('Environment name')
param environmentName string

@description('Unique suffix')
param resourceSuffix string

var accountName = 'oai-devpod-${environmentName}-${resourceSuffix}'

// ── Azure OpenAI Account ──────────────────────────────────────────────────────

resource openAiAccount 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: accountName
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: accountName
    publicNetworkAccess: 'Enabled'
  }
  tags: {
    project: 'daily-dev-podcast'
    environment: environmentName
  }
}

// ── GPT-4o Model Deployment ───────────────────────────────────────────────────
// Capacity: 10K TPM — sufficient for 1 episode/day (~2K tokens each)
// Adjust capacity up if you see throttling

resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-10-01-preview' = {
  parent: openAiAccount
  name: 'gpt-4o'
  sku: {
    name: 'Standard'
    capacity: 10 // 10K tokens per minute
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-11-20'
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output endpoint string = openAiAccount.properties.endpoint
output accountName string = openAiAccount.name
