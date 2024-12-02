# StarburstVTProvider

## Starburst Data Provider

The Starburst virtual table data provider is a generic read-only provider for Starburst databases. Users can create read-only Virtual Tables in Dataverse based on Views and Tables in Starburst using the data provider and a data source. The provider is a C# Plugin that is registered in a Dataverse environment and can be transported between environments as part of a solution. The provider uses the Starburst provided statement endpoint to request data from Starburst and translates Fetch XML queries in Dataverse to a corresponding SQL statement.

## Starburst Data Source

The Data Source contains information about the Data Source for this Data Provider, including the endpoint url, username and password. You can have many Data Sources for a particular Data Provider. For example, you may have different endpoints or credentials for a given Data Source. When handling authorization using different usernames and passwords you can configure multiple Data Sources and specify the username / password for each. Then when you create a Virtual Table, you’ll select the appropriate Data Source that would have access to the source tables you are mapping to the Virtual Table. Data Sources are stored as Dataverse records and are solution aware so they can be included in a solution when exporting / deploying a solution.

### Security

Both authentication and authorization are handled by a username and password configured on the Data Source for the Data Provider. Virtual Tables in Dataverse do not provide Role Based Access Controls. Therefore, when using the provider to provide access to specific parts of the Starburst schema, a user will need to be created and given specific access to that schema. When configuring a new Data Source, the username and password can be configured on the Data Source. When creating the Virtual Table, you will select the appropriate Data Source that has access to the View or Table in the Starburst schema. The password can be provided either as plain text (for testing) on the Data Source or as a URL to the key vault secret that contains the password when Managed Identities are configured in the environment.

### Updating the Data Provider

To update the Data Provider, use the following steps.

1.      Open the Plugin Project Folder in VS Code or Visual Studio

2.      Make any changes necessary

3.      From a command prompt run dotnet build Starburst.Plugins.csproj

4.      If you are planning to use managed identities for password security use the following steps

a.      If you don’t already have a valid signed certificate you can generate a self-signed certificate using the following steps (NOTE: For production scenarios it is necessary to use a valid root certificate. Self-signed certificates should only be used in development and testing scenarios.)

i.      Update./Scripts/GenerateCertAndSign.ps1

1.      certificatePath variable – Update this based on the root of your project

2.      password variable – set this to a secure value to secure your certificate.

3.      name variable – it’s recommended to leave this value as is for self-signed certificates for simplicity in downstream processes but you can update it if you want to any valid name.

4.      friendlyName variable – Same as above

5.      dllPath variable – set this to the path to the dll that was built in step 3.

6.      signToolPath variable – this path may differ in your environment. Set this to the location of the signtool.exe in your environment. NOTE: You may need to install the Windows Development Kit on your machine if the tool doesn’t exist.

ii.      Save the script.

iii.      Run the script from a Powershell terminal.

b.      If you already have a signed valid certificate, you can use SignPlugin.ps1 to sign the assembly with your existing certificate.

i.      Update./Scripts/GenerateCertAndSign.ps1

1.      certificatePath variable – Update this based on the root of your project

2.      password variable – set this to a secure value to secure your certificate.

3.      dllPath variable – set this to the path to the dll that was built in step 3.

ii.      Save the script.

iii.      Run the script from a Powershell terminal.

