{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "location": {
      "type": "string",
      "defaultValue": "[resourceGroup().location]"
    },
    "cdnName": {
      "type": "string"
    },
    "webAppName": {
      "type": "string"
    },
    "storageAccountName": {
      "type": "string"
    },
    "functionAppName": {
      "type": "string"
    },
    "sqlServerName": {
      "type": "string"
    },
    "sqlDatabaseName": {
      "type": "string"
    },
    "redisCacheName": {
      "type": "string"
    },
    "applicationInsightsName": {
      "type": "string"
    }
  },
  "resources": [
    {
      "type": "Microsoft.Storage/storageAccounts",
      "apiVersion": "2021-04-01",
      "name": "[parameters('storageAccountName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard_LRS"
      },
      "kind": "StorageV2",
      "properties": {}
    },
    {
      "type": "Microsoft.Web/sites",
      "apiVersion": "2021-01-01",
      "name": "[parameters('webAppName')]",
      "location": "[parameters('location')]",
      "properties": {
        "serverFarmId": "Default1"
      }
    },
    {
      "type": "Microsoft.Web/sites",
      "apiVersion": "2021-01-01",
      "name": "[parameters('functionAppName')]",
      "location": "[parameters('location')]",
      "properties": {
        "serverFarmId": "Default1"
      }
    },
    {
      "type": "Microsoft.Storage/storageAccounts/queueServices",
      "apiVersion": "2019-06-01",
      "name": "[concat(parameters('storageAccountName'), '/default')]",
      "properties": {}
    },
    {
      "type": "Microsoft.Sql/servers",
      "apiVersion": "2021-02-01-preview",
      "name": "[parameters('sqlServerName')]",
      "location": "[parameters('location')]",
      "properties": {
        "administratorLogin": "adminUser",
        "administratorLoginPassword": "P@ssword1"
      }
    },
    {
      "type": "Microsoft.Sql/servers/databases",
      "apiVersion": "2021-02-01-preview",
      "name": "[concat(parameters('sqlServerName'), '/', parameters('sqlDatabaseName'))]",
      "location": "[parameters('location')]",
      "properties": {}
    },
    {
      "type": "Microsoft.Cache/Redis",
      "apiVersion": "2021-06-01",
      "name": "[parameters('redisCacheName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard",
        "family": "C",
        "capacity": 0
      }
    },
    {
      "type": "Microsoft.Insights/components",
      "apiVersion": "2020-02-02",
      "name": "[parameters('applicationInsightsName')]",
      "location": "[parameters('location')]",
      "properties": {
        "Application_Type": "web"
      }
    },
    {
      "type": "Microsoft.Cdn/profiles",
      "apiVersion": "2020-09-01",
      "name": "[parameters('cdnName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard_Microsoft"
      },
      "properties": {}
    }
  ],
  "outputs": {
    "storageAccountConnectionString": {
      "type": "string",
      "value": "[reference(resourceId('Microsoft.Storage/storageAccounts', parameters('storageAccountName')), '2021-04-01').primaryEndpoints.blob]"
    },
    "sqlServerFqdn": {
      "type": "string",
      "value": "[reference(resourceId('Microsoft.Sql/servers', parameters('sqlServerName')), '2021-02-01-preview').fullyQualifiedDomainName]"
    }
  }
}