using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using URegistry.Core.Attributes;

namespace URegistry.Core
{
    public enum PluginState
    {
        Unknown = 0, // When the plugin state is unknown
        Loaded, // When the assembly is loaded
        Mounted, // When the plugin is mounted and ready to be used
        Initialized, // When the plugin is initialized and ready to be used
        Shutdown, // When the plugin is shutdown and ready to be unloaded
        Unloaded, // When the assembly is unloaded
        Error // When the plugin is in an error state
    }
    internal class DependencyNode
    {
        public string Name { get; set; } = "nameless_node";
        public PluginState State { get; set; } = PluginState.Unknown;
        public List<WeakReference<DependencyNode>>? Dependencies { get; private set; }

        public void AddDependency(DependencyNode dependency)
        {

            Dependencies ??= new List<WeakReference<DependencyNode>>(0);
            
            Dependencies.Add(new WeakReference<DependencyNode>(dependency, trackResurrection: true));
        }

    }
    internal class DependencyGraph : IDisposable, IEnumerable<DependencyNode>
    {
        private List<DependencyNode> Nodes { get; set; } = new List<DependencyNode>(0);

        private void AddNode(string name)
        {
            BaseRegistry.Log(LogLevel.Debug, "Adding node '{0}' to the dependency graph.", [name]);
            if (Nodes.Any(n => n.Name == name))
            {
                BaseRegistry.Log(LogLevel.Warning, "Node with name '{0}' already exists in the graph.", [name]);
                return;
            }
            Nodes.Add(new DependencyNode { Name = name });
        }

        private bool GetNode(string name, ref DependencyNode outNode)
        {
            outNode = null!;
            int index = GetNodeIndex(name);
            if (index != -1)
            {
                outNode = Nodes[index];
                return true;
            }
            else
            {
                BaseRegistry.Log(LogLevel.Warning, "Node '{0}' does not exist in the graph.", [name]);
                return false;
            }
        }

        private int GetNodeIndex(string name)
        {
            int index = -1;
            if (NodeExists(name))
            {
                for(int i = 0; i < Nodes.Count; i++)
                {
                    if (Nodes[i].Name == name)
                    {
                        index = i;
                        break;
                    }
                }
            }
            return index;
        }

        private bool GetOrCreateNode(string name, ref DependencyNode outNode)
        {
            outNode = null!;
            int index = GetNodeIndex(name);
            if (index != -1)
            {
                outNode = Nodes[index];
                return true;
            }
            else
            {
                AddNode(name);
                outNode = Nodes.Last();
                return false;
            }
        }

        private bool NodeExists(string name)
        {
            return Nodes.Any(n => n.Name == name);
        }

        public void Add(PluginIdentityAttribute pluginIdentity, PluginRequiresAttribute? requiresAttribute = null)
        {
            DependencyNode rootNode = null!;
            GetOrCreateNode(pluginIdentity.Id, ref rootNode);

            if (requiresAttribute is null) return; //  No dependencies to add

            foreach (string required in requiresAttribute.Dependencies)
            {
                string formattedRequired = required.ToLower().Replace(' ', '_');
                DependencyNode dependencyNode = null!;
                if (GetOrCreateNode(formattedRequired, ref dependencyNode))
                {
                    BaseRegistry.Log(LogLevel.Debug, "Dependency '{0}' already exists in the graph.", [formattedRequired]);
                }
                else
                {
                    BaseRegistry.Log(LogLevel.Debug, "Created new node for dependency '{0}'.", [formattedRequired]);
                }
                rootNode.AddDependency(dependencyNode);
            }
        }

        public void Dispose()
        {
            Nodes.Clear();
        }

        public IEnumerator<DependencyNode> GetEnumerator()
        {
            foreach (DependencyNode node in Nodes)
            {
                yield return node;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void SetNodeState(PluginIdentityAttribute identity, PluginState state) 
        {
            int index = GetNodeIndex(identity.Id);
            if (index == -1)
            {
                BaseRegistry.Log(LogLevel.Warning, "Node '{0}' does not exist in the graph.", [identity.Id]);
                return; 
            }

            Span<DependencyNode> nodesSpan = CollectionsMarshal.AsSpan(Nodes);

            ref DependencyNode node = ref nodesSpan[index];

            node.State = state;
            BaseRegistry.Log(LogLevel.Debug, "Set node '{0}' state to {1}.", [identity.Id, state]);
        }

        public bool IsReady()
        {
            foreach (DependencyNode node in Nodes)
            {
                bool depsLoaded = AreAllDependenciesLoadedInternal(node);

                if(!depsLoaded)
                {
                    BaseRegistry.Log(LogLevel.Warning, "Node '{0}' is not ready, dependencies are not loaded.", [node.Name]);
                    return false; // If any node is not ready, the graph is not ready
                }

            }

            return true;

        }

        public bool AreAllDependenciesLoaded(PluginIdentityAttribute identity)
        {
            return AreAllDependenciesLoaded(identity.Id);
        }

        internal bool AreAllDependenciesLoaded(string pluginId)
        {
            DependencyNode outNode = null!;
            if (GetNode(pluginId, ref outNode))
            {  
                return AreAllDependenciesLoadedInternal(outNode);
            }
            else
            {
                BaseRegistry.Log(LogLevel.Warning, "Node '{0}' does not exist in the graph.", [pluginId]);
                return false;
            }
        }

        internal bool AreAllDependenciesLoadedInternal(DependencyNode node)
        {

            if (node.Dependencies is null || node.Dependencies.Count == 0)
            {
                BaseRegistry.Log(LogLevel.Debug, "Node '{0}' has no dependencies.", [node.Name]);
                return true; // No dependencies to check
            }

            foreach (WeakReference<DependencyNode> weakRef in node.Dependencies)
            {
                if (weakRef.TryGetTarget(out DependencyNode? dependencyNode))
                {
                    // TODO: turn this into a bitmask or something similar to avoid multiple checks
                    if (dependencyNode.State != PluginState.Loaded && dependencyNode.State != PluginState.Mounted && dependencyNode.State != PluginState.Initialized)
                    {
                        BaseRegistry.Log(LogLevel.Warning, "Dependency '{0}' is not loaded or mounted.", [dependencyNode.Name]);
                        return false;
                    }
                }
                else
                {
                    BaseRegistry.Log(LogLevel.Warning, "Dependency in graph is not available.", Array.Empty<object>());
                    return false;
                }
            }

            return true;

        }
    }
}
