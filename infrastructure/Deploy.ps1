Param (
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
  [ValidateSet("None","Optional","Required")] [string] [Parameter(Mandatory = $true)] $TicketNumberWhileVoting,
  [string] [Parameter(Mandatory = $true)] $WaitingListCanVoteWithEmail,
  [string] [Parameter(Mandatory = $true)] $MinVotes,
  [string] [Parameter(Mandatory = $true)] $MaxVotes,
  [string] [Parameter(Mandatory = $true)] $StopSyncingSessionsFrom,
  [string] [Parameter(Mandatory = $true)] $TitoWebhookSecret,
  [string] [Parameter(Mandatory = $true)] $TitoEventId,
  [string] [Parameter(Mandatory = $true)] $TitoAccountId,
  [string] [Parameter(Mandatory = $true)] $StopSyncingTitoFrom,
  [string] [Parameter(Mandatory = $true)] $AppInsightsApplicationId,
  [string] [Parameter(Mandatory = $true)] $AppInsightsApplicationKey,
  [string] [Parameter(Mandatory = $true)] $StartSyncingAppInsightsFrom,
  [string] [Parameter(Mandatory = $true)] $StopSyncingAppInsightsFrom,
  [string] [Parameter(Mandatory = $true)] $StopSyncingAgendaFrom,
  [string] [Parameter(Mandatory = $true)] $SessionizeAgendaApiKey,
  [string] [Parameter(Mandatory = $true)] $IsSingleVoteEligibleForPrizeDraw,
  [string] [Parameter(Mandatory = $true)] $FeedbackAvailableFrom,
  [string] [Parameter(Mandatory = $true)] $FeedbackAvailableTo,
  [string] [Parameter(Mandatory = $true)] $EloPasswordPhrase,
  [string] [Parameter(Mandatory = $true)] $EloAllowedTimeInSecondsToSubmit,  
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
    "ticketNumberWhileVoting"           = $TicketNumberWhileVoting;
    "waitingListCanVoteWithEmail"       = $WaitingListCanVoteWithEmail;
    "minVotes"                          = $MinVotes;
    "maxVotes"                          = $MaxVotes;
    "titoWebhookSecret"                 = $TitoWebhookSecret;
    "titoEventId"                       = $TitoEventId;
    "titoAccountId"                     = $TitoAccountId;
    "stopSyncingTitoFrom"               = $StopSyncingTitoFrom;
    "appInsightsApplicationId"          = $AppInsightsApplicationId;
    "appInsightsApplicationKey"         = $AppInsightsApplicationKey;
    "startSyncingAppInsightsFrom"       = $StartSyncingAppInsightsFrom;
    "stopSyncingAppInsightsFrom"        = $StopSyncingAppInsightsFrom;
    "stopSyncingAgendaFrom"             = $StopSyncingAgendaFrom;
    "sessionizeAgendaApiKey"            = $SessionizeAgendaApiKey;
    "isSingleVoteEligibleForPrizeDraw"  = $IsSingleVoteEligibleForPrizeDraw
    "feedbackAvailableFrom"             = $FeedbackAvailableFrom;
    "feedbackAvailableTo"               = $FeedbackAvailableTo;
    "eloPasswordPhrase"                 = $EloPasswordPhrase;
    "eloAllowedTimeInSecondsToSubmit"   = $EloAllowedTimeInSecondsToSubmit;
    "eloUserSessionStoreAccountName"                = "$($ConferenceName)data$AppEnvironment".ToLower();
    "eloUserSessionStoreDatabaseName"               = "$($ConferenceName)data$AppEnvironment".ToLower();
    "eloUserSessionStoreContainerName"              = "$($ConferenceName)-$AppEnvironment-sessions".ToLower();
  }
}

try {
  #Requires -Version 7.0.0
  Set-StrictMode -Version "Latest"
  $ErrorActionPreference = "Stop"

  Import-Module Az.Accounts -Verbose:$false
  Import-Module Az.Websites

  $azureContext = Get-AzContext
  if (-not $azureContext) {
    throw "Execute Connect-AzAccount to establish your Azure connection"
  }
  if ($azureContext.Subscription.Id -ne $SubscriptionId) {
    Write-Verbose "Ensuring Azure context is set to specified Azure subscription $($SubscriptionId)"
    Set-AzContext -Tenant $TenantId -SubscriptionId $SubscriptionId
  }
  if ($azureContext.Subscription.Id -ne $SubscriptionId) {
    throw "Error trying to set Azure context to specified Azure subscription $($SubscriptionId)"
  }
    
  Write-Output "Ensuring resource group $ResourceGroupName exists"
  New-AzResourceGroup -Location $Location -Name $ResourceGroupName -Force | Out-Null

  Write-Output "Checking if it's the first run"
  $Parameters = Get-Parameters
  $firstRun = $false
  try {
    Get-AzResource -ResourceGroupName $ResourceGroupName -ResourceType "Microsoft.Web/sites" -ResourceName $Parameters["functionsAppName"] | Out-Null
  } catch {
    $firstRun = $true
  }

  Write-Output "Deploying to ARM"
  $result = New-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile "$PSScriptRoot\azuredeploy.json" -TemplateParameterObject $Parameters -Name ("$ConferenceName-$AppEnvironment-" + (Get-Date -Format "yyyy-MM-dd-HH-mm-ss")) -SkipTemplateParameterPrompt -Verbose
  Write-Output $result

  if ($firstRun) {
    Write-Warning "First run: working around Azure Functions WEBSITE_USE_ZIP first start limitations by restarting app"
    Restart-AzWebapp -ResourceGroupName $ResourceGroupName -Name $Parameters["functionsAppName"]
    Start-Sleep -Seconds 30
    $result = New-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile "$PSScriptRoot\azuredeploy.json" -TemplateParameterObject $Parameters -Name ("$ConferenceName-$AppEnvironment-" + (Get-Date -Format "yyyy-MM-dd-HH-mm-ss")) -Verbose
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
