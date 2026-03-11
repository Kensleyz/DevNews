using '../main.bicep'

// Production — deploys to the myspace resource group
// Deploy with:
//   az deployment group create \
//     --resource-group myspace \
//     --template-file main.bicep \
//     --parameters parameters/prod.bicepparam

param environmentName = 'prod'

// southafricanorth — closest region, lowest latency from ZA
param location = 'southafricanorth'

// Azure OpenAI is NOT available in southafricanorth — use a supported region
// Options: eastus, eastus2, swedencentral, australiaeast, westeurope
param openAiLocation = 'eastus'
