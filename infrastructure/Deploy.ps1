Param (
  [string]
  [Parameter(ParameterSetName = "SpecifyServicePrincipal", Mandatory = $true)]
  $ServicePrincipalId,

  [string]
  [Parameter(ParameterSetName = "SpecifyServicePrincipal", Mandatory = $true)]
  $ServicePrincipalPassword,

  [switch]
  [Parameter(ParameterSetName = "AlreadyLoggedIn", Mandatory = $true)]
  $AlreadyLoggedIn,

  [string] [Parameter(Mandatory = $true)] $SubscriptionId,
  [string] [Parameter(Mandatory = $true)] $TenantId,
  [string] [Parameter(Mandatory = $true)] $Location,
  [string] [Parameter(Mandatory = $true)] $ConferenceName,
  [string] [Parameter(Mandatory = $true)] $AppEnvironment,
  [string] [Parameter(Mandatory = $true)] $AppServicePlanResourceGroup,
  [string] [Parameter(Mandatory = $true)] $AppServicePlanName,
  [string] [Parameter(Mandatory = $true)] $NewSessionNotificationLogicAppUrl,
  [string] [Parameter(Mandatory = $true)] $DeploymentZipUrl,
  [string] [Parameter(Mandatory = $true)] $SessionizeApiKey,
  [string] [Parameter(Mandatory = $true)] $EventbriteApiBearerToken,
  [string] $SessionizeReadModelSyncSchedule = "0 */5 * * * *",
  [string] $ResourceGroupName = "$ConferenceName-backend-$AppEnvironment"
)

function Get-Parameters() {
  return @{
    "serverFarmResourceId"              = "/subscriptions/$SubscriptionId/resourceGroups/$AppServicePlanResourceGroup/providers/Microsoft.Web/serverfarms/$AppServicePlanName";
    "functionsAppName"                  = "$ConferenceName-functions-$AppEnvironment".ToLower();
    "storageName"                       = "$($ConferenceName)functions$AppEnvironment".ToLower();
    "storageType"                       = "Standard_LRS";
    "newSessionNotificationLogicAppUrl" = $NewSessionNotificationLogicAppUrl;
    "deploymentZipUrl"                  = $DeploymentZipUrl;
    "sessionizeApiKey"                  = $SessionizeApiKey;
    "sessionizeReadModelSyncSchedule"   = $SessionizeReadModelSyncSchedule;
    "eventbriteApiBearerToken"          = $EventbriteApiBearerToken;
  }
}

try {
  Set-StrictMode -Version "Latest"
  $ErrorActionPreference = "Stop"

  if (-not $AlreadyLoggedIn) {
    Write-Output "Authenticating to ARM as service principal $ServicePrincipalId"
    $securePassword = ConvertTo-SecureString $ServicePrincipalPassword -AsPlainText -Force
    $servicePrincipalCredentials = New-Object System.Management.Automation.PSCredential ($ServicePrincipalId, $securePassword)
    Login-AzureRmAccount -ServicePrincipal -TenantId $TenantId -Credential $servicePrincipalCredentials | Out-Null
  }
    
  Write-Output "Selecting subscription $SubscriptionId"
  Select-AzureRmSubscription -SubscriptionId $SubscriptionId -TenantId $TenantId | Out-Null

  Write-Output "Ensuring resource group $ResourceGroupName exists"
  New-AzureRmResourceGroup -Location $Location -Name $ResourceGroupName -Force | Out-Null

  Write-Output "Deploying to ARM"
  $Parameters = Get-Parameters
  $result = New-AzureRmResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile "$PSScriptRoot\azuredeploy.json" -TemplateParameterObject $Parameters -Name ("$ConferenceName-$AppEnvironment-" + (Get-Date -Format "yyyy-MM-dd-HH-mm-ss")) -ErrorAction Continue -Verbose
  Write-Output $result
  if ((-not $result) -or ($result.ProvisioningState -ne "Succeeded")) {
    throw "Deployment failed"
  }

}
catch {
  Write-Error $_ -ErrorAction Continue
  exit 1
}
