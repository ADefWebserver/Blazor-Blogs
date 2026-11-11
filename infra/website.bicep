@description('Azure region for the App Service resources.')
param location string = resourceGroup().location

@description('Globally unique App Service name.')
param websiteName string

@description('App Service plan SKU.')
param appServiceSku string = 'B1'

@description('Resource ID of the user-assigned managed identity.')
param identityId string

@description('Client ID of the user-assigned managed identity.')
param identityClientId string

@secure()
@description('Azure SQL connection string supplied by Aspire.')
param sqlConnectionString string

@description('True after the Azure SQL Entra administrator has been configured.')
param sqlAdminReady bool

var planName = 'blazorblogs-plan-${uniqueString(resourceGroup().id)}'
var planTier = startsWith(appServiceSku, 'B') ? 'Basic' : (startsWith(appServiceSku, 'P') ? 'PremiumV3' : 'Basic')

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  kind: 'app'
  sku: {
    name: appServiceSku
    tier: planTier
    capacity: 1
  }
}

resource website 'Microsoft.Web/sites@2023-12-01' = {
  name: websiteName
  location: location
  kind: 'app'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
  properties: {
    serverFarmId: plan.id
    clientAffinityEnabled: true
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v11.0'
      use32BitWorkerProcess: false
      webSocketsEnabled: true
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'AZURE_CLIENT_ID'
          value: identityClientId
        }
        {
          name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
          value: 'true'
        }
        {
          name: 'Install__AllowDatabaseConfiguration'
          value: 'false'
        }
        {
          name: 'Storage__UserContentPath'
          value: '%HOME%\\data\\BlazorBlogs'
        }
      ]
      connectionStrings: [
        {
          name: 'DefaultConnection'
          connectionString: sqlConnectionString
          type: 'SQLAzure'
        }
      ]
    }
  }
}

output websiteName string = website.name
output defaultHostName string = website.properties.defaultHostName
output sqlAdminReady bool = sqlAdminReady
