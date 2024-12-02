#Credit: https://github.com/scottdurow/magical-mystery-tour/tree/eca34c9429c2d13b642c18c255e95cc03eb2f901/src/core/solution/deployment-scripts
$managedIdentityName = "StarburstPlugins"
$applicationId = "00000000-0000-0000-0000-000000000000"
$tenantId = "00000000-0000-0000-0000-000000000000"
$pluginAssemblyId = "e2532ba3-70ae-4d42-8bd0-a8097c3d2c26"
### Initialization
# Empty pfx script
$pfxScript = ""

# Create an empty folder named pfx-scripts
$folderPath = Join-Path -Path (Get-Location) -ChildPath "pfx-scripts"
if (-not (Test-Path -Path $folderPath)) {
    New-Item -Path $folderPath -ItemType Directory
}

### List managed identities not created by SYSTEM
$pfxScript = @"
    ShowColumns(
        Filter(
            'Managed Identities',
            'Created By'.'Full Name' <> "SYSTEM"
        ),
        'ManagedIdentity Id',
        TenantId,
        ApplicationId,
        Name
    )
"@

$listManagedIdentitiesPfxScriptPath = Join-Path -Path $folderPath -ChildPath "list-managed-identities.pfx"

Set-Content -Path $listManagedIdentitiesPfxScriptPath -Value $pfxScript

pac pfx run --file $listManagedIdentitiesPfxScriptPath --echo

### Create a new managed identity
$pfxScript = @"
Collect(
    'Managed Identities',
    {
        Name: "$managedIdentityName",
        ApplicationId:GUID("$applicationId"),
        TenantId:GUID("$tenantId"),
        'Credential Source':'Credential Source (Managed Identities)'.IsManaged,
        'Subject Scope':'Subject Scope (Managed Identities)'.EnviornmentScope
    }
).'ManagedIdentity Id'
"@
    
$createManagedIdentityPfxScriptPath = Join-Path -Path $folderPath -ChildPath "create-managed-identity.pfx"

Set-Content -Path $createManagedIdentityPfxScriptPath -Value $pfxScript

pac pfx run --file $createManagedIdentityPfxScriptPath --echo

### List plug-in assemblies not created by SYSTEM
$pfxScript = @"
    AddColumns(
        ShowColumns(
            Filter(
                'Plug-in Assemblies',
                'Created By'.'Full Name' <> "SYSTEM"
            ),
            PluginAssemblyId,
            Name,
            ManagedIdentityId
        ),
        ManagedIdentityName,
        LookUp(
            'Managed Identities',
            'ManagedIdentity Id' = ThisRecord.'ManagedIdentity Id'
        ).Name
    )
"@

$listPluginAssembliesPfxScriptPath = Join-Path -Path $folderPath -ChildPath "list-plugin-assemblies.pfx"

Set-Content -Path $listPluginAssembliesPfxScriptPath -Value $pfxScript

pac pfx run --file $listPluginAssembliesPfxScriptPath --echo

### Link the managed identity to the plug-in assembly
$pfxScript = @"
    Patch(
        'Plug-in Assemblies',
        LookUp(
            'Plug-in Assemblies',
            PluginAssemblyId = GUID("$pluginAssemblyId")
        ),
        {
            ManagedIdentityId: LookUp(
                'Managed Identities',
                ApplicationId = GUID("$applicationId") && TenantId = GUID("$tenantId")
            )
        }
    )
"@

$linkManagedIdentityToPluginAssemblyPfxScriptPath = Join-Path -Path $folderPath -ChildPath "link-managed-identity-to-plugin-assembly.pfx"

Set-Content -Path $linkManagedIdentityToPluginAssemblyPfxScriptPath -Value $pfxScript

pac pfx run --file $linkManagedIdentityToPluginAssemblyPfxScriptPath --echo

### Delete folder with the pfx scripts
Remove-Item -Path $folderPath -Recurse -Force