![URegistry Banner](https://github.com/Margoovian/URegistry/blob/main/res/URegistryBanner.png)

# URegistry - A ReflectionDriven Plugin Registry for .NET

> **Goal:** Provide a simple, stronglytyped way to build plugin systems via reflection, with attributebased discovery, a lightweight dependency graph, and clean lifecycle hooks.

## TL;DR

- Drop your plugin DLLs into a folder.
- Each plugin class is annotated with `[PluginIdentity]` and optional `[PluginRequires]`.
- The host app uses `PluginRegistry<T>` to load assemblies, resolve dependencies, mount, initialize, and verify plugins.
- Plugins log through the shared registry logger and can communicate with the host via `CallEventWrapper<T>`.
- Assemblies are loaded in collectible `AssemblyLoadContext`s for clean unloads.

---

## Why URegistry?

- **Attributebased discovery.** Mark your plugin class with `[PluginIdentity]` to make it discoverable. The unique plugin ID comes from `IdPath.Name` lowercased with spaces converted to underscores (e.g., `The Path` + `My Plugin` -> `the_path.my_plugin`). 
- **Soft dependency model.** Express dependencies with `[PluginRequires("other_plugin_id")]`. The registry builds a dependency graph and ensures requirements are **loaded/mounted** before your plugin runs. 
- **Simple lifecycle.** The registry calls `OnMount()` -> `OnInitialized()` -> `Verify()`. If verification fails, the plugin is flagged; otherwise it's marked ready. 
- **Centralized logging.** Initialize once, log everywhere-plugins can log via `BasePlugin.Log(...)`; the registry uses the same logger under the hood. 
- **Collectible loading & clean unloads.** Plugin assemblies are loaded into collectible `AssemblyLoadContext`s and disposed with the registry. 

---

## Key Concepts

### Plugin Identity

Annotate your concrete plugin class with `[PluginIdentity]`:

```csharp
[PluginIdentity(
 PluginType = typeof(MyPlugin),
 Name = "My_Plugin",
 IdPath = "My_Product",
 Authors = new[] { "Your Name" },
 MajorVersion = 1, MinorVersion = 0, PatchVersion = 0
)]
public sealed class MyPlugin : BasePlugin, IMyPlugin { /* ... */ }
```

- The generated ID is `my_product.my_plugin` (lowercase, spaces -> `_`). Use this in dependencies. 
- Version is composed as `Major.Minor.Patch`. 

### Declaring Dependencies

Add `[PluginRequires]` with one or more plugin IDs. The dependency graph ensures the required plugins are loaded/mounted/initialized before your plugin proceeds.

```csharp
[PluginRequires(
 "my_product.some_required_plugin",
 "another_path.another_plugin"
)]
public sealed class MyPlugin : BasePlugin, IMyPlugin { /* ... */ }
```

Under the hood, the graph connects nodes by ID and checks each dependency's state (`Loaded`, `Mounted`, `Initialized`). 

### Plugin Lifecycle

Your plugin implements the `IPlugin` lifecycle used by the registry:

- `OnMount()` - created via reflection; return `true` to mount. 
- `OnInitialized()` - called after mounting; safe place to wire runtime behavior. 
- `Verify()` - registry calls this last; must return `true` for the plugin to be marked ready. 
- `OnUnmount()` / `OnDeinitialized()` - optional teardown hooks (see demo plugins). 

Use `BasePlugin.Log(...)` for namespaced logging that includes your class and calling member. 

### Host Plugin Communication

Expose host callbacks to plugins using the `CallEventWrapper<T>`:

```csharp
// In the host, after mounting:
plugin.HelloWorldHook = new CallEventWrapper<IMyPlugin> { Func = HelloWorld };

// Host method signature:
private void HelloWorld(IMyPlugin sender, EventArgs args) { /* ... */ }

// In the plugin:
HelloWorldHook.Call(this, EventArgs.Empty);
```
The wrapper is a tiny struct that invokes a hostprovided `Action<T, EventArgs>`. 

---

## Using the Registry in a Host App

### 1) Create the registry and initialize logging

```csharp
var registry = new PluginRegistry<IMyPlugin>();
registry.InitializeLogger(); // can pass extra ILoggerProvider(s) if desired
```
Logging is configured with `SimpleConsole` and a default minimum of `Debug`. 

### 2) (Optional) Subscribe to plugin events

```csharp
registry.PluginMounted += plugin =>
{
 // Provide host callbacks to the plugin here
 plugin.HelloWorldHook = new CallEventWrapper<IMyPlugin> { Func = HelloWorld };
};
```

### 3) Load your plugin folder

```csharp
// e.g., some path with *.dll plugins
registry.LoadPluginFolder(pathToPlugins);
```
The registry:
1. Loads all `*.dll` with a collectible `AssemblyLoadContext`. 
2. Builds the dependency graph from `[PluginIdentity]` and `[PluginRequires]`. 
3. Marks nodes `Loaded` when assemblies contain identifiable plugins. 
4. Mounts/initializes/verifychecks all ready plugins, then logs counts and results. 

> **Demo:** The sample program wires `PluginMounted` and calls `LoadPluginFolder` with a hardcoded path targeting `net9.0` demo builds. 

### 4) Unload (dispose) when done

```csharp
registry.Dispose();
```
This clears mounted plugins and unloads every collectible `AssemblyLoadContext`, logging successes/failures. 

---

## Authoring a Plugin

1. **Define an interface** (your plugin contract) that extends `IPlugin`. The demo uses `IDemoPlugin : IPlugin` and adds a `HelloWorldHook`. 
2. **Implement the interface** in a concrete class that also derives from `BasePlugin` for convenient logging. 
3. **Annotate the class** with `[PluginIdentity(...)]` and optional `[PluginRequires(...)]`. See the two demo plugins that depend on each other by ID. 
4. **Build to a DLL** and place it where the host will scan.

**Example (condensed)**

```csharp
[PluginIdentity(
 PluginType = typeof(DemoPluginImplementation),
 Name = "Demo_plugin",
 IdPath = "The_Ultimate_Demo_Plugin")]
[PluginRequires("the_ultimate_demo_plugin.demo_plugin_req")]
public sealed class DemoPluginImplementation : BasePlugin, IDemoPlugin
{
 public CallEventWrapper<IDemoPlugin> HelloWorldHook { get; set; }

 public bool OnMount() => true;
 public void OnInitialized()
 {
 Log(LogLevel.Information, "Plugin 1 Initialized");
 HelloWorldHook.Call(this, EventArgs.Empty);
 }
 public bool OnUnmount() => true;
 public void OnDeinitialized() { }
}
```

---

## Dependency Graph (What's Happening Internally)

- Nodes are the plugin IDs. 
- Edges are the IDs listed in `[PluginRequires]`. 
- A plugin is considered **ready** when all of its dependencies are in a nonerror state (`Loaded | Mounted | Initialized`). 
- The registry iterates nodes to mount/initialize those not in `Unknown/Unloaded/Shutdown/Error`. 

States tracked: `Unknown, Loaded, Mounted, Initialized, Shutdown, Unloaded, Error`. 

---

## Logging From Plugins

Call `Log(level, message, args?)` from any `BasePlugin` method. The helper prefixes logs with your **class name** and the **calling member** for tidy tracing. 

```csharp
Log(LogLevel.Information, "Connected to host with options: {0}", new object?[] { options });
```

Initialize the shared logger once in the host via `registry.InitializeLogger(...)`. 

---

## Demo Layout (Quick Look)

- **URegistry.Core**: registry, base plugin, attributes, dependency graph, ALC wrapper, logging. 
- **DemoPluginBridge**: `IDemoPlugin` interface used by host and plugins. 
- **DemoPlugin / DemoPluginReqTest**: sample plugins showing identity, dependency, and host callback usage. 
- **URegistryDemo**: a simple console host loading `bin/Plugins/Debug/net9.0/`. 

---

## Notes & Tips

- **Dependency IDs must match** the generated `IdPath.Name` of the target plugin (lowercase, spaces -> `_`). 
- **Verification (`Verify()`) is your last chance** to confirm the plugin is in a good state; return `true` to be marked ready. 
- **PluginSettings** currently exists as a placeholder for future configuration. 

---

## Minimum Target

The demo references `net9.0` in the example path; target a recent .NET runtime consistent with your build output.

---


---

## License

MIT License

Copyright (c) 2025 Margoovian

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

