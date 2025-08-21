How the core works step by step

1. Load all assemblies from specified path
2. Construct a dependency graph (State machine, Holds if an assembly is loaded/plugin mounted/initialized)
3. Before initialization check graph for non-loaded plugins
3. Mount all plugins (Stage 1 instantiation)
4. Initialize all plugins (Stage 2 instantiation, Happens after all plugins are mounted)
5. Verify all plugins (Stage 3 instantiation, Happens after all plugins are initialized, plugin checks it's self if it's setup correctly)