using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vantum.AppKit.Generators.Models;

namespace Vantum.AppKit.Generators.Utilities
{
    /// <summary>
    /// Simple JSON serializer for manifest DTOs.
    /// Produces stable, deterministic, indented JSON.
    /// </summary>
    internal static class JsonSerializer
    {
        public static string Serialize(ManifestDto manifest)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");

            AppendStringProperty(sb, "name", manifest.Name, false);
            AppendStringProperty(sb, "displayName", manifest.DisplayName, false);
            AppendStringProperty(sb, "version", manifest.Version, false);
            AppendStringProperty(sb, "description", manifest.Description, false);
            AppendStringArray(sb, "permissions", manifest.Permissions, false);
            AppendRoutes(sb, manifest.Routes, false);
            AppendSettings(sb, manifest.Settings, false);
            AppendStringArray(sb, "eventsPublished", manifest.EventsPublished, false);
            AppendStringArray(sb, "eventsSubscribed", manifest.EventsSubscribed, false);
            AppendDependencies(sb, manifest.Dependencies, true);

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void AppendStringProperty(StringBuilder sb, string name, string value, bool isLast)
        {
            sb.Append("  \"");
            sb.Append(name);
            sb.Append("\": \"");
            sb.Append(Escape(value));
            sb.Append("\"");
            if (!isLast) sb.Append(",");
            sb.AppendLine();
        }

        private static void AppendStringArray(StringBuilder sb, string name, List<string> values, bool isLast)
        {
            sb.Append("  \"");
            sb.Append(name);
            sb.Append("\": [");

            if (values.Count == 0)
            {
                sb.Append("]");
            }
            else
            {
                sb.AppendLine();
                for (int i = 0; i < values.Count; i++)
                {
                    sb.Append("    \"");
                    sb.Append(Escape(values[i]));
                    sb.Append("\"");
                    if (i < values.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.Append("  ]");
            }

            if (!isLast) sb.Append(",");
            sb.AppendLine();
        }

        private static void AppendRoutes(StringBuilder sb, List<RouteDto> routes, bool isLast)
        {
            sb.Append("  \"routes\": [");

            if (routes.Count == 0)
            {
                sb.Append("]");
            }
            else
            {
                sb.AppendLine();
                for (int i = 0; i < routes.Count; i++)
                {
                    var route = routes[i];
                    sb.Append("    {");
                    sb.Append("\"method\": \"");
                    sb.Append(Escape(route.Method));
                    sb.Append("\", \"path\": \"");
                    sb.Append(Escape(route.Path));
                    sb.Append("\"");

                    if (!string.IsNullOrEmpty(route.RequiredPermission))
                    {
                        sb.Append(", \"requiredPermission\": \"");
                        sb.Append(Escape(route.RequiredPermission));
                        sb.Append("\"");
                    }

                    sb.Append("}");
                    if (i < routes.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.Append("  ]");
            }

            if (!isLast) sb.Append(",");
            sb.AppendLine();
        }

        private static void AppendSettings(StringBuilder sb, List<SettingDto> settings, bool isLast)
        {
            sb.Append("  \"settings\": [");

            if (settings.Count == 0)
            {
                sb.Append("]");
            }
            else
            {
                sb.AppendLine();
                for (int i = 0; i < settings.Count; i++)
                {
                    var setting = settings[i];
                    sb.Append("    {");
                    sb.Append("\"key\": \"");
                    sb.Append(Escape(setting.Key));
                    sb.Append("\", \"type\": \"");
                    sb.Append(Escape(setting.Type));
                    sb.Append("\"");

                    if (!string.IsNullOrEmpty(setting.DefaultValue))
                    {
                        sb.Append(", \"defaultValue\": \"");
                        sb.Append(Escape(setting.DefaultValue));
                        sb.Append("\"");
                    }

                    if (!string.IsNullOrEmpty(setting.Description))
                    {
                        sb.Append(", \"description\": \"");
                        sb.Append(Escape(setting.Description));
                        sb.Append("\"");
                    }

                    sb.Append("}");
                    if (i < settings.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.Append("  ]");
            }

            if (!isLast) sb.Append(",");
            sb.AppendLine();
        }

        private static void AppendDependencies(StringBuilder sb, List<DependencyDto> dependencies, bool isLast)
        {
            sb.Append("  \"dependencies\": [");

            if (dependencies.Count == 0)
            {
                sb.Append("]");
            }
            else
            {
                sb.AppendLine();
                for (int i = 0; i < dependencies.Count; i++)
                {
                    var dep = dependencies[i];
                    sb.Append("    {");
                    sb.Append("\"app\": \"");
                    sb.Append(Escape(dep.App));
                    sb.Append("\", \"versionRange\": \"");
                    sb.Append(Escape(dep.VersionRange));
                    sb.Append("\"}");
                    if (i < dependencies.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.Append("  ]");
            }

            if (!isLast) sb.Append(",");
            sb.AppendLine();
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}
