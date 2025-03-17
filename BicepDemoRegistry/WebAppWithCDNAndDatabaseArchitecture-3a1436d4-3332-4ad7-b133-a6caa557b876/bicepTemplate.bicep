{
    "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
    "contentVersion": "1.0.0.0",
    "parameters": {
        "location": {
            "type": "string",
            "defaultValue": "[resourceGroup().location]",
            "metadata": {
                "description": "Location"
            }
        },
        "skuName": {
            "type": "string",
            "defaultValue": "Standard_S1"
        },
        "maxSizeBytes": {
            "type": "string",
            "defaultValue": "2147483648"
        }
    },
    "resources": [
        {
            "type": "Microsoft.Sql/servers",
            "apiVersion": "2021-05-01-preview",
            "name": "mySqlServer",
            "location": "[parameters('location')]",
            "properties": {
                "administratorLogin": "adminUser",
                "administratorLoginPassword": "adminPassword123!"
            },
            "sku": {
                "name": "S1",
                "tier": "Standard"
            }
        },
        {
            "type": "Microsoft.Sql/servers/databases",
            "apiVersion": "2021-05-01-preview",
            "name": "mySqlServer/myDatabase",
            "location": "[parameters('location')]",
            "properties": {
                "maxSizeBytes": "[parameters('maxSizeBytes')]"
            },
            "dependsOn": [
                "mySqlServer"
            ]
        },
        {
            "type": "Microsoft.Web/serverfarms",
            "apiVersion": "2021-02-01",
            "name": "myAppServicePlan",
            "location": "[parameters('location')]",
            "sku": {
                "name": "P1v2",
                "tier": "PremiumV2",
                "size": "P1v2"
            }
        },
        {
            "type": "Microsoft.Web/sites",
            "apiVersion": "2021-02-01",
            "name": "myWebApp",
            "location": "[parameters('location')]",
            "properties": {
                "serverFarmId": "[resourceId('Microsoft.Web/serverfarms', 'myAppServicePlan')]"
            },
            "dependsOn": [
                "myAppServicePlan"
            ]
        },
        {
            "type": "Microsoft.Cdn/profiles",
            "apiVersion": "2021-06-01",
            "name": "myCdnProfile",
            "location": "[parameters('location')]",
            "sku": {
                "name": "[parameters('skuName')]"
            }
        },
        {
            "type": "Microsoft.Cdn/profiles/endpoints",
            "apiVersion": "2021-06-01",
            "name": "myCdnProfile/myCdnEndpoint",
            "location": "[parameters('location')]",
            "properties": {
                "origins": [
                    {
                        "name": "myWebAppOrigin",
                        "properties": {
                            "hostName": "[reference(resourceId('Microsoft.Web/sites', 'myWebApp')).defaultHostName]"
                        }
                    }
                ]
            },
            "dependsOn": [
                "myCdnProfile",
                "myWebApp"
            ]
        },
        {
            "type": "Microsoft.Storage/storageAccounts",
            "apiVersion": "2021-02-01",
            "name": "mystorageaccount",
            "location": "[parameters('location')]",
            "kind": "StorageV2",
            "sku": {
                "name": "Standard_LRS"
            }
        },
        {
            "type": "Microsoft.Storage/storageAccounts/queueServices/queues",
            "apiVersion": "2021-02-01",
            "name": "mystorageaccount/default/myqueue",
            "dependsOn": [
                "mystorageaccount"
            ]
        },
        {
            "type": "Microsoft.Storage/storageAccounts/blobServices/containers",
            "apiVersion": "2021-02-01",
            "name": "mystorageaccount/default/mycontainer",
            "dependsOn": [
                "mystorageaccount"
            ]
        },
        {
            "type": "Microsoft.Insights/components",
            "apiVersion": "2021-05-01-preview",
            "name": "myAppInsights",
            "location": "[parameters('location')]",
            "kind": "web",
            "properties": {
                "Application_Type": "web"
            }
        },
        {
            "type": "Microsoft.Web/sites",
            "apiVersion": "2021-02-01",
            "name": "myFunctionApp",
            "location": "[parameters('location')]",
            "kind": "functionapp",
            "properties": {
                "serverFarmId": "[resourceId('Microsoft.Web/serverfarms', 'myAppServicePlan')]",
                "httpsOnly": true,
                "siteConfig": {
                    "appSettings": [
                        {
                            "name": "APPINSIGHTS_INSTRUMENTATIONKEY",
                            "value": "[reference(resourceId('Microsoft.Insights/components', 'myAppInsights')).InstrumentationKey]"
                        },
                        {
                            "name": "AzureWebJobsStorage",
                            "value": "DefaultEndpointsProtocol=https;AccountName=[variables('storageAccountName')];AccountKey=[listKeys(resourceId('Microsoft.Storage/storageAccounts', 'mystorageaccount'), '2019-06-01').keys[0].value]"
                        }
                    ]
                }
            },
            "dependsOn": [
                "myAppServicePlan",
                "myAppInsights",
                "mystorageaccount"
            ]
        },
        {
            "type": "Microsoft.Cache/Redis",
            "apiVersion": "2020-06-01",
            "name": "myRedisCache",
            "location": "[parameters('location')]",
            "properties": {
                "sku": {
                    "name": "Standard",
                    "family": "C",
                    "capacity": 1
                }
            }
        },
        {
            "type": "Microsoft.Search/searchServices",
            "apiVersion": "2020-03-13",
            "name": "mySearchService",
            "location": "[parameters('location')]",
            "sku": {
                "name": "standard"
            },
            "properties": {
                "partitionCount": 1,
                "replicaCount": 1
            }
        }
    ],
    "outputs": {
        "sqlServerName": {
            "type": "string",
            "value": "mySqlServer"
        },
        "sqlDatabaseName": {
            "type": "string",
            "value": "myDatabase"
        },
        "webAppName": {
            "type": "string",
            "value": "myWebApp"
        },
        "cdnProfileName": {
            "type": "string",
            "value": "myCdnProfile"
        },
        "cdnEndpointName": {
            "type": "string",
            "value": "myCdnEndpoint"
        },
        "storageAccountName": {
            "type": "string",
            "value": "mystorageaccount"
        },
        "queueName": {
            "type": "string",
            "value": "myqueue"
        },
        "blobContainerName": {
            "type": "string",
            "value": "mycontainer"
        },
        "appInsightsName": {
            "type": "string",
            "value": "myAppInsights"
        },
        "functionAppName": {
            "type": "string",
            "value": "myFunctionApp"
        },
        "redisName": {
            "type": "string",
            "value": "myRedisCache"
        },
        "searchServiceName": {
            "type": "string",
            "value": "mySearchService"
        }
    }
}