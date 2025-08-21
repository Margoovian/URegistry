using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace URegistry.Core.Attributes
{
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Struct,
        AllowMultiple = false
        )  // Multiuse attribute.
]
    public class PluginRequiresAttribute : Attribute
    {
        /// <summary>
        /// The Dependencies that a plugin needs
        /// </summary>
        public string[] Dependencies { get; private set; }
        public PluginRequiresAttribute(params string[] dependencies)
        {
            Dependencies = dependencies;
        }
    }
}

