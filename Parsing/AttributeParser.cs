#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Vantum.AppKit.Generators.Models;

namespace Vantum.AppKit.Generators.Parsing
{
    /// <summary>
    /// Parses AppKit attributes from type symbols and builds manifest DTOs.
    /// </summary>
    internal static class AttributeParser
    {
        private const string AppModuleAttribute = "Vantum.AppKit.AppModuleAttribute";
        private const string AppPermissionsAttribute = "Vantum.AppKit.AppPermissionsAttribute";
        private const string AppRouteAttribute = "Vantum.AppKit.AppRouteAttribute";
        private const string AppSettingAttribute = "Vantum.AppKit.AppSettingAttribute";
        private const string AppPublishesEventsAttribute = "Vantum.AppKit.AppPublishesEventsAttribute";
        private const string AppSubscribesEventsAttribute = "Vantum.AppKit.AppSubscribesEventsAttribute";
        private const string AppDependsOnAttribute = "Vantum.AppKit.AppDependsOnAttribute";

        public static ManifestDto? ParseManifest(INamedTypeSymbol typeSymbol)
        {
            var appModuleAttr = typeSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == AppModuleAttribute);

            if (appModuleAttr == null)
                return null;

            var manifest = new ManifestDto();

            // Parse AppModule attribute
            ParseAppModuleAttribute(appModuleAttr, manifest);

            // Parse permissions
            ParsePermissions(typeSymbol, manifest);

            // Parse routes from methods
            ParseRoutes(typeSymbol, manifest);

            // Parse settings from fields and properties
            ParseSettings(typeSymbol, manifest);

            // Parse events
            ParseEvents(typeSymbol, manifest);

            // Parse dependencies
            ParseDependencies(typeSymbol, manifest);

            // Apply defaults
            if (string.IsNullOrWhiteSpace(manifest.DisplayName))
                manifest.DisplayName = manifest.Name;

            if (string.IsNullOrWhiteSpace(manifest.Version))
                manifest.Version = "0.1.0";

            return manifest;
        }

        private static void ParseAppModuleAttribute(AttributeData attr, ManifestDto manifest)
        {
            // Named arguments
            foreach (var namedArg in attr.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "Name":
                        manifest.Name = namedArg.Value.Value?.ToString() ?? string.Empty;
                        break;
                    case "DisplayName":
                        manifest.DisplayName = namedArg.Value.Value?.ToString() ?? string.Empty;
                        break;
                    case "Version":
                        manifest.Version = namedArg.Value.Value?.ToString() ?? "0.1.0";
                        break;
                    case "Description":
                        manifest.Description = namedArg.Value.Value?.ToString() ?? string.Empty;
                        break;
                }
            }

