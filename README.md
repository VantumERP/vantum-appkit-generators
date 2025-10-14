# Vantum.AppKit.Generators

**Roslyn Incremental Source Generator** that scans compiled code for AppKit attributes and produces manifest constants describing each app module.

## Overview

This generator enables **code-first metadata** for Vantum modules. By decorating your classes with attributes from `Vantum.AppKit`, the generator automatically produces JSON manifests that describe your module's:

- **Metadata** (name, version, description)
- **Permissions** (canonical permission list)
- **HTTP Routes** (endpoints and required permissions)
- **Settings** (configuration surface)
- **Events** (published and subscribed domain events)
- **Dependencies** (required modules with version ranges)

## Installation

Add the generator to your project:

```xml
<PackageReference Include="Vantum.AppKit.Generators" Version="0.2.0"
  PrivateAssets="all"
  IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
<PackageReference Include="Vantum.AppKit" Version="0.1.5" />
```

## Usage

This generator uses **auto-inferred routes** from ASP.NET Core MVC attributes. Keep your existing controller attributes (`[HttpGet]`, `[Route]`, etc.) and let the generator automatically infer HTTP methods and paths.

### Step 1: Define a Module Anchor

Create an anchor class for your module's metadata, permissions, and settings:

```csharp
using Vantum.AppKit;

[AppModule(Name = "Contacts", DisplayName = "Contacts Management",
           Version = "1.0.0", Description = "Manage customer contacts")]
[AppPermissions("Contacts.Read", "Contacts.Write", "Contacts.Delete")]
[AppPublishesEvents("Contact.Created", "Contact.Updated", "Contact.Deleted")]
[AppSubscribesEvents("Company.Created")]
[AppDependsOn("Tenancy", ">=1.0 <2.0")]
[AppDependsOn("UserManagement", "^1.0")]
public static class ContactsModuleAnchor
{
    [AppSetting("Contacts.DefaultPageSize", AppSettingType.Int,
                DefaultValue = "25",
                Description = "Default number of contacts per page")]
    public const string DefaultPageSize = "Contacts.DefaultPageSize";
}
```

### Step 2: Mark Controllers with `[AppBelongsTo]`

Annotate your existing ASP.NET Core controllers with `[AppBelongsTo]` to associate them with a module:

```csharp
using Microsoft.AspNetCore.Mvc;
using Vantum.AppKit;

[ApiController]
[Route("api/contacts")]
[AppBelongsTo("Contacts")]
public class ContactsController : ControllerBase
{
    [HttpGet]
    [AppRouteAuto(RequiredPermission = "Contacts.Read")]
    public async Task<ActionResult> List() { ... }

    [HttpGet("{id}")]
    [AppRouteAuto(RequiredPermission = "Contacts.Read")]
    public async Task<ActionResult> Get(Guid id) { ... }

    [HttpPost]
    [AppRouteAuto(RequiredPermission = "Contacts.Write")]
    public async Task<ActionResult> Create([FromBody] CreateContactDto dto) { ... }

    [HttpPut("{id}")]
    [AppRouteAuto(RequiredPermission = "Contacts.Write")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateContactDto dto) { ... }

    [HttpDelete("{id}")]
    [AppRouteAuto(RequiredPermission = "Contacts.Delete")]
    public async Task<ActionResult> Delete(Guid id) { ... }
}
```

### Step 3: Generator Infers Routes Automatically

The generator will automatically:

- ✅ Detect HTTP method from `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]`
- ✅ Combine controller-level `[Route]` prefix with action-level route template
- ✅ Replace tokens like `[controller]`, `[action]`, `[area]`
- ✅ Support multiple `[Route]` attributes on controllers (generates one route per combination)
- ✅ Extract `RequiredPermission` from `[AppRouteAuto]`

**Generated manifest:**

```json
{
  "name": "Contacts",
  "routes": [
    {
      "method": "GET",
      "path": "/api/contacts",
      "requiredPermission": "Contacts.Read"
    },
    {
      "method": "GET",
      "path": "/api/contacts/{id}",
      "requiredPermission": "Contacts.Read"
    },
    {
      "method": "POST",
      "path": "/api/contacts",
      "requiredPermission": "Contacts.Write"
    },
    {
      "method": "PUT",
      "path": "/api/contacts/{id}",
      "requiredPermission": "Contacts.Write"
    },
    {
      "method": "DELETE",
      "path": "/api/contacts/{id}",
      "requiredPermission": "Contacts.Delete"
    }
  ]
}
```

