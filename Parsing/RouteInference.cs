#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Vantum.AppKit.Generators.Models;

namespace Vantum.AppKit.Generators.Parsing
{
    /// <summary>
    /// Infers HTTP routes from ASP.NET Core MVC attributes on controllers and actions.
    /// </summary>
    internal static class RouteInference
    {
        private const string AppBelongsToAttribute = "Vantum.AppKit.AppBelongsToAttribute";
        private const string AppRouteAutoAttribute = "Vantum.AppKit.AppRouteAutoAttribute";
        private const string RouteAttribute = "Microsoft.AspNetCore.Mvc.RouteAttribute";
        private const string AreaAttribute = "Microsoft.AspNetCore.Mvc.AreaAttribute";
        private const string HttpGetAttribute = "Microsoft.AspNetCore.Mvc.HttpGetAttribute";
        private const string HttpPostAttribute = "Microsoft.AspNetCore.Mvc.HttpPostAttribute";
        private const string HttpPutAttribute = "Microsoft.AspNetCore.Mvc.HttpPutAttribute";
        private const string HttpDeleteAttribute = "Microsoft.AspNetCore.Mvc.HttpDeleteAttribute";
        private const string HttpPatchAttribute = "Microsoft.AspNetCore.Mvc.HttpPatchAttribute";

        /// <summary>
        /// Scans all types in the compilation for controllers with [AppBelongsTo] 
        /// and groups their auto-inferred routes by module name.
        /// </summary>
        public static Dictionary<string, List<RouteDto>> InferRoutesFromControllers(Compilation compilation)
        {
            var routesByModule = new Dictionary<string, List<RouteDto>>(StringComparer.OrdinalIgnoreCase);

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = syntaxTree.GetRoot();

                foreach (var typeDecl in root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>())
                {
                    var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
                    if (typeSymbol == null)
                        continue;

                    // Check for [AppBelongsTo]
                    var belongsToAttr = typeSymbol.GetAttributes()
                        .FirstOrDefault(a => IsAttributeType(a.AttributeClass, AppBelongsToAttribute));

                    if (belongsToAttr == null)
                        continue;

                    // Extract module name
                    string? moduleName = null;
                    if (belongsToAttr.ConstructorArguments.Length > 0)
                        moduleName = belongsToAttr.ConstructorArguments[0].Value?.ToString();

                    if (string.IsNullOrWhiteSpace(moduleName))
                        continue;

                    // Parse routes from this controller
                    var routes = InferRoutesFromController(typeSymbol);
                    if (routes.Count > 0)
                    {
                        if (!routesByModule.ContainsKey(moduleName))
                            routesByModule[moduleName] = new List<RouteDto>();

                        routesByModule[moduleName].AddRange(routes);
                    }
                }
            }

            return routesByModule;
        }

        /// <summary>
        /// Infers routes from a single controller type.
        /// </summary>
        private static List<RouteDto> InferRoutesFromController(INamedTypeSymbol controllerType)
        {
            var routes = new List<RouteDto>();

            // Get controller-level route prefixes
            var controllerPrefixes = GetControllerRoutePrefixes(controllerType);
            if (controllerPrefixes.Count == 0)
                controllerPrefixes.Add(""); // Default empty prefix

            // Get area (if any)
            var area = GetAreaFromController(controllerType);

            // Get controller name (strip "Controller" suffix if present)
            var controllerName = controllerType.Name;
            if (controllerName.EndsWith("Controller", StringComparison.Ordinal))
                controllerName = controllerName.Substring(0, controllerName.Length - "Controller".Length);

            // Scan action methods
            var methods = controllerType.GetMembers().OfType<IMethodSymbol>();
            foreach (var method in methods)
            {
                var routeAutoAttr = method.GetAttributes()
                    .FirstOrDefault(a => IsAttributeType(a.AttributeClass, AppRouteAutoAttribute));

                if (routeAutoAttr == null)
                    continue;

                // Parse [AppRouteAuto] parameters
                string? requiredPermission = null;
                string? methodOverride = null;
                string? pathOverride = null;

                foreach (var namedArg in routeAutoAttr.NamedArguments)
                {
                    switch (namedArg.Key)
                    {
                        case "RequiredPermission":
                            requiredPermission = namedArg.Value.Value?.ToString();
                            break;
                        case "MethodOverride":
                            methodOverride = namedArg.Value.Value?.ToString();
                            break;
                        case "PathOverride":
                            pathOverride = namedArg.Value.Value?.ToString();
                            break;
                    }
                }

                // If PathOverride is set, use it directly
                if (!string.IsNullOrWhiteSpace(pathOverride))
                {
                    var httpMethod = methodOverride ?? InferHttpMethodFromAttributes(method) ?? "GET";
                    routes.Add(new RouteDto
                    {
                        Method = httpMethod.ToUpperInvariant(),
                        Path = NormalizePath(pathOverride),
                        RequiredPermission = requiredPermission
                    });
                    continue;
                }

                // Otherwise, infer from MVC attributes
                var inferredRoutes = InferRoutesFromAction(method, controllerPrefixes, controllerName, area);
                foreach (var route in inferredRoutes)
                {
                    // Apply methodOverride if specified
                    if (!string.IsNullOrWhiteSpace(methodOverride))
                        route.Method = methodOverride.ToUpperInvariant();

                    route.RequiredPermission = requiredPermission;
                    routes.Add(route);
                }
            }

            return routes;
        }

        /// <summary>
        /// Gets controller-level route prefixes from [Route] attributes.
        /// </summary>
        private static List<string> GetControllerRoutePrefixes(INamedTypeSymbol controllerType)
        {
            var prefixes = new List<string>();

            var routeAttrs = controllerType.GetAttributes()
                .Where(a => IsAttributeType(a.AttributeClass, RouteAttribute));

            foreach (var attr in routeAttrs)
            {
                if (attr.ConstructorArguments.Length > 0)
                {
                    var template = attr.ConstructorArguments[0].Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(template))
                        prefixes.Add(template);
                }
            }

            return prefixes;
        }

        /// <summary>
        /// Gets the area name from [Area] attribute.
        /// </summary>
        private static string? GetAreaFromController(INamedTypeSymbol controllerType)
        {
            var areaAttr = controllerType.GetAttributes()
                .FirstOrDefault(a => IsAttributeType(a.AttributeClass, AreaAttribute));

            if (areaAttr?.ConstructorArguments.Length > 0)
                return areaAttr.ConstructorArguments[0].Value?.ToString();

            return null;
        }

        /// <summary>
        /// Infers routes from an action method by examining Http* and Route attributes.
        /// </summary>
        private static List<RouteDto> InferRoutesFromAction(
            IMethodSymbol method,
            List<string> controllerPrefixes,
            string controllerName,
            string? area)
        {
            var routes = new List<RouteDto>();

            // Check for Http* attributes (HttpGet, HttpPost, etc.)
            var httpAttrs = method.GetAttributes()
                .Where(a => IsHttpMethodAttribute(a.AttributeClass))
                .ToList();

            if (httpAttrs.Count > 0)
            {
                // Each Http* attribute can specify a template
                foreach (var httpAttr in httpAttrs)
                {
                    var httpMethod = GetHttpMethodFromAttribute(httpAttr.AttributeClass!);
                    var actionTemplate = "";

                    if (httpAttr.ConstructorArguments.Length > 0)
                        actionTemplate = httpAttr.ConstructorArguments[0].Value?.ToString() ?? "";

                    // Combine with each controller prefix
                    foreach (var prefix in controllerPrefixes)
                    {
                        var fullPath = CombinePaths(prefix, actionTemplate);
                        fullPath = ReplaceTokens(fullPath, controllerName, method.Name, area);
                        fullPath = NormalizePath(fullPath);

                        routes.Add(new RouteDto
                        {
                            Method = httpMethod.ToUpperInvariant(),
                            Path = fullPath
                        });
                    }
                }
            }
            else
            {
                // Check for [Route] attribute without Http* attribute
                var routeAttrs = method.GetAttributes()
                    .Where(a => IsAttributeType(a.AttributeClass, RouteAttribute));

                foreach (var routeAttr in routeAttrs)
                {
                    var actionTemplate = "";
                    if (routeAttr.ConstructorArguments.Length > 0)
                        actionTemplate = routeAttr.ConstructorArguments[0].Value?.ToString() ?? "";

                    // Default to GET
                    foreach (var prefix in controllerPrefixes)
                    {
                        var fullPath = CombinePaths(prefix, actionTemplate);
                        fullPath = ReplaceTokens(fullPath, controllerName, method.Name, area);
                        fullPath = NormalizePath(fullPath);

                        routes.Add(new RouteDto
                        {
                            Method = "GET",
                            Path = fullPath
                        });
                    }
                }
            }

            return routes;
        }

        /// <summary>
        /// Infers HTTP method from method attributes (for MethodOverride fallback).
        /// </summary>
        private static string? InferHttpMethodFromAttributes(IMethodSymbol method)
        {
            var httpAttr = method.GetAttributes()
                .FirstOrDefault(a => IsHttpMethodAttribute(a.AttributeClass));

            if (httpAttr?.AttributeClass != null)
                return GetHttpMethodFromAttribute(httpAttr.AttributeClass);

            return null;
        }

        /// <summary>
        /// Checks if an attribute is an HTTP method attribute.
        /// </summary>
        private static bool IsHttpMethodAttribute(INamedTypeSymbol? attrClass)
        {
            if (attrClass == null) return false;

            var fullName = attrClass.ToDisplayString();
            return fullName == HttpGetAttribute
                || fullName == HttpPostAttribute
                || fullName == HttpPutAttribute
                || fullName == HttpDeleteAttribute
                || fullName == HttpPatchAttribute;
        }

        /// <summary>
        /// Gets the HTTP method name from an Http* attribute.
        /// </summary>
        private static string GetHttpMethodFromAttribute(INamedTypeSymbol attrClass)
        {
            var fullName = attrClass.ToDisplayString();
            if (fullName == HttpGetAttribute) return "GET";
            if (fullName == HttpPostAttribute) return "POST";
            if (fullName == HttpPutAttribute) return "PUT";
            if (fullName == HttpDeleteAttribute) return "DELETE";
            if (fullName == HttpPatchAttribute) return "PATCH";
            return "GET";
        }

        /// <summary>
        /// Combines controller prefix and action template into a single path.
        /// </summary>
        private static string CombinePaths(string prefix, string action)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return action ?? "";

            if (string.IsNullOrWhiteSpace(action))
                return prefix;

            // Handle absolute action template (starts with /)
            if (action.StartsWith("/", StringComparison.Ordinal))
                return action;

            // Combine with /
            var combined = prefix.TrimEnd('/') + "/" + action.TrimStart('/');
            return combined;
        }

        /// <summary>
        /// Replaces tokens like [controller], [action], [area] in a route template.
        /// </summary>
        private static string ReplaceTokens(string template, string controllerName, string actionName, string? area)
        {
            var result = template;

            result = result.Replace("[controller]", controllerName, StringComparison.OrdinalIgnoreCase);
            result = result.Replace("[action]", actionName, StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(area))
                result = result.Replace("[area]", area, StringComparison.OrdinalIgnoreCase);

            return result;
        }

        /// <summary>
        /// Normalizes a path: ensures leading /, removes double slashes, removes trailing / unless root.
        /// </summary>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "/";

            // Ensure leading /
            if (!path.StartsWith("/", StringComparison.Ordinal))
                path = "/" + path;

            // Collapse double slashes
            while (path.Contains("//"))
                path = path.Replace("//", "/");

            // Remove trailing / unless it's the root
            if (path.Length > 1 && path.EndsWith("/", StringComparison.Ordinal))
                path = path.Substring(0, path.Length - 1);

            return path;
        }

        /// <summary>
        /// Checks if an attribute type matches a given full name.
        /// </summary>
        private static bool IsAttributeType(INamedTypeSymbol? attrClass, string fullName)
        {
            return attrClass?.ToDisplayString() == fullName;
        }
    }
}