            // Constructor arguments (if any)
            if (attr.ConstructorArguments.Length > 0)
            {
                manifest.Name = attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
            }
        }

        private static void ParsePermissions(INamedTypeSymbol typeSymbol, ManifestDto manifest)
        {
            var permissionAttrs = typeSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == AppPermissionsAttribute);

            foreach (var attr in permissionAttrs)
            {
                // AppPermissions takes params string[] or an array
                if (attr.ConstructorArguments.Length > 0)
                {
                    var arg = attr.ConstructorArguments[0];
                    if (arg.Kind == TypedConstantKind.Array)
                    {
                        foreach (var value in arg.Values)
                        {
                            var permission = value.Value?.ToString();
                            if (!string.IsNullOrWhiteSpace(permission))
                                manifest.Permissions.Add(permission);
                        }
                    }
                    else
                    {
                        var permission = arg.Value?.ToString();
                        if (!string.IsNullOrWhiteSpace(permission))
                            manifest.Permissions.Add(permission);
                    }
                }
            }
        }

        private static void ParseRoutes(INamedTypeSymbol typeSymbol, ManifestDto manifest)
        {
            var methods = typeSymbol.GetMembers().OfType<IMethodSymbol>();

            foreach (var method in methods)
            {
                var routeAttrs = method.GetAttributes()
                    .Where(a => a.AttributeClass?.ToDisplayString() == AppRouteAttribute);

                foreach (var attr in routeAttrs)
                {
                    var route = new RouteDto();

                    // Constructor args: method, path
                    if (attr.ConstructorArguments.Length >= 2)
                    {
                        route.Method = attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                        route.Path = attr.ConstructorArguments[1].Value?.ToString() ?? string.Empty;
                    }

                    // Named arg: RequiredPermission
                    foreach (var namedArg in attr.NamedArguments)
                    {
                        if (namedArg.Key == "RequiredPermission")
                        {
                            route.RequiredPermission = namedArg.Value.Value?.ToString();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(route.Method) && !string.IsNullOrWhiteSpace(route.Path))
                        manifest.Routes.Add(route);
                }
            }
        }

        private static void ParseSettings(INamedTypeSymbol typeSymbol, ManifestDto manifest)
        {
            var members = typeSymbol.GetMembers()
                .Where(m => m.Kind == SymbolKind.Field || m.Kind == SymbolKind.Property);

            foreach (var member in members)
            {
                var settingAttrs = member.GetAttributes()
                    .Where(a => a.AttributeClass?.ToDisplayString() == AppSettingAttribute);

                foreach (var attr in settingAttrs)
                {
                    var setting = new SettingDto();

                    // Constructor args: key, type
                    if (attr.ConstructorArguments.Length >= 2)
                    {
                        setting.Key = attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;

                        // Handle enum type - get the name instead of numeric value
                        var typeArg = attr.ConstructorArguments[1];
                        if (typeArg.Type?.TypeKind == TypeKind.Enum && typeArg.Value != null)
                        {
                            // Get the enum field name
                            var enumType = typeArg.Type as INamedTypeSymbol;
                            if (enumType != null)
                            {
                                var enumValue = Convert.ToInt32(typeArg.Value);
                                var enumMember = enumType.GetMembers()
                                    .OfType<IFieldSymbol>()
                                    .FirstOrDefault(f => f.IsConst && f.ConstantValue != null && Convert.ToInt32(f.ConstantValue) == enumValue);
                                setting.Type = enumMember?.Name ?? typeArg.Value.ToString() ?? string.Empty;
                            }
                            else
                            {
                                setting.Type = typeArg.Value.ToString() ?? string.Empty;
                            }
                        }
                        else
                        {
                            setting.Type = typeArg.Value?.ToString() ?? string.Empty;
                        }
                    }

                    // Named args
                    foreach (var namedArg in attr.NamedArguments)
                    {
                        switch (namedArg.Key)
                        {
                            case "DefaultValue":
                                setting.DefaultValue = namedArg.Value.Value?.ToString();
                                break;
                            case "Description":
                                setting.Description = namedArg.Value.Value?.ToString();
                                break;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(setting.Key))
                        manifest.Settings.Add(setting);
                }
            }
        }

        private static void ParseEvents(INamedTypeSymbol typeSymbol, ManifestDto manifest)
        {
            // Published events
            var publishAttrs = typeSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == AppPublishesEventsAttribute);

            foreach (var attr in publishAttrs)
            {
                if (attr.ConstructorArguments.Length > 0)
                {
                    var arg = attr.ConstructorArguments[0];
                    if (arg.Kind == TypedConstantKind.Array)
                    {
                        foreach (var value in arg.Values)
                        {
                            var eventName = value.Value?.ToString();
                            if (!string.IsNullOrWhiteSpace(eventName))
                                manifest.EventsPublished.Add(eventName);
                        }
                    }
                }
            }

            // Subscribed events
            var subscribeAttrs = typeSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == AppSubscribesEventsAttribute);

            foreach (var attr in subscribeAttrs)
            {
                if (attr.ConstructorArguments.Length > 0)
                {
                    var arg = attr.ConstructorArguments[0];
                    if (arg.Kind == TypedConstantKind.Array)
                    {
                        foreach (var value in arg.Values)
                        {
                            var eventName = value.Value?.ToString();
                            if (!string.IsNullOrWhiteSpace(eventName))
                                manifest.EventsSubscribed.Add(eventName);
                        }
                    }
                }
            }
        }

        private static void ParseDependencies(INamedTypeSymbol typeSymbol, ManifestDto manifest)
        {
            var dependencyAttrs = typeSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.ToDisplayString() == AppDependsOnAttribute);

            foreach (var attr in dependencyAttrs)
            {
                // AppDependsOn takes two constructor arguments: appName and versionRange
                if (attr.ConstructorArguments.Length >= 2)
                {
                    var appName = attr.ConstructorArguments[0].Value?.ToString();
                    var versionRange = attr.ConstructorArguments[1].Value?.ToString();

                    if (!string.IsNullOrWhiteSpace(appName) && !string.IsNullOrWhiteSpace(versionRange))
                    {
                        manifest.Dependencies.Add(new DependencyDto
                        {
                            App = appName,
                            VersionRange = versionRange
                        });
                    }
                }
            }
        }
    }
}
