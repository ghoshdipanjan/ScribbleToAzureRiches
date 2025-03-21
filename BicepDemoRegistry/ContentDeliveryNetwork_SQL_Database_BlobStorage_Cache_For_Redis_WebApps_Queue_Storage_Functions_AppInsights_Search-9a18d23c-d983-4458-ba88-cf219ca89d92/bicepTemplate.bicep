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
    "sqlDbName": {
      "type": "string"
    },
    "adminUsername": {
      "type": "string"
    },
    "adminPassword": {
      "type": "securestring"
    },
    "storageAccountName": {
      "type": "string"
    },
    "hostingPlanName": {
      "type": "string"
    },
    "webAppName": {
      "type": "string"
    },
    "redisCacheName": {
      "type": "string"
    },
    "redisSkuName": {
      "type": "string",
      "defaultValue": "Standard"
    },
    "queueStorageAccountName": {
      "type": "string"
    },
    "functionAppName": {
      "type": "string"
    },
    "appInsightsName": {
      "type": "string"
    },
    "searchServiceName": {
      "type": "string"
    },
    "searchServiceSku": {
      "type": "string",
      "defaultValue": "Basic"
    },
    "cdnProfileName": {
      "type": "string"
    },
    "cdnEndpointName": {
      "type": "string"
    }
  },
  "resources": [
    {
      "type": "Microsoft.Sql/servers",
      "apiVersion": "2020-11-01-preview",
      "name": "[parameters('sqlServerName')]",
      "location": "[parameters('location')]",
      "properties": {
        "administratorLogin": "[parameters('adminUsername')]",
        "administratorLoginPassword": "[parameters('adminPassword')]"
      }
    },
    {
      "type": "Microsoft.Sql/servers/databases",
      "apiVersion": "2020-11-01-preview",
      "name": "[concat(parameters('sqlServerName'), '/', parameters('sqlDbName'))]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "S1"
      }
    },
    {
      "type": "Microsoft.Storage/storageAccounts",
      "apiVersion": "2021-04-01",
      "name": "[parameters('storageAccountName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard_LRS"
      },
      "kind": "StorageV2"
    },
    {
      "type": "Microsoft.Web/serverfarms",
      "apiVersion": "2021-01-15",
      "name": "[parameters('hostingPlanName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "S1",
        "capacity": 1
      }
    },
    {
      "type": "Microsoft.Web/sites",
      "apiVersion": "2021-01-15",
      "name": "[parameters('webAppName')]",
      "location": "[parameters('location')]",
      "properties": {
        "serverFarmId": "[resourceId('Microsoft.Web/serverfarms', parameters('hostingPlanName'))]"
      }
    },
    {
      "type": "Microsoft.Cache/Redis",
      "apiVersion": "2021-06-01",
      "name": "[parameters('redisCacheName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "[parameters('redisSkuName')]",
        "family": "C",
        "capacity": 1
      }
    },
    {
      "type": "Microsoft.Storage/storageAccounts",
      "apiVersion": "2021-04-01",
      "name": "[parameters('queueStorageAccountName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard_LRS"
      },
      "kind": "StorageV2"
    },
    {
      "type": "Microsoft.Web/sites",
      "apiVersion": "2020-12-01",
      "name": "[parameters('functionAppName')]",
      "location": "[parameters('location')]",
      "properties": {
        "serverFarmId": "[resourceId('Microsoft.Web/serverfarms', parameters('hostingPlanName'))]",
        "siteConfig": {
          "appSettings": [
            {
              "name": "AzureWebJobsStorage",
              "value": "[reference(resourceId('Microsoft.Storage/storageAccounts', parameters('queueStorageAccountName'))).primaryEndpoints.queue]"
            }
          ]
        }
      }
    },
    {
      "type": "Microsoft.Insights/components",
      "apiVersion": "2020-02-02",
      "name": "[parameters('appInsightsName')]",
      "location": "[parameters('location')]",
      "kind": "web",
      "properties": {
        "Application_Type": "web"
      }
    },
    {
      "type": "Microsoft.Search/searchServices",
      "apiVersion": "2020-03-13",
      "name": "[parameters('searchServiceName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "[parameters('searchServiceSku')]"
      }
    },
    {
      "type": "Microsoft.Cdn/profiles",
      "apiVersion": "2021-06-01",
      "name": "[parameters('cdnProfileName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard_Verizon"
      }
    },
    {
      "type": "Microsoft.Cdn/profiles/endpoints",
      "apiVersion": "2021-06-01",
      "name": "[concat(parameters('cdnProfileName'), '/', parameters('cdnEndpointName'))]",
      "location": "[parameters('location')]",
      "properties": {
        "originHostHeader": "[parameters('webAppName')]",
        "origins": [
          {
            "name": "origin",
            "properties": {
              "hostName": "[reference(resourceId('Microsoft.Web/sites', parameters('webAppName'))).defaultHostName]"
            }
          }
        ]
      }
    }
  ],
  "outputs": {
    "sqlServerName": {
      "type": "string",
      "value": "[parameters('sqlServerName')]"
    },
    "sqlDbName": {
      "type": "string",
      "value": "[parameters('sqlDbName')]"
    },
    "storageAccountName": {
      "type": "string",
      "value": "[parameters('storageAccountName')]"
    },
    "webAppName": {
      "type": "string",
      "value": "[parameters('webAppName')]"
    },
    "redisCacheName": {
      "type": "string",
      "value": "[parameters('redisCacheName')]"
    },
    "queueStorageAccountName": {
      "type": "string",
      "value": "[parameters('queueStorageAccountName')]"
    },
    "functionAppName": {
      "type": "string",
      "value": "[parameters('functionAppName')]"
    },
    "appInsightsName": {
      "type": "string",
      "value": "[parameters('appInsightsName')]"
    },
    "searchServiceName": {
      "type": "string",
      "value": "[parameters('searchServiceName')]"
    },
    "cdnProfileName": {
      "type": "string",
      "value": "[parameters('cdnProfileName')]"
    },
    "cdnEndpointName": {
      "type": "string",
      "value": "[parameters('cdnEndpointName')]"
    }
  }
}