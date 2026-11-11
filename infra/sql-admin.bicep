@description('Name of the Azure SQL logical server.')
param sqlServerName string

@description('Display name of the user-assigned managed identity.')
param identityName string

@description('Object/principal ID of the user-assigned managed identity.')
param identityPrincipalId string

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: sqlServerName
}

resource sqlAdministrator 'Microsoft.Sql/servers/administrators@2023-08-01' = {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: identityName
    sid: identityPrincipalId
    tenantId: tenant().tenantId
  }
}

output configured bool = !empty(sqlAdministrator.properties.sid)
