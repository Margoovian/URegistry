namespace URegistry.Core.Attributes
{

    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Struct,
        AllowMultiple = true
        )  // Multiuse attribute.
]
    public class PluginIdentityAttribute : Attribute
    {

        /// <summary>
        /// Type information about the actual plugins class. I:E the class that inherrits from the users interface
        /// </summary>
        public required Type PluginType { get; init; }

        /// <summary>
        /// Name of the plugin
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Unique path appened to plugins name to create it's unique identity
        /// </summary>
        public required string IdPath { get; init; }

        /// <summary>
        /// Name of the plugins author(s)
        /// </summary>
        public string[] Authors { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Unique Id of the plugin consisting of the plugins IdPath and Name, formatted as such: IdPath.Name
        /// </summary>
        public string Id { get => GetId(); }

        /// <summary>
        /// Major version of the plugin - default: 1
        /// </summary>
        public uint MajorVersion { get; set; } = 1;

        /// <summary>
        /// Minor version of the plugin - default: 0
        /// </summary>
        public uint MinorVersion { get; set; } = 0;

        /// <summary>
        /// Patch version of the plugin - default: 0
        /// </summary>
        public uint PatchVersion { get; set; } = 0;

        /// <summary>
        /// Combined version of the plugin, formatted as such: Major.Minor.Patch
        /// </summary>
        public string Version { get => GetVersion(); }

        /// <summary>
        /// Gets the plugins name
        /// </summary>
        /// <returns></returns>
        public string GetPluginName() => Name;

        /// <summary>
        /// Gets the plugins authors
        /// </summary>
        /// <returns></returns>
        public string[] GetAuthors() => Authors;

        /// <summary>
        /// Gets the first author of the plugin, this can be useful if only one person is working on the plugin
        /// </summary>
        /// <returns></returns>
        public string? GetAuthorAsSoleCreator() => Authors[0];

        /// <summary>
        /// Gets the major version of the plugin - default: 1
        /// </summary>
        /// <returns></returns>
        public uint GetMajorVersion() => MajorVersion;

        /// <summary>
        /// Gets the minor version of the plugin - default : 0
        /// </summary>
        /// <returns></returns>
        public uint GetMinorVersion() => MinorVersion;

        /// <summary>
        /// Gets the patch version of the plugin - default: 0
        /// </summary>
        /// <returns></returns>
        public uint GetPatchVersion() => PatchVersion;

        /// <summary>
        /// Gets the full version of the plugin
        /// </summary>
        /// <returns></returns>
        public string GetVersion()
        {
            return $"{MajorVersion}.{MinorVersion}.{PatchVersion}";
        }

        /// <summary>
        /// Gets the unique Id of the plugin
        /// </summary>
        /// <returns></returns>
        public string GetId() 
        {
            return $"{IdPath.ToLower().Replace(' ','_')}.{Name.ToLower().Replace(' ', '_')}";
        }
    }
}