### Advanced: Overrides

Use `MethodOverride` or `PathOverride` when needed:

```csharp
[HttpGet("legacy-endpoint")]
[AppRouteAuto(
    RequiredPermission = "Contacts.Read",
    MethodOverride = "POST",           // Override inferred method
    PathOverride = "/api/v1/contacts"  // Override entire path
)]
public async Task<ActionResult> LegacyEndpoint() { ... }
```

### Implicit Modules (Optional)

You can omit the anchor class—the generator will create a minimal manifest automatically:

```csharp
[ApiController]
[Route("api/reports")]
[AppBelongsTo("Reporting")]  // No anchor defined
public class ReportsController : ControllerBase
{
    [HttpGet("sales")]
    [AppRouteAuto(RequiredPermission = "Reports.View")]
    public async Task<ActionResult> SalesReport() { ... }
}
```

This generates a manifest for "Reporting" with default metadata.

### Generated Output

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
    ""Contact.Created"",
    ""Contact.Updated"",
    ""Contact.Deleted""
  ],
  ""eventsSubscribed"": [
    ""Company.Created""
  ],
  ""dependencies"": [
    {""app"": ""Tenancy"", ""versionRange"": "">=1.0 <2.0""},
    {""app"": ""UserManagement"", ""versionRange"": ""^1.0""}
  ]
}";
    }
}
```

### Runtime Consumption

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

✅ **Auto-inferred routes** – reads your existing ASP.NET Core MVC attributes  
✅ **Incremental compilation** – only regenerates when attributes change  
✅ **Multi-module support** – handles multiple `[AppModule]` anchors  
✅ **Multiple controllers per module** – use `[AppBelongsTo]` on any number of controllers  
✅ **Token replacement** – supports `[controller]`, `[action]`, `[area]`  
✅ **Deterministic output** – stable JSON property ordering  
✅ **Graceful defaults** – missing optional values get sensible fallbacks  
✅ **Name sanitization** – handles special characters in module names  
✅ **Zero runtime dependencies** – pure compile-time generator

## Supported Attributes

| Attribute               | Target         | Purpose                                                    |
| ----------------------- | -------------- | ---------------------------------------------------------- |
| `[AppModule]`           | Class          | Module metadata (name, version, description)               |
| `[AppBelongsTo]`        | Class          | Associates a controller with a module                      |
| `[AppRouteAuto]`        | Method         | Marks an action method for route inference with permission |
| `[AppPermissions]`      | Class          | List of canonical permissions for the module               |
| `[AppSetting]`          | Field/Property | Configuration setting                                      |
| `[AppPublishesEvents]`  | Class          | Events this module publishes                               |
| `[AppSubscribesEvents]` | Class          | Events this module consumes                                |
| `[AppDependsOn]`        | Class          | Declares a dependency on another module with version range |

### Route Inference

When using `[AppRouteAuto]`, the generator automatically infers routes from ASP.NET Core MVC attributes:

| MVC Attribute                 | Generator Behavior                                           |
| ----------------------------- | ------------------------------------------------------------ |
| `[HttpGet]`                   | Infers HTTP method = GET                                     |
| `[HttpPost]`                  | Infers HTTP method = POST                                    |
| `[HttpPut]`                   | Infers HTTP method = PUT                                     |
| `[HttpDelete]`                | Infers HTTP method = DELETE                                  |
| `[HttpPatch]`                 | Infers HTTP method = PATCH                                   |
| `[Route("...")]` (controller) | Used as route prefix, combined with action template          |
| `[Route("...")]` (method)     | Used as action template (if no Http\* attribute)             |
| `[Area("...")]`               | Replaces `[area]` token in route templates                   |
| Multiple `[Route]`            | Generates one manifest entry per prefix × action combination |

**Tokens replaced:**

- `[controller]` → Controller name (minus "Controller" suffix)
- `[action]` → Method name
- `[area]` → Area name from `[Area]` attribute

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
  "dependencies": [
    {
      "app": "string",
      "versionRange": "string"
    }
  ]
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
