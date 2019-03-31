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
  [string] [Parameter(Mandatory = $true)] $TitoApiBearerToken,
  [string] [Parameter(Mandatory = $true)] $SubmissionsAvailableFrom,
  [string] [Parameter(Mandatory = $true)] $SubmissionsAvailableTo,
  [string] [Parameter(Mandatory = $true)] $AnonymousSubmissions,
  [string] [Parameter(Mandatory = $true)] $ConferenceInstance,
  [string] [Parameter(Mandatory = $true)] $VotingAvailableFrom,
  [string] [Parameter(Mandatory = $true)] $VotingAvailableTo,
  [string] [Parameter(Mandatory = $true)] $MinVotes,
  [string] [Parameter(Mandatory = $true)] $MaxVotes,
  [string] [Parameter(Mandatory = $true)] $StopSyncingSessionsFrom,
  [string] [Parameter(Mandatory = $true)] $TitoEventId,
  [string] [Parameter(Mandatory = $true)] $TitoAccountId,
  [string] [Parameter(Mandatory = $true)] $StopSyncingTitoFrom,
  [string] [Parameter(Mandatory = $true)] $AppInsightsApplicationId,
  [string] [Parameter(Mandatory = $true)] $AppInsightsApplicationKey,
  [string] [Parameter(Mandatory = $true)] $StartSyncingAppInsightsFrom,
  [string] [Parameter(Mandatory = $true)] $StopSyncingAppInsightsFrom,
  [string] [Parameter(Mandatory = $true)] $StopSyncingAgendaFrom,
  [string] [Parameter(Mandatory = $true)] $SessionizeAgendaApiKey,
  [string] $ResourceGroupName = "$ConferenceName-backend-$AppEnvironment"
)

function Get-Parameters() {
  return @{
    "serverFarmResourceId"              = "/subscriptions/$SubscriptionId/resourceGroups/$AppServicePlanResourceGroup/providers/Microsoft.Web/serverfarms/$AppServicePlanName";
    "functionsAppName"                  = "$ConferenceName-functions-$AppEnvironment".ToLower();
    "storageName"                       = "$($ConferenceName)functions$AppEnvironment".ToLower();
    "storageType"                       = "Standard_LRS";
    "dataStorageName"                   = "$($ConferenceName)data$AppEnvironment".ToLower();
    "dataStorageType"                   = "Standard_GRS";
    "stopSyncingSessionsFrom"           = $StopSyncingSessionsFrom;
    "newSessionNotificationLogicAppUrl" = $NewSessionNotificationLogicAppUrl;
    "deploymentZipUrl"                  = $DeploymentZipUrl;
    "sessionizeApiKey"                  = $SessionizeApiKey;
    "titoApiBearerToken"                = $TitoApiBearerToken;
    "submissionsAvailableFrom"          = $SubmissionsAvailableFrom;
    "submissionsAvailableTo"            = $SubmissionsAvailableTo;
    "anonymousSubmissions"              = $AnonymousSubmissions;
    "conferenceInstance"                = $ConferenceInstance;
    "votingAvailableFrom"               = $VotingAvailableFrom;
    "votingAvailableTo"                 = $VotingAvailableTo;
    "minVotes"                          = $MinVotes;
    "maxVotes"                          = $MaxVotes;
    "titoEventId"                       = $TitoEventId;
    "titoAccountId"                     = $TitoAccountId;
    "stopSyncingtitoFrom"               = $StopSyncingTitoFrom;
    "appInsightsApplicationId"          = $AppInsightsApplicationId;
    "appInsightsApplicationKey"         = $AppInsightsApplicationKey;
    "startSyncingAppInsightsFrom"       = $StartSyncingAppInsightsFrom;
    "stopSyncingAppInsightsFrom"        = $StopSyncingAppInsightsFrom;
    "stopSyncingAgendaFrom"             = $StopSyncingAgendaFrom;
    "sessionizeAgendaApiKey"            = $SessionizeAgendaApiKey;
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

  Write-Output "Checking if it's the first run"
  $Parameters = Get-Parameters
  $firstRun = $false
  try {
    Get-AzureRmResource -ResourceGroupName $ResourceGroupName -ResourceType "Microsoft.Web/sites" -ResourceName $Parameters["functionsAppName"] | Out-Null
  } catch {
    $firstRun = $true
  }

  Write-Output "Deploying to ARM"
  $result = New-AzureRmResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile "$PSScriptRoot\azuredeploy.json" -TemplateParameterObject $Parameters -Name ("$ConferenceName-$AppEnvironment-" + (Get-Date -Format "yyyy-MM-dd-HH-mm-ss")) -ErrorAction Continue -Verbose
  Write-Output $result

  if ($firstRun) {
    Write-Warning "First run: working around Azure Functions WEBSITE_USE_ZIP first start limitations by restarting app"
    Restart-AzureRmWebApp -ResourceGroupName $ResourceGroupName -Name $Parameters["functionsAppName"]
    Start-Sleep -Seconds 30
    $result = New-AzureRmResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile "$PSScriptRoot\azuredeploy.json" -TemplateParameterObject $Parameters -Name ("$ConferenceName-$AppEnvironment-" + (Get-Date -Format "yyyy-MM-dd-HH-mm-ss")) -ErrorAction Continue -Verbose
    Write-Output $result
  }

  if ((-not $result) -or ($result.ProvisioningState -ne "Succeeded")) {
    throw "Deployment failed"
  }

}
catch {
  Write-Error $_ -ErrorAction Continue
  exit 1
}
