# Vantum.AppKit.Generators

**Roslyn Incremental Source Generator** that scans compiled code for AppKit attributes and produces manifest constants describing each app module.

## Overview

This generator enables **code-first metadata** for Vantum modules. By decorating your classes with attributes from `Vantum.AppKit`, the generator automatically produces JSON manifests that describe your module's:

- **Metadata** (name, version, description)
- **Permissions** (canonical permission list)
- **HTTP Routes** (endpoints and required permissions)
- **Settings** (configuration surface)
- **Domain Events** (published/subscribed)

## Installation

Add the generator to your project:

```xml
<PackageReference Include="Vantum.AppKit.Generators" Version="0.1.0"
  PrivateAssets="all"
  IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
<PackageReference Include="Vantum.AppKit" Version="0.1.2" />
```

## Usage

### 1. Define an App Module

```csharp
using Vantum.AppKit;

[AppModule(Name = "ContactsManagement", DisplayName = "Contacts",
           Version = "0.1.0", Description = "Manage contacts")]
[AppPermissions("Contacts.Read", "Contacts.Write")]
[AppPublishesEvents("ContactCreated", "ContactUpdated")]
[AppSubscribesEvents("UserCreated")]
public class ContactsModule
{
    [AppSetting("Contacts.DefaultGroup", "String",
                DefaultValue = "General",
                Description = "Default group for new contacts")]
    public const string DefaultGroupSetting = "Contacts.DefaultGroup";

    [AppRoute("GET", "/api/contacts", RequiredPermission = "Contacts.Read")]
    public void GetContacts() { }

    [AppRoute("POST", "/api/contacts", RequiredPermission = "Contacts.Write")]
    public void CreateContact() { }
}
```

### 2. Generated Output

The generator produces a class in the `Vantum.Generated` namespace:

```csharp
namespace Vantum.Generated
{
    public static class Manifest_ContactsManagement
    {
        public const string Json = @"{
  ""name"": ""ContactsManagement"",
  ""displayName"": ""Contacts"",
  ""version"": ""0.1.0"",
  ""description"": ""Manage contacts"",
  ""permissions"": [
    ""Contacts.Read"",
    ""Contacts.Write""
  ],
  ""routes"": [
    {""method"": ""GET"", ""path"": ""/api/contacts"", ""requiredPermission"": ""Contacts.Read""},
    {""method"": ""POST"", ""path"": ""/api/contacts"", ""requiredPermission"": ""Contacts.Write""}
  ],
  ""settings"": [
    {""key"": ""Contacts.DefaultGroup"", ""type"": ""String"", ""defaultValue"": ""General"", ""description"": ""Default group for new contacts""}
  ],
  ""eventsPublished"": [
    ""ContactCreated"",
    ""ContactUpdated""
  ],
  ""eventsSubscribed"": [
    ""UserCreated""
  ],
  ""dependencies"": []
}";
    }
}
```

### 3. Runtime Consumption

App Management can discover all manifests at runtime:

```csharp
var manifestTypes = AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(a => a.GetTypes())
    .Where(t => t.Namespace == "Vantum.Generated" && t.Name.StartsWith("Manifest_"));

foreach (var type in manifestTypes)
{
    var jsonField = type.GetField("Json");
    var json = jsonField?.GetValue(null) as string;
    // Deserialize and process manifest...
}
```

## Features

✅ **Incremental compilation** – only regenerates when attributes change  
✅ **Multi-module support** – handles multiple `[AppModule]` classes  
✅ **Deterministic output** – stable JSON property ordering  
✅ **Graceful defaults** – missing optional values get sensible fallbacks  
✅ **Name sanitization** – handles special characters in module names  
✅ **Zero runtime dependencies** – pure compile-time generator

## Supported Attributes

| Attribute               | Target         | Purpose                                      |
| ----------------------- | -------------- | -------------------------------------------- |
| `[AppModule]`           | Class          | Module metadata (name, version, description) |
| `[AppPermissions]`      | Class          | List of canonical permissions                |
| `[AppRoute]`            | Method         | HTTP endpoint definition                     |
| `[AppSetting]`          | Field/Property | Configuration setting                        |
| `[AppPublishesEvents]`  | Class          | Events this module publishes                 |
| `[AppSubscribesEvents]` | Class          | Events this module consumes                  |

## JSON Schema

```json
{
  "name": "string",
  "displayName": "string",
  "version": "string",
  "description": "string",
  "permissions": ["string"],
  "routes": [
    {
      "method": "string",
      "path": "string",
      "requiredPermission": "string?"
    }
  ],
  "settings": [
    {
      "key": "string",
      "type": "string",
      "defaultValue": "string?",
      "description": "string?"
    }
  ],
  "eventsPublished": ["string"],
  "eventsSubscribed": ["string"],
  "dependencies": ["string"]
}
```

## Defaults

- `displayName` defaults to `name` if omitted
- `version` defaults to `"0.1.0"` if omitted
- Module name is sanitized to valid C# identifier (alphanumeric + `_`)

## Building

```powershell
dotnet build
dotnet pack
```

The package will include the generator DLL in `analyzers/dotnet/cs/`.

## License

AGPL-3.0-only
