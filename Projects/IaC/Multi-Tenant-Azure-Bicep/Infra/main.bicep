targetScope = 'subscription'

// infra/main.bicep
param location string = deployment().location

@description('Deployment environment. Isolates resource and resource group names between environments.')
@allowed([
  'prod'
  'test'
])
param environmentName string = 'prod'

// The magic array: adding a new customer ID here deploys their infrastructure next run.
param customerIds array = [
  'custalpha'
  'custbeta'
]

@description('Microsoft Entra login/UPN to set as Azure SQL logical server admin')
param sqlEntraAdminLogin string

@description('Object ID of the Microsoft Entra principal used as Azure SQL logical server admin')
param sqlEntraAdminObjectId string

@description('Resource group name prefix for each customer RG. Final name is <prefix><environmentName>-<customerId>')
param resourceGroupPrefix string = 'rg-'

// 1. Create one resource group per customer
resource customerResourceGroups 'Microsoft.Resources/resourceGroups@2024-03-01' = [for id in customerIds: {
  name: '${resourceGroupPrefix}${environmentName}-${id}'
  location: location
}]

// 2. Deploy customer resources into each customer's resource group
module customerInfra './customer-resources.bicep' = [for (id, i) in customerIds: {
  name: 'customer-infra-${environmentName}-${id}'
  scope: customerResourceGroups[i]
  params: {
    customerId: id
    environmentName: environmentName
    location: location
    sqlEntraAdminLogin: sqlEntraAdminLogin
    sqlEntraAdminObjectId: sqlEntraAdminObjectId
  }
}]

output customerResourceGroupNames array = [for id in customerIds: '${resourceGroupPrefix}${environmentName}-${id}']
