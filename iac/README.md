# iac

## Navigation

- [Resource Types](#resource-types)
- [Parameters](#parameters)
- [Outputs](#outputs)
- [Cross-referenced Modules](#cross-referenced-modules)

## Resource Types

| Resource Type | Existing |
| :-- | :-- |
| `Microsoft.Insights/diagnosticSettings@2021-05-01-preview` | No |
| `Microsoft.Web/staticSites/config@2023-12-01` | No |
| `Microsoft.Web/staticSites@2023-12-01` | Yes |

## Parameters

| Name | Type | Required | Description |
| :-- | :-- | :-- | :-- |
| `applicationInsightsName` | `string` | No | Name of the Application Insights component linked to the Static Web App. |
| `location` | `string` | No | Default Azure region for all resources. |
| `logAnalyticsWorkspaceName` | `string` | No | Name of the Log Analytics workspace backing Application Insights. |
| `resourceGroupName` | `string` | No | Name of the dedicated resource group. |
| `staticWebAppCustomDomain` | `string` | No | Custom domain for the Static Web App. |
| `staticWebAppName` | `string` | No | Name of the Static Web App. |
| `staticWebAppSku` | `string` | No | Static Web App SKU. |

### `location`

- Default value: `'westeurope'`

### `applicationInsightsName`

- Default value: `'appi-spora-web-prd-bec'`

### `logAnalyticsWorkspaceName`

- Default value: `'log-spora-web-prd-bec'`

### `resourceGroupName`

- Default value: `'rg-spora-web-prd-bec'`

### `staticWebAppCustomDomain`

- Default value: `'sporaleuven.be'`

### `staticWebAppName`

- Default value: `'stapp-spora-web-prd-bec'`

### `staticWebAppSku`

- Default value: `'Free'`

- Allowed values: `Free`, `Standard`

## Outputs

| Name | Type | Description |
| :-- | :-- | :-- |
| `applicationInsightsName` | `string` | Name of the Application Insights component linked to the Static Web App. |
| `resourceGroupName` | `string` | Name of the resource group that was created. |
| `staticWebAppHostName` | `string` | Default hostname of the deployed Static Web App. |
| `staticWebAppName` | `string` | Name of the Static Web App that was created. |

## Cross-referenced Modules

| Symbolic Name | Path | Description |
| :-- | :-- | :-- |
| `applicationInsights` | `br/public:avm/res/insights/component:0.8.0` | This module deploys a workspace-based Application Insights component. |
| `logAnalyticsWorkspace` | `br/public:avm/res/operational-insights/workspace:0.16.0` | This module deploys a Log Analytics Workspace. |
| `staticWebApp` | `br/public:avm/res/web/static-site:0.9.6` | This module deploys a Static Web App. |
