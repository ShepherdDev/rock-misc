<# Right-click in Explorer and select 'Run In Powershell'
 #
 # Prepares a new project for use with either RockIt development kit or a production Rock
 # installation by setting up windows hard links between the project folders and their corresponding
 # folders under the RockIt or RockWeb folders. This allows you to maintain version control through
 # git or another system of the plugin all in one folder tree while still having them show up directly
 # in Rock.
 #
 # Note: Hard links requires NTFS and that both folders exist on the same drive letter.
 #
 # Expected project format:
 #  com.yourchurch.project_name/				- Linked to RockIt/com.yourchurch.project_name/
 #   |- com.yourchurch.project_name.csproj		- Used to determine your church domain and the project name.
 #   |- Controls/								- Linked to RockIt/RockWeb/Plugins/com_yourchurch/project_name/
 #   |- Themes/*								- Linked to RockIt/RockWeb/Themes/*/
 #   \- Webhooks/*								- Linked to RockIt/RockWeb/Webhooks/*
 #
 # The Controls and Themes folders should be excluded from your project. You will be able to access them
 # via the RockWeb project (you may need to refresh solution folders after running this script). If you do
 # include them you will get errors as they will be compiled into the project DLL.
 #
 # You can run this script multiple times without errors. So if you add a new theme then you can re-run this
 # script to link it into the RockIt folder.
 #
 # When creating a new theme it is recommended that you use Windows Explorer to copy the Stark theme from the
 # RockWeb/Themes/ folder into your project's Themes/ folder (with a new name).
 #
 # Version History:
 #
 #   Version 1.1:
 #
 #     Add support for hard linking Webhooks.
 #
 #   Version 1.0:
 #
 #     Builds a hard link from the main project folder (containing this script) to the RockIt folder
 #     for easy ability of adding the project into theRockIt solution.
 #
 #     Builds a hard link from the optional Controls folder to the RockWeb Plugins folder.
 #
 #     Builds hard links from any themes in the optional Themes folder to the RockWeb Themes folder.
 #
 #>


<#
 # Ask the user for a folder.
 #>
Function Select-FolderDialog
{
    param([string]$Description="Select Folder",[string]$RootFolder="C:\")

	$app = new-object -com Shell.Application
	$folder = $app.BrowseForFolder(0, $Description, 0, $RootFolder)

    Return $folder.Self.Path
}

<#
 # Ask the user where their RockIt folder is.
 #>
$RockItPath = Select-FolderDialog("Select the RockIt folder for development or Rock root IIS folder for production.")
if ( $RockItPath -eq $null )
{
	Return
}

<#
 # Check if this is a RockIt folder or a production Rock site.
 #>
$RockWebPath = Join-Path $RockItPath "RockWeb"
$HasRockIt = 1
if ( !(Test-Path $RockWebPath) )
{
	$RockWebPath = $RockItPath
	$HasRockIt = 0
}

<#
 # Get some helpful variables for path references.
 #>
$ProjectPath = Split-Path (Get-Variable MyInvocation).Value.MyCommand.Path
$ProjectControlsPath = Join-Path $ProjectPath "Controls"
$ProjectThemesPath = Join-Path $ProjectPath "Themes"
$ProjectWebhooksPath = Join-Path $ProjectPath "Webhooks"
$ProjectFullName = (Get-ChildItem -Path $ProjectPath -Filter *.csproj).Name
$ProjectFullName = $ProjectFullName.Substring(0, $ProjectFullName.Length - 7)
$ProjectOrganziation = $ProjectFullName.Substring(0, $ProjectFullName.LastIndexOf('.'))
$ProjectName = $ProjectFullName.Substring($ProjectFullName.LastIndexOf('.') + 1)
$RockWebPluginsPath = Join-Path $RockWebPath "Plugins"
$RockWebThemesPath = Join-Path $RockWebPath "Themes"
$RockWebWebhooksPath = Join-Path $RockWebPath "Webhooks"
$RockWebPluginOrganizationPath = Join-Path $RockWebPluginsPath $ProjectOrganziation.Replace(".", "_")
$RockWebPluginProjectPath = Join-Path $RockWebPluginOrganizationPath $ProjectName

<#
 # Make sure this is a RockIt path.
 #>
if ( !(Test-Path $RockWebPluginsPath) -or !(Test-Path $RockWebThemesPath) -or !(Test-Path $RockWebWebhooksPath) )
{
	throw "Path does not appear to be a valid RockIt path or Rock production path."
}

<#
 # Create any intermediate folders we need.
 #>
if ( !(Test-Path $RockWebPluginOrganizationPath) )
{
	New-Item -Path $RockWebPluginsPath -Name $ProjectOrganziation.Replace(".", "_") -ItemType directory
}

<#
 # Hard link the from the Plugins folder to the Project Controls.
 #>
if ( Test-Path $ProjectControlsPath )
{
	if ( !(Test-Path $RockWebPluginProjectPath) )
	{
		cmd /c mklink /J "$RockWebPluginProjectPath" "$ProjectControlsPath"
	}
}

<#
 # Hard link each theme if it doesn't already exist.
 #>
if ( Test-Path $ProjectThemesPath )
{
	$themes = (Get-ChildItem -Path $ProjectThemesPath)
	Foreach ($theme in $themes)
	{
		$SourceTheme = Join-Path $ProjectThemesPath $theme
		$TargetTheme = Join-Path $RockWebThemesPath $theme

		if ( !(Test-Path $TargetTheme) )
		{
			cmd /c mklink /J "$TargetTheme" "$SourceTheme"
		}
	}
}

<#
 # Hard link each webhook if it doesn't already exist.
 #>
if ( Test-Path $ProjectWebhooksPath )
{
	$webhooks = (Get-ChildItem -Path $ProjectWebhooksPath *.ashx)
	Foreach ($webhook in $webhooks)
	{
		$SourceWebhook = Join-Path $ProjectWebhooksPath $webhook
		$TargetWebhook = Join-Path $RockWebWebhooksPath $webhook
		Write-Host $SourceWebhook
		Write-Host $TargetWebhook

		if ( !(Test-Path $TargetWebhook) )
		{
			cmd /c mklink /H "$TargetWebhook" "$SourceWebhook"
		}
	}
}

<#
 # Hard link the actual project path if this is a RockIt install.
 #>
if ( $HasRockIt -eq 1 )
{
	$TargetProjectPath = Join-Path $RockItPath $ProjectFullName
	if ( !(Test-Path $TargetProjectPath) )
	{
		cmd /c mklink /J "$TargetProjectPath" "$ProjectPath"
	}
}

Read-Host -Prompt "All finished, press enter to close."
