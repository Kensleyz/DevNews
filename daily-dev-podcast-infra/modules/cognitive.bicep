@description('Azure region')
param location string

@description('Environment name')
param environmentName string

@description('Unique suffix')
param resourceSuffix string

var accountName = 'cog-devpod-${environmentName}-${resourceSuffix}'

// ── Cognitive Services — Speech (TTS) ─────────────────────────────────────────
// S0 tier: pay-per-use neural TTS
// ~3000 chars/episode x 30 days = ~90K chars/month — well within free 500K chars/month on F0
// Using S0 to avoid F0 regional/quota limitations in production

resource speechAccount 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: accountName
  location: location
  kind: 'SpeechServices'
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

// ── Outputs ───────────────────────────────────────────────────────────────────

output endpoint string = 'https://${location}.api.cognitive.microsoft.com/'
output accountName string = speechAccount.name
