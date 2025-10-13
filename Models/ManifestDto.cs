#nullable enable

using System.Collections.Generic;

namespace Vantum.AppKit.Generators.Models
{
    /// <summary>
    /// Internal DTO representing an app module manifest.
    /// </summary>
    internal sealed class ManifestDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Version { get; set; } = "0.1.0";
        public string Description { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new List<string>();
        public List<RouteDto> Routes { get; set; } = new List<RouteDto>();
        public List<SettingDto> Settings { get; set; } = new List<SettingDto>();
        public List<string> EventsPublished { get; set; } = new List<string>();
        public List<string> EventsSubscribed { get; set; } = new List<string>();
        public List<string> Dependencies { get; set; } = new List<string>();
    }

    internal sealed class RouteDto
    {
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? RequiredPermission { get; set; }
    }

    internal sealed class SettingDto
    {
        public string Key { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? DefaultValue { get; set; }
        public string? Description { get; set; }
    }
}