c.      Create a Power Platform CLI auth profile. (To install pac cli go to https://learn.microsoft.com/en-us/power-platform/developer/cli/introduction?tabs=windows#install-microsoft-power-platform-cli)

i.      Run pac auth create --environment \[your\_environment\_id\]

ii.      Follow the prompts to create an auth profile

d.      Run pac plugin push --pluginFile .\\bin\\Debug\\net462\\Starburst.Plugins.dll --pluginId e2532ba3-70ae-4d42-8bd0-a8097c3d2c26. NOTE: The pluginId above assumes the plugin has already been registered via solution import in the environment.

## Using Key Vault for Password

For key vault to be accessed from the plugin you must enable and configure managed identities in the environment. This involves configuration of an App Registration in Azure, Key Vault Configuration to give the App Registration Key Vault User permissions and Dataverse configuration to add the Managed Identity. For more information on enabling and configuring Managed Identities see [https://learn.microsoft.com/en-us/power-platform/release-plan/2024wave1/power-platform-governance-administration/use-managed-identities-dataverse-plug-ins](https://learn.microsoft.com/en-us/power-platform/release-plan/2024wave1/power-platform-governance-administration/use-managed-identities-dataverse-plug-ins)

When setting Password on the Datasource, rather than supplying the password, supply the Key Vault Secret API Url (e.g. [https://mykeyvault.vault.azure.net/secrets/StarburstPassword/1db03aee41404bbc89c66ece9407fc9e?api-version=7.4](https://mykeyvault.vault.azure.net/secrets/StarburstPassword/1db03aee41404bbc89c66ece9407fc9e?api-version=7.4)). Be sure to include the api-version request parameter when setting this value.

## Troubleshooting

Troubleshooting Plugins can be difficult. Unit tests can only simulate the real-life plugin environment so well. Plugin Tracing is generally the best way to see errors as they occur in the Plugin system. For more information on Plugin Tracing see [https://learn.microsoft.com/en-us/power-apps/developer/data-platform/logging-tracing](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/logging-tracing). Trace statements can be added to the code to help with troubleshooting issues when using the data provider. Where logging is used, you’ll see statements in the data provider code like the following:

1\. context.Trace($"Query: {queryText}");

I recommend familiarizing yourself with XrmToolBox in general and the Plugin Trace Viewer plugin for XrmToolBox specifically to make troubleshooting easier.

## Limitations / Know Issues

1.      Tables in Dataverse expect key columns to be Guids. However, most other systems don’t use Guids as their key column. The Data Provider uses a couple of different techniques to coerce a key column into a Guid.

a.      If the column in an integer column, then the Data Provider will convert the integer value to a Guid by prepending zeroes to the value and formatting correctly with dashes.

b.      If the column is other than an integer value and not a guid the Data Provider will attempt to convert the value to a string and then convert the characters of the string to their hexadecimal value and combine them before prefixing the converted string with 0’s. This translation is limited based on the length of the string value which when converted may exceed the maximum length of a Guid. In this case a new guid is assigned to the record so it can be displayed in a list view. However, loading the row into a form will fail because the record’s key cannot be converted back to its original value from the data source.

2.      Virtual Tables don’t allow for Currency Columns. For limitations of Virtual Tables see (https://learn.microsoft.com/en-us/power-apps/maker/data-platform/create-edit-virtual-entities#considerations-when-you-use-virtual-tables)

3.      Tables in Dataverse must have a ‘Name’ column, the Name column or sometimes referred to as the Primary Attribute is a human readable text column that is generally used to uniquely identify a record when displaying it as a Lookup on another table’s form. For example, the Name column for the Contact table is the Full Name of the Contact. This is true for Virtual tables as well and you will need to map the name column to some column on your source table.

4.      Translation of FetchXML (Dataverse’s native query language) to SQL is not a native capability in the platform and therefore requires 3rd party libraries to do the translation. The provider uses source code from an open-source library called Sql4CDS ([GitHub - MarkMpn/Sql4Cds: SQL 4 CDS core engine and XrmToolbox tool](https://github.com/MarkMpn/Sql4Cds)) for this translation. However, this library was built to translate to SQL Server SQL syntax where Starburst uses MySQL SQL syntax. As such, some modifications were needed to the original source code and the data provider was built by including the source code rather than importing the library and overriding the implementation. It may be possible to remove the source code and use the library as an alternative with overriding logic for specific syntax differences but is not part of the current implementation. The TOP / LIMIT statement is one example where the SQL Server and MySQL syntaxes differ. However, there may be other cases that aren’t handled, and the translation capabilities were not exhaustively tested. Some FetchXML statements may not be handled perfectly as part of this translation and may require additional work to handle them.
