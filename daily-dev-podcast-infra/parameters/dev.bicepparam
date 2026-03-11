using '../main.bicep'

// Dev — use a separate resource group (e.g. myspace-dev) to keep costs isolated
// Deploy with:
//   az deployment group create \
//     --resource-group myspace-dev \
//     --template-file main.bicep \
//     --parameters parameters/dev.bicepparam

param environmentName = 'dev'

param location = 'southafricanorth'

param openAiLocation = 'eastus'
