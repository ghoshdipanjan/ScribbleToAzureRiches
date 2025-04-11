{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "location": { "type": "string" },
    "vmName": { "type": "string" },
    "keyVaultName": { "type": "string" },
    "logAnalyticsWorkspaceName": { "type": "string" },
    "automationAccountName": { "type": "string" },
    "firewallName": { "type": "string" },
    "webAppName": { "type": "string" },
    "apiManagementName": { "type": "string" },
    "appGatewayName": { "type": "string" },
    "frontDoorName": { "type": "string" },
    "cosmosDbAccountName": { "type": "string" },
    "storageAccountName": { "type": "string" },
    "dnsZoneName": { "type": "string" },
    "aiServiceName": { "type": "string" },
    "ddosProtectionPlanName": { "type": "string" },
    "networkWatcherName": { "type": "string" }
  },
  "resources": [
    {
      "type": "Microsoft.Compute/virtualMachines",
      "apiVersion": "2021-07-01",
      "name": "[parameters('vmName')]",
      "location": "[parameters('location')]",
      "properties": {
        ...
      }
    },
    {
      "type": "Microsoft.KeyVault/vaults",
      "apiVersion": "2021-06-01-preview",
      "name": "[parameters('keyVaultName')]",
      "location": "[parameters('location')]",
      "properties": {
        ...
      }
    },
    {
      "type": "Microsoft.OperationalInsights/workspaces",
      "apiVersion": "2020-08-01",
      "name": "[parameters('logAnalyticsWorkspaceName')]",
      "location": "[parameters('location')]",
      "properties": {
        ...
      }
    },
    {
      "type": "Microsoft.Automation/automationAccounts",
      "apiVersion": "2021-06-22",
      "name": "[parameters('automationAccountName')]",
      "location": "[parameters('location')]",
      "properties": {
        ...
      }
    },
    {
      "type": "Microsoft.Network/azureFirewalls",
      "apiVersion": "2021-02-01",
      "name": "[parameters('firewallName')]",
      "location": "[parameters('location')]",
      "properties": {
        ...
      }
    },
    {
      "type": "Microsoft.Web/sites",
      "apiVersion": "2021-01-01",
      "name": "[parameters('webAppName')]",
      "location": "[parameters('location')]",
      "properties": {
        ...
      }
    },
    {
      "type": "Microsoft.ApiManagement/service",
      "apiVersion": "2021-08-01",
      "name": "[parameters('apiManagementName')]",
      "location": "[parameters('location')]",
      "properties": {
        ...
      }
    },
    {
      "type": "Microsoft.Network/applicationGateways",
      "apiVersion": "2021-02-01",
      "name": "[parameters('appGatewayName')]",
      "location": "[parameters('location')]",
      "properties": {
        ...
      }
    },
    {
      "type": "Microsoft.Cdn/profiles",
      "apiVersion": "2021-05-01",
      "name": "[parameters('frontDoorName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard_AzureFrontDoor"
      }
    },
    {
      "type": "Microsoft.DocumentDB/databaseAccounts",
      "apiVersion": "2021-04-15",
      "name": "[parameters('cosmosDbAccountName')]",
      "location": "[parameters('location')]",
      "properties": {
        ...
      }
    },
    {
      "type": "Microsoft.Storage/storageAccounts",
      "apiVersion": "2021-04-01",
      "name": "[parameters('storageAccountName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard_LRS"
      }
    },
    {
      "type": "Microsoft.Network/dnsZones",
      "apiVersion": "2018-05-01",
      "name": "[parameters('dnsZoneName')]",
      "location": "[parameters('location')]"
    },
    {
      "type": "Microsoft.CognitiveServices/accounts",
      "apiVersion": "2021-04-30",
      "name": "[parameters('aiServiceName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "S0"
      }
    },
    {
      "type": "Microsoft.Network/ddosProtectionPlans",
      "apiVersion": "2019-07-01",
      "name": "[parameters('ddosProtectionPlanName')]",
      "location": "[parameters('location')]"
    },
    {
      "type": "Microsoft.Network/networkWatchers",
      "apiVersion": "2021-02-01",
      "name": "[parameters('networkWatcherName')]",
      "location": "[parameters('location')]"
    },
    {
      "type": "Microsoft.Security/pricings",
      "apiVersion": "2021-07-01-preview",
      "name": "default",
      "location": "[parameters('location')]",
      "properties": {
        ...
      }
    }
  ]
}