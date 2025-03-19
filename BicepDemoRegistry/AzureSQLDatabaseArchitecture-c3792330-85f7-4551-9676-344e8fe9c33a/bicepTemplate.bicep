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
    "sqlAdministratorLogin": {
      "type": "string"
    },
    "sqlAdministratorLoginPassword": {
      "type": "securestring"
    },
    "sqlDatabaseName": {
      "type": "string"
    }
  },
  "resources": [
    {
      "type": "Microsoft.Sql/servers",
      "apiVersion": "2020-11-01-preview",
      "location": "[parameters('location')],"
      "name": "[parameters('sqlServerName')]",
      "properties": {
        "administratorLogin": "[parameters('sqlAdministratorLogin')]",
        "administratorLoginPassword": "[parameters('sqlAdministratorLoginPassword')]"
      },
      "resources": [
        {
          "type": "databases",
          "apiVersion": "2020-11-01-preview",
          "location": "[parameters('location')]",
          "name": "[parameters('sqlDatabaseName')]",
          "properties": {}
        }
      ]
    }
  ],
  "outputs": {
    "sqlServerName": {
      "type": "string",
      "value": "[resourceId('Microsoft.Sql/servers', parameters('sqlServerName'))]"
    },
    "sqlDatabaseName": {
      "type": "string",
      "value": "[concat(parameters('sqlServerName'), '/', parameters('sqlDatabaseName'))]"
    }
  }
}