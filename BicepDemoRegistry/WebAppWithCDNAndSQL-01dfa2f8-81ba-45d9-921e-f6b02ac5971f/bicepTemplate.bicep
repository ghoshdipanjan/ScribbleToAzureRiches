{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "location": {
      "type": "string",
      "defaultValue": "[resourceGroup().location]"
    },
    "sqlServerName": {
      "type": "string"
    },
    "databaseName": {
      "type": "string"
    },
    "adminUser": {
      "type": "string"
    },
    "adminPassword": {
      "type": "securestring"
    },
    "appServicePlanName": {
      "type": "string"
    },
    "webAppName": {
      "type": "string"
    },
    "functionName": {
      "type": "string"
    },
    "storageAccountName": {
      "type": "string"
    },
    "insightsName": {
      "type": "string"
    },
    "cdnProfileName": {
      "type": "string"
    },
    "cdnEndpointName": {
      "type": "string"
    },
    "redisCacheName": {
      "type": "string"
    }
  },
  "resources": [
    {
      "type": "Microsoft.Sql/servers",
      "apiVersion": "2021-02-01-preview",
      "name": "[parameters('sqlServerName')]",
      "location": "[parameters('location')]",
      "properties": {
        "administratorLogin": "[parameters('adminUser')]",
        "administratorLoginPassword": "[parameters('adminPassword')]"
      }
    },
    {
      "type": "Microsoft.Sql/servers/databases",
      "apiVersion": "2021-02-01-preview",
      "name": "[concat(parameters('sqlServerName'), '/', parameters('databaseName'))]",
      "location": "[parameters('location')]"
    },
    {
      "type": "Microsoft.Web/serverfarms",
      "apiVersion": "2020-12-01",
      "name": "[parameters('appServicePlanName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "S1",
        "tier": "Standard"
      }
    },
    {
      "type": "Microsoft.Web/sites",
      "apiVersion": "2020-12-01",
      "name": "[parameters('webAppName')]",
      "location": "[parameters('location')]",
      "properties": {
        "serverFarmId": "[resourceId('Microsoft.Web/serverfarms', parameters('appServicePlanName'))]"
      }
    },
    {
      "type": "Microsoft.Web/sites",
      "apiVersion": "2020-12-01",
      "name": "[parameters('functionName')]",
      "location": "[parameters('location')]",
      "properties": {
        "serverFarmId": "[resourceId('Microsoft.Web/serverfarms', parameters('appServicePlanName'))]"
      }
    },
    {
      "type": "Microsoft.Storage/storageAccounts",
      "apiVersion": "2021-08-01",
      "name": "[parameters('storageAccountName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard_LRS"
      },
      "kind": "StorageV2"
    },
    {
      "type": "Microsoft.Insights/components",
      "apiVersion": "2020-02-02",
      "name": "[parameters('insightsName')]",
      "location": "[parameters('location')]",
      "properties": {
        "Application_Type": "web"
      }
    },
    {
      "type": "Microsoft.Cdn/profiles",
      "apiVersion": "2020-09-01",
      "name": "[parameters('cdnProfileName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard_Akamai"
      }
    },
    {
      "type": "Microsoft.Cdn/profiles/endpoints",
      "apiVersion": "2020-09-01",
      "name": "[concat(parameters('cdnProfileName'), '/', parameters('cdnEndpointName'))]",
      "location": "[parameters('location')]"
    },
    {
      "type": "Microsoft.Cache/Redis",
      "apiVersion": "2020-06-01",
      "name": "[parameters('redisCacheName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard",
        "family": "C",
        "capacity": 1
      }
    }
  ],
  "outputs": {
    "sqlServerName": {
      "type": "string",
      "value": "[parameters('sqlServerName')]"
    },
    "sqlDatabaseName": {
      "type": "string",
      "value": "[parameters('databaseName')]"
    },
    "webAppName": {
      "type": "string",
      "value": "[parameters('webAppName')]"
    },
    "storageAccountName": {
      "type": "string",
      "value": "[parameters('storageAccountName')]"
    },
    "applicationInsightsName": {
      "type": "string",
      "value": "[parameters('insightsName')]"
    },
    "cdnProfileName": {
      "type": "string",
      "value": "[parameters('cdnProfileName')]"
    },
    "cdnEndpointName": {
      "type": "string",
      "value": "[parameters('cdnEndpointName')]"
    },
    "redisCacheName": {
      "type": "string",
      "value": "[parameters('redisCacheName')]"
    }
  }
}