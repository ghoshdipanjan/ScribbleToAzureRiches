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
    "vmName": {
      "type": "string",
      "defaultValue": "myVM",
      "metadata": {
        "description": "Name of the virtual machine."
      }
    },
    "adminUsername": {
      "type": "string",
      "defaultValue": "azureuser",
      "metadata": {
        "description": "Admin username for the virtual machine."
      }
    },
    "adminPassword": {
      "type": "secureString",
      "defaultValue": "P@ssword1234",
      "metadata": {
        "description": "Admin password for the virtual machine."
      }
    },
    "sqlServerName": {
      "type": "string",
      "defaultValue": "mySqlServer",
      "metadata": {
        "description": "Name of the SQL Server."
      }
    },
    "sqlAdminUsername": {
      "type": "string",
      "defaultValue": "sqladmin",
      "metadata": {
        "description": "Admin username for the SQL Server."
      }
    },
    "sqlAdminPassword": {
      "type": "secureString",
      "defaultValue": "P@ssword1234!",
      "metadata": {
        "description": "Admin password for the SQL Server."
      }
    },
    "storageAccountName": {
      "type": "string",
      "defaultValue": "mystorageacct",
      "metadata": {
        "description": "Storage Account name."
      }
    }
  },
  "variables": {},
  "resources": [
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
      "type": "Microsoft.Sql/servers",
      "apiVersion": "2021-05-01-preview",
      "name": "[parameters('sqlServerName')]",
      "location": "[parameters('location')]",
      "properties": {
        "administratorLogin": "[parameters('sqlAdminUsername')]",
        "administratorLoginPassword": "[parameters('sqlAdminPassword')]"
      },
      "resources": [
        {
          "type": "databases",
          "apiVersion": "2021-05-01-preview",
          "name": "master",
          "location": "[parameters('location')]",
          "properties": {}
        }
      ]
    },
    {
      "type": "Microsoft.Compute/virtualMachines",
      "apiVersion": "2021-07-01",
      "name": "[parameters('vmName')]",
      "location": "[parameters('location')]",
      "properties": {
        "hardwareProfile": {
          "vmSize": "Standard_DS1_v2"
        },
        "osProfile": {
          "computerName": "[parameters('vmName')]",
          "adminUsername": "[parameters('adminUsername')]",
          "adminPassword": "[parameters('adminPassword')]"
        },
        "storageProfile": {
          "osDisk": {
            "createOption": "FromImage"
          },
          "imageReference": {
            "publisher": "Canonical",
            "offer": "UbuntuServer",
            "sku": "18.04-LTS",
            "version": "latest"
          }
        },
        "networkProfile": {
          "networkInterfaces": [
            {
              "id": "[resourceId('Microsoft.Network/networkInterfaces', concat(parameters('vmName'), '-nic'))]"
            }
          ]
        }
      },
      "dependsOn": [
        "[resourceId('Microsoft.Network/networkInterfaces', concat(parameters('vmName'), '-nic'))]"
      ]
    },
    {
      "type": "Microsoft.Network/networkInterfaces",
      "apiVersion": "2021-05-01",
      "name": "[concat(parameters('vmName'), '-nic')]",
      "location": "[parameters('location')]",
      "properties": {
        "ipConfigurations": [
          {
            "name": "ipconfig1",
            "properties": {
              "subnet": {
                "id": "[resourceId('Microsoft.Network/virtualNetworks/subnets', concat(parameters('vmName'), '-vnet'), 'default')]"
              },
              "privateIPAllocationMethod": "Dynamic"
            }
          }
        ]
      },
      "dependsOn": [
        "[resourceId('Microsoft.Network/virtualNetworks', concat(parameters('vmName'), '-vnet'))]"
      ]
    },
    {
      "type": "Microsoft.Network/virtualNetworks",
      "apiVersion": "2021-05-01",
      "name": "[concat(parameters('vmName'), '-vnet')]",
      "location": "[parameters('location')]",
      "properties": {
        "addressSpace": {
          "addressPrefixes": [
            "10.0.0.0/16"
          ]
        },
        "subnets": [
          {
            "name": "default",
            "properties": {
              "addressPrefix": "10.0.0.0/24"
            }
          }
        ]
      }
    }
  ],
  "outputs": {
    "vmId": {
      "type": "string",
      "value": "[resourceId('Microsoft.Compute/virtualMachines', parameters('vmName'))]"
    },
    "storageAccountId": {
      "type": "string",
      "value": "[resourceId('Microsoft.Storage/storageAccounts', parameters('storageAccountName'))]"
    },
    "sqlServerId": {
      "type": "string",
      "value": "[resourceId('Microsoft.Sql/servers', parameters('sqlServerName'))]"
    }
  }
}