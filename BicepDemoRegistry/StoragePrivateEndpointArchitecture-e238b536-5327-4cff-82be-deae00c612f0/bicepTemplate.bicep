{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "storageAccountName": {
      "type": "string"
    },
    "location": {
      "type": "string",
      "defaultValue": "[resourceGroup().location]"
    },
    "vnetName": {
      "type": "string"
    },
    "subnetName": {
      "type": "string"
    },
    "privateEndpointName": {
      "type": "string"
    },
    "privateDnsZoneName": {
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
      "type": "Microsoft.Network/virtualNetworks/subnets",
      "apiVersion": "2021-02-01",
      "name": "[concat(parameters('vnetName'), '/', parameters('subnetName'))]",
      "location": "[parameters('location')]",
      "dependsOn": []
    },
    {
      "type": "Microsoft.Network/privateEndpoints",
      "apiVersion": "2021-03-01",
      "name": "[parameters('privateEndpointName')]",
      "location": "[parameters('location')]",
      "dependsOn": [
        "[resourceId('Microsoft.Storage/storageAccounts', parameters('storageAccountName'))]",
        "[resourceId('Microsoft.Network/virtualNetworks/subnets', parameters('vnetName'), parameters('subnetName'))]"
      ],
      "properties": {
        "subnet": {
          "id": "[resourceId('Microsoft.Network/virtualNetworks/subnets', parameters('vnetName'), parameters('subnetName'))]"
        },
        "privateLinkServiceConnections": [
          {
            "name": "[concat(parameters('privateEndpointName'), '-connection')]",
            "properties": {
              "privateLinkServiceId": "[resourceId('Microsoft.Storage/storageAccounts', parameters('storageAccountName'))]",
              "groupIds": [
                "blob"
              ]
            }
          }
        ]
      }
    },
    {
      "type": "Microsoft.Network/privateDnsZones",
      "apiVersion": "2020-06-01",
      "name": "[parameters('privateDnsZoneName')]",
      "location": "global"
    },
    {
      "type": "Microsoft.Network/privateEndpoints/privateDnsZoneGroups",
      "apiVersion": "2020-06-01",
      "name": "[concat(parameters('privateEndpointName'), '-dnszonegroup')]",
      "dependsOn": [
        "[resourceId('Microsoft.Network/privateEndpoints', parameters('privateEndpointName'))]"
      ],
      "properties": {
        "privateDnsZoneConfigs": [
          {
            "name": "default",
            "properties": {
              "privateDnsZoneId": "[resourceId('Microsoft.Network/privateDnsZones', parameters('privateDnsZoneName'))]"
            }
          }
        ]
      }
    }
  ],
  "outputs": {
    "storageAccountId": {
      "type": "string",
      "value": "[resourceId('Microsoft.Storage/storageAccounts', parameters('storageAccountName'))]"
    },
    "privateEndpointId": {
      "type": "string",
      "value": "[resourceId('Microsoft.Network/privateEndpoints', parameters('privateEndpointName'))]"
    }
  }
}