{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "location": {
      "type": "string",
      "metadata": {
        "description": "Deployment location"
      }
    },
    "storageAccountName": {
      "type": "string"
    },
    "cdnProfileName": {
      "type": "string"
    },
    "appName": {
      "type": "string"
    },
    "sqlServerName": {
      "type": "string"
    },
    "sqlDbName": {
      "type": "string"
    },
    "redisCacheName": {
      "type": "string"
    },
    "functionName": {
      "type": "string"
    },
    "appInsightsName": {
      "type": "string"
    }
  },
  "resources": [
    {
      "type": "Microsoft.Storage/storageAccounts",
      "apiVersion": "2022-05-01",
      "name": "[parameters('storageAccountName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard_LRS"
      },
      "kind": "StorageV2",
      "properties": {}
    },
    {
      "type": "Microsoft.Storage/storageAccounts/queueServices/queues",
      "apiVersion": "2022-05-01",
      "name": "[concat(parameters('storageAccountName'), '/default/queue')]",
      "dependsOn": [
        "[resourceId('Microsoft.Storage/storageAccounts', parameters('storageAccountName'))]"
      ],
      "properties": {}
    },
    {
      "type": "Microsoft.Cdn/profiles",
      "apiVersion": "2021-06-01",
      "name": "[parameters('cdnProfileName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard_Microsoft"
      },
      "properties": {}
    },
    {
      "type": "Microsoft.Web/sites",
      "apiVersion": "2021-02-01",
      "name": "[parameters('appName')]",
      "location": "[parameters('location')]",
      "properties": {}
    },
    {
      "type": "Microsoft.Sql/servers",
      "apiVersion": "2021-05-01-preview",
      "name": "[parameters('sqlServerName')]",
      "location": "[parameters('location')]",
      "properties": {
        "administratorLogin": "sqladmin",
        "administratorLoginPassword": "P@ssw0rd!"
      }
    },
    {
      "type": "Microsoft.Sql/servers/databases",
      "apiVersion": "2021-05-01-preview",
      "name": "[concat(parameters('sqlServerName'), '/', parameters('sqlDbName'))]",
      "dependsOn": [
        "[resourceId('Microsoft.Sql/servers', parameters('sqlServerName'))]"
      ],
      "properties": {
        "sku": {
          "name": "S0"
        }
      }
    },
    {
      "type": "Microsoft.Cache/Redis",
      "apiVersion": "2020-06-01",
      "name": "[parameters('redisCacheName')]",
      "location": "[parameters('location')]",
      "properties": {
        "sku": {
          "name": "Basic",
          "family": "C",
          "capacity": 0
        }
      }
    },
    {
      "type": "Microsoft.Insights/components",
      "apiVersion": "2020-02-02-preview",
      "name": "[parameters('appInsightsName')]",
      "location": "[parameters('location')]",
      "properties": {
        "Application_Type": "web"
      }
    },
    {
      "type": "Microsoft.Web/sites",
      "apiVersion": "2021-02-01",
      "name": "[parameters('functionName')]",
      "location": "[parameters('location')]",
      "kind": "functionapp",
      "properties": {
        "serverFarmId": "" // Specify the hosting plan resource ID, can be added as a parameter additionally
      }
    }
  ]
}