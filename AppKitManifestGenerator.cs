#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Vantum.AppKit.Generators.Models;
using Vantum.AppKit.Generators.Parsing;
using Vantum.AppKit.Generators.Utilities;

namespace Vantum.AppKit.Generators
{
    /// <summary>
    /// Incremental source generator that scans for [AppModule] classes
    /// and produces manifest constants in the Vantum.Generated namespace.
    /// </summary>
    [Generator]
    public class AppKitManifestGenerator : IIncrementalGenerator
    {
        private const string AppModuleAttribute = "Vantum.AppKit.AppModuleAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. Filter syntax nodes: only classes with attributes
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsCandidateClass(s),
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null);

            // 2. Combine with compilation
            var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

            // 3. Generate source for each module
            context.RegisterSourceOutput(compilationAndClasses,
                static (spc, source) => Execute(source.Left, source.Right!, spc));
        }

        private static bool IsCandidateClass(SyntaxNode node)
        {
            return node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0;
        }

        private static INamedTypeSymbol? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

            if (symbol == null)
                return null;

            // Check if class has [AppModule] attribute
            var hasAppModule = symbol.GetAttributes()
                .Any(a => a.AttributeClass?.ToDisplayString() == AppModuleAttribute);

            return hasAppModule ? symbol : null;
        }

        private static void Execute(Compilation compilation, ImmutableArray<INamedTypeSymbol?> classes, SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
                return;

            // STEP 1: Parse manifests from anchors with [AppModule]
            var manifestsByName = new Dictionary<string, ManifestDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var classSymbol in classes)
            {
                if (classSymbol == null)
                    continue;

                // Parse the manifest from the anchor class
                var manifest = AttributeParser.ParseManifest(classSymbol);
                if (manifest == null || string.IsNullOrWhiteSpace(manifest.Name))
                    continue;

                manifestsByName[manifest.Name] = manifest;
            }

            // STEP 2: Infer routes from controllers with [AppBelongsTo]
            var inferredRoutes = RouteInference.InferRoutesFromControllers(compilation);

            // STEP 3: Merge inferred routes into manifests
            foreach (var kvp in inferredRoutes)
            {
                var moduleName = kvp.Key;
                var routes = kvp.Value;

                if (manifestsByName.TryGetValue(moduleName, out var manifest))
                {
                    // Add inferred routes to the manifest
                    manifest.Routes.AddRange(routes);
                }
                else
                {
                    // No anchor found for this module - create a minimal manifest
                    // (This allows controllers to define modules implicitly)
                    manifest = new ManifestDto
                    {
                        Name = moduleName,
                        DisplayName = moduleName,
                        Version = "0.1.0",
                        Description = $"Auto-generated manifest for {moduleName}",
                        Routes = routes
                    };
                    manifestsByName[moduleName] = manifest;
                }
            }

            // STEP 4: Generate source for each manifest
            foreach (var manifest in manifestsByName.Values)
            {
                // Serialize to JSON
                var json = JsonSerializer.Serialize(manifest);

                // Sanitize module name for class identifier
                var safeName = NameSanitizer.SanitizeIdentifier(manifest.Name);

                // Generate source code
                var source = GenerateManifestClass(safeName, json);

                // Add to compilation
                context.AddSource($"Manifest_{safeName}.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        }

        private static string GenerateManifestClass(string safeName, string json)
        {
            // Escape for C# verbatim string literal (@"...")
            var escapedJson = json.Replace("\"", "\"\"");

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("namespace Vantum.Generated");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Manifest for the '{safeName}' app module.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public static class Manifest_{safeName}");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// The JSON manifest describing this module's metadata.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public const string Json = @\"{escapedJson}\";");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
