@description('Azure region for the temporary upgrade-test resources.')
param location string = resourceGroup().location

@description('Stable prefix used to derive globally unique temporary resource names.')
@minLength(3)
@maxLength(20)
param resourceNameSeed string = 'blazorblogs-upgrade'

@description('App Service .NET runtime version. Use v9.0 for the baseline and v11.0 for the upgrade target.')
@allowed([
  'v9.0'
  'v11.0'
])
param dotnetVersion string = 'v9.0'

var suffix = take(uniqueString(subscription().id, resourceGroup().id, resourceNameSeed), 8)
var identityName = '${resourceNameSeed}-id-${suffix}'
var sqlServerName = '${resourceNameSeed}-sql-${suffix}'
var databaseName = 'blazorblogs'
var appServicePlanName = '${resourceNameSeed}-plan-${suffix}'
var websiteName = '${resourceNameSeed}-${suffix}'
var sqlConnectionString = 'Server=tcp:${sqlServer.outputs.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};Authentication=Active Directory Managed Identity;User ID=${managedIdentity.outputs.clientId};Encrypt=True;TrustServerCertificate=False;Connect Timeout=30;'
var tags = {
  Workload: 'BlazorBlogs'
  Purpose: 'TemporaryUpgradeVerification'
  DeleteAfterTest: 'true'
}

// Use a dedicated identity for both the web app and Azure SQL Entra administration.
module managedIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.6.0' = {
  params: {
    name: identityName
    location: location
    tags: tags
  }
}

// Azure SQL is configured with Entra-only authentication and no SQL password.
module sqlServer 'br/public:avm/res/sql/server:0.22.1' = {
  params: {
    name: sqlServerName
    location: location
    tags: tags
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: identityName
      principalType: 'Application'
      sid: managedIdentity.outputs.principalId
      tenantId: tenant().tenantId
    }
    databases: [
      {
        availabilityZone: -1
        name: databaseName
        sku: {
          name: 'Basic'
          tier: 'Basic'
        }
        maxSizeBytes: 2147483648
        zoneRedundant: false
        tags: tags
      }
    ]
    firewallRules: [
      {
        name: 'AllowAzureServices'
        startIpAddress: '0.0.0.0'
        endIpAddress: '0.0.0.0'
      }
    ]
  }
}

module appServicePlan 'br/public:avm/res/web/serverfarm:0.7.0' = {
  params: {
    name: appServicePlanName
    location: location
    tags: tags
    kind: 'Windows'
    reserved: false
    skuName: 'B1'
    skuCapacity: 1
    zoneRedundant: false
  }
}

// Keep the same App Service while changing only its runtime during the upgrade.
module website 'br/public:avm/res/web/site:0.24.0' = {
  params: {
    name: websiteName
    location: location
    tags: tags
    kind: 'app'
    serverFarmResourceId: appServicePlan.outputs.resourceId
    httpsOnly: true
    clientAffinityEnabled: false
    publicNetworkAccess: 'Enabled'
    managedIdentities: {
      systemAssigned: false
      userAssignedResourceIds: [
        managedIdentity.outputs.resourceId
      ]
    }
    siteConfig: {
      alwaysOn: true
      ftpsState: 'Disabled'
      http20Enabled: true
      minTlsVersion: '1.2'
      netFrameworkVersion: dotnetVersion
      use32BitWorkerProcess: false
      webSocketsEnabled: true
    }
    configs: [
      {
        name: 'appsettings'
        properties: {
          ASPNETCORE_FORWARDEDHEADERS_ENABLED: 'true'
          AZURE_CLIENT_ID: managedIdentity.outputs.clientId
          Install__AllowDatabaseConfiguration: 'false'
          Storage__UserContentPath: '%HOME%\\data\\BlazorBlogs'
        }
        retainCurrentAppSettings: true
      }
      {
        name: 'connectionstrings'
        properties: {
          DefaultConnection: {
            type: 'SQLAzure'
            value: sqlConnectionString
          }
        }
      }
    ]
    basicPublishingCredentialsPolicies: [
      {
        name: 'ftp'
        allow: false
      }
    ]
  }
}

output resourceGroupName string = resourceGroup().name
output websiteName string = website.outputs.name
output defaultHostName string = website.outputs.defaultHostname
output sqlServerName string = sqlServer.outputs.name
output databaseName string = databaseName
output managedIdentityName string = managedIdentity.outputs.name
