{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "location": {
      "type": "string",
      "defaultValue": "[resourceGroup().location]",
      "metadata": {
        "description": "Location of the resources."
      }
    },
    "webAppName": {
      "type": "string",
      "metadata": {
        "description": "Name of the Web App."
      }
    },
    "storageAccountName": {
      "type": "string",
      "metadata": {
        "description": "Name of the Storage Account."
      }
    },
    "vnetName": {
      "type": "string",
      "metadata": {
        "description": "Name of the Virtual Network."
      }
    },
    "subnet1Name": {
      "type": "string",
      "metadata": {
        "description": "Name of the first subnet."
      }
    },
    "subnet2Name": {
      "type": "string",
      "metadata": {
        "description": "Name of the second subnet."
      }
    }
  },
  "resources": [
    {
      "type": "Microsoft.Network/virtualNetworks",
      "apiVersion": "2020-06-01",
      "name": "[parameters('vnetName')]",
      "location": "[parameters('location')]",
      "properties": {
        "addressSpace": {
          "addressPrefixes": [
            "10.0.0.0/16"
          ]
        },
        "subnets": [
          {
            "name": "[parameters('subnet1Name')]",
            "properties": {
              "addressPrefix": "10.0.1.0/24"
            }
          },
          {
            "name": "[parameters('subnet2Name')]",
            "properties": {
              "addressPrefix": "10.0.2.0/24"
            }
          }
        ]
      }
    },
    {
      "type": "Microsoft.Web/sites",
      "apiVersion": "2021-02-01",
      "name": "[parameters('webAppName')]",
      "location": "[parameters('location')]",
      "properties": {
        "serverFarmId": "SERVICE_PLAN_ID"
      }
    },
    {
      "type": "Microsoft.Storage/storageAccounts",
      "apiVersion": "2021-02-01",
      "name": "[parameters('storageAccountName')]",
      "location": "[parameters('location')]",
      "kind": "StorageV2",
      "sku": {
        "name": "Standard_LRS"
      }
    },
    {
      "type": "Microsoft.Network/privateEndpoints",
      "apiVersion": "2021-03-01",
      "name": "[concat(parameters('webAppName'), '-pe')]",
      "location": "[parameters('location')]",
      "properties": {
        "subnet": {
          "id": "[resourceId('Microsoft.Network/virtualNetworks/subnets', parameters('vnetName'), parameters('subnet1Name'))]"
        },
        "privateLinkServiceConnections": [
          {
            "name": "[concat(parameters('webAppName'), '-plsc')]",
            "properties": {
              "privateLinkServiceId": "[resourceId('Microsoft.Web/sites', parameters('webAppName'))]",
              "groupIds": [
                "sites"
              ]
            }
          }
        ]
      }
    },
    {
      "type": "Microsoft.Network/privateEndpoints",
      "apiVersion": "2021-03-01",
      "name": "[concat(parameters('storageAccountName'), '-pe')]",
      "location": "[parameters('location')]",
      "properties": {
        "subnet": {
          "id": "[resourceId('Microsoft.Network/virtualNetworks/subnets', parameters('vnetName'), parameters('subnet2Name'))]"
        },
        "privateLinkServiceConnections": [
          {
            "name": "[concat(parameters('storageAccountName'), '-plsc')]",
            "properties": {
              "privateLinkServiceId": "[resourceId('Microsoft.Storage/storageAccounts', parameters('storageAccountName'))]",
              "groupIds": [
                "blob"
              ]
            }
          }
        ]
      }
    }
  ],
  "outputs": {
    "webAppEndpointId": {
      "type": "string",
      "value": "[resourceId('Microsoft.Network/privateEndpoints', concat(parameters('webAppName'), '-pe'))]"
    },
    "storageEndpointId": {
      "type": "string",
      "value": "[resourceId('Microsoft.Network/privateEndpoints', concat(parameters('storageAccountName'), '-pe'))]"
    }
  }
}