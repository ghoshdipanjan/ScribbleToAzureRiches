{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "location": {
      "type": "string",
      "metadata": {
        "description": "Location for all resources."
      }
    },
    "vmName": {
      "type": "string",
      "defaultValue": "myVM"
    },
    "adminUsername": {
      "type": "string",
      "defaultValue": "azureuser"
    },
    "adminPassword": {
      "type": "securestring"
    },
    "sqlServerName": {
      "type": "string",
      "defaultValue": "mySqlServer"
    },
    "sqlAdminUsername": {
      "type": "string",
      "defaultValue": "sqladmin"
    },
    "sqlAdminPassword": {
      "type": "securestring"
    },
    "storageAccountName": {
      "type": "string",
      "defaultValue": "mystorageacct"
    }
  },
  "resources": [
    {
      "type": "Microsoft.Storage/storageAccounts",
      "apiVersion": "2021-09-01",
      "name": "[parameters('storageAccountName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "Standard_LRS"
      },
      "kind": "StorageV2",
      "properties": {}
    },
    {
      "type": "Microsoft.Sql/servers",
      "apiVersion": "2021-05-01-preview",
      "name": "[parameters('sqlServerName')]",
      "location": "[parameters('location')]",
      "properties": {
        "administratorLogin": "[parameters('sqlAdminUsername')]",
        "administratorLoginPassword": "[parameters('sqlAdminPassword')]"
      },
      "sku": {
        "name": "GP_Gen5_2",
        "tier": "GeneralPurpose"
      }
    },
    {
      "type": "Microsoft.Sql/servers/databases",
      "apiVersion": "2021-02-01-preview",
      "name": "[concat(parameters('sqlServerName'), '/myDatabase')]",
      "location": "[parameters('location')]",
      "properties": {
        "collation": "SQL_Latin1_General_CP1_CI_AS",
        "maxSizeBytes": 5368709120
      },
      "sku": {
        "name": "Standard",
        "tier": "Standard"
      }
    },
    {
      "type": "Microsoft.Network/networkInterfaces",
      "apiVersion": "2021-02-01",
      "name": "[concat(parameters('vmName'), '-nic')]",
      "location": "[parameters('location')]",
      "properties": {
        "ipConfigurations": [
          {
            "name": "ipconfig1",
            "properties": {
              "privateIPAllocationMethod": "Dynamic",
              "subnet": {
                "id": "[resourceId('Microsoft.Network/virtualNetworks/subnets', 'myVnet', 'default')]"
              }
            }
          }
        ]
      }
    },
    {
      "type": "Microsoft.Compute/virtualMachines",
      "apiVersion": "2021-07-01",
      "name": "[parameters('vmName')]",
      "location": "[parameters('location')]",
      "dependsOn": [
        "[resourceId('Microsoft.Network/networkInterfaces', concat(parameters('vmName'), '-nic'))]"
      ],
      "properties": {
        "hardwareProfile": {
          "vmSize": "Standard_DS1_v2"
        },
        "osProfile": {
          "computerName": "[parameters('vmName')]",
          "adminUsername": "[parameters('adminUsername')]",
          "adminPassword": "[parameters('adminPassword')]"
        },
        "networkProfile": {
          "networkInterfaces": [
            {
              "id": "[resourceId('Microsoft.Network/networkInterfaces', concat(parameters('vmName'), '-nic'))]"
            }
          ]
        },
        "storageProfile": {
          "imageReference": {
            "publisher": "MicrosoftWindowsServer",
            "offer": "WindowsServer",
            "sku": "2019-Datacenter",
            "version": "latest"
          },
          "osDisk": {
            "name": "[concat(parameters('vmName'), '-osdisk')]",
            "caching": "ReadWrite",
            "createOption": "FromImage"
          }
        }
      }
    }
  ],
  "outputs": {
    "storageAccountName": {
      "type": "string",
      "value": "[parameters('storageAccountName')]"
    },
    "sqlServerName": {
      "type": "string",
      "value": "[parameters('sqlServerName')]"
    },
    "vmName": {
      "type": "string",
      "value": "[parameters('vmName')]"
    }
  }
}
