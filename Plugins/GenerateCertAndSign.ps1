# Managed Identity Federated Credentials
# Credit: https://medium.com/rapha%C3%ABl-pothin/power-platforms-protection-managed-identity-for-dataverse-plug-ins-0ae0ed405338
# Issuer: https://environment_prefix.environment_suffix.environment.api.powerplatform.com/sts
# Audience: api://azureadtokenexchange
# Name: StarburstPlugins
# Subject Identifier: component:pluginassembly,thumbprint:cert_thumbprint_all_caps,environment:environment_id
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
$certificatePath = "C:\source\repos\StarburstVTProvider\Plugins\certificate.pfx"
$password = "certificate_password"
$name = "StarburstPlugins"
$friendlyName = "Starburst Plugins"
$dllPath = "C:\source\repos\StarburstVTProvider\Plugins\bin\Debug\net462\Starburst.Plugins.dll"
$fileDigestAlgorithm = "SHA256"
$cert = New-SelfSignedCertificate -Subject "CN=$name, O=corp, C=$name.com" -DnsName "www.$name.com" -Type CodeSigning -KeyUsage DigitalSignature -CertStoreLocation Cert:\CurrentUser\My -FriendlyName $friendlyName

# Note: The cert object contains a Thumbprint property we will use for the configuration of the federated credentials of the managed identity so keep it available

# 2. Set a password for the private key (optional)
$pw = ConvertTo-SecureString -String $password -Force -AsPlainText

# 3. Export the certificate as a PFX file
Export-PfxCertificate -Cert $cert -FilePath $certificatePath -Password $pw

# 4. Sign the plug-in assembly with the certificate
# Note: The signtool utility is part of the Windows SDK (Software Development Kit). You can find it in the installation directory of the Windows SDK. If you haven't already installed the Windows SDK, you can download it from here: https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/
$certificatePath = "C:\source\repos\StarburstVTProvider\Plugins\certificate.pfx"
$password = "certificate_password"
$name = "StarburstPlugins"
$friendlyName = "Starburst Plugins"
$dllPath = "C:\source\repos\StarburstVTProvider\Plugins\bin\Debug\net462\Starburst.Plugins.dll"
$fileDigestAlgorithm = "SHA256"


$signToolPath = "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\signtool.exe"
Start-Process -FilePath $signToolPath -ArgumentList "sign /fd $fileDigestAlgorithm /f `"$certificatePath`" /p `"$password`" `"$dllPath`"" -NoNewWindow -Wait