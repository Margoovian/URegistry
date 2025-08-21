![URegistry Banner](https://github.com/Margoovian/URegistry/tree/main/res/URegistryBanner.png?raw=true)

# URegistry — A Reflection‑Driven Plugin Registry for .NET

> **Goal:** Provide a simple, strongly‑typed way to build plugin systems via reflection, with attribute‑based discovery, a lightweight dependency graph, and clean lifecycle hooks.

## TL;DR

- Drop your plugin DLLs into a folder.
- Each plugin class is annotated with `[PluginIdentity]` and optional `[PluginRequires]`.
- The host app uses `PluginRegistry<T>` to load assemblies, resolve dependencies, mount, initialize, and verify plugins.
- Plugins log through the shared registry logger and can communicate with the host via `CallEventWrapper<T>`.
- Assemblies are loaded in collectible `AssemblyLoadContext`s for clean unloads.

---

## Why URegistry?

- **Attribute‑based discovery.** Mark your plugin class with `[PluginIdentity]` to make it discoverable. The unique plugin ID comes from `IdPath.Name` lower‑cased with spaces converted to underscores (e.g., `The Path` + `My Plugin` → `the_path.my_plugin`). fileciteturn1file30L65-L73  
- **Soft dependency model.** Express dependencies with `[PluginRequires("other_plugin_id")]`. The registry builds a dependency graph and ensures requirements are **loaded/mounted** before your plugin runs. fileciteturn1file31L14-L24 fileciteturn1file27L67-L82  
- **Simple lifecycle.** The registry calls `OnMount()` → `OnInitialized()` → `Verify()`. If verification fails, the plugin is flagged; otherwise it’s marked ready. fileciteturn1file26L43-L75 fileciteturn1file26L77-L106  
- **Centralized logging.** Initialize once, log everywhere—plugins can log via `BasePlugin.Log(...)`; the registry uses the same logger under the hood. fileciteturn1file24L7-L36 fileciteturn1file29L13-L28  
- **Collectible loading & clean unloads.** Plugin assemblies are loaded into collectible `AssemblyLoadContext`s and disposed with the registry. fileciteturn1file28L10-L24 fileciteturn1file26L183-L219

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

- The generated ID is `my_product.my_plugin` (lower‑case, spaces → `_`). Use this in dependencies. fileciteturn1file30L65-L73
- Version is composed as `Major.Minor.Patch`. fileciteturn1file30L55-L63

### Declaring Dependencies

Add `[PluginRequires]` with one or more plugin IDs. The dependency graph ensures the required plugins are loaded/mounted/initialized before your plugin proceeds.

```csharp
[PluginRequires(
    "my_product.some_required_plugin",
    "another_path.another_plugin"
)]
public sealed class MyPlugin : BasePlugin, IMyPlugin { /* ... */ }
```

Under the hood, the graph connects nodes by ID and checks each dependency’s state (`Loaded`, `Mounted`, `Initialized`). fileciteturn1file27L1-L14 fileciteturn1file27L112-L129

### Plugin Lifecycle

Your plugin implements the `IPlugin` lifecycle used by the registry:

- `OnMount()` — created via reflection; return `true` to mount. fileciteturn1file26L43-L75  
- `OnInitialized()` — called after mounting; safe place to wire runtime behavior. fileciteturn1file26L77-L92  
- `Verify()` — registry calls this last; must return `true` for the plugin to be marked ready. fileciteturn1file26L94-L106  
- `OnUnmount()` / `OnDeinitialized()` — optional teardown hooks (see demo plugins). fileciteturn1file37L31-L49

Use `BasePlugin.Log(...)` for namespaced logging that includes your class and calling member. fileciteturn1file29L13-L28

### Host ↔ Plugin Communication

Expose host callbacks to plugins using the `CallEventWrapper<T>`:

```csharp
// In the host, after mounting:
plugin.HelloWorldHook = new CallEventWrapper<IMyPlugin> { Func = HelloWorld };

// Host method signature:
private void HelloWorld(IMyPlugin sender, EventArgs args) { /* ... */ }

// In the plugin:
HelloWorldHook.Call(this, EventArgs.Empty);
```
The wrapper is a tiny struct that invokes a host‑provided `Action<T, EventArgs>`. fileciteturn1file32L6-L24

---

## Using the Registry in a Host App

### 1) Create the registry and initialize logging

```csharp
var registry = new PluginRegistry<IMyPlugin>();
registry.InitializeLogger(); // can pass extra ILoggerProvider(s) if desired
```
Logging is configured with `SimpleConsole` and a default minimum of `Debug`. fileciteturn1file24L9-L27

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
1. Loads all `*.dll` with a collectible `AssemblyLoadContext`. fileciteturn1file26L115-L140  
2. Builds the dependency graph from `[PluginIdentity]` and `[PluginRequires]`. fileciteturn1file26L142-L154  
3. Marks nodes `Loaded` when assemblies contain identifiable plugins. fileciteturn1file26L198-L211  
4. Mounts/initializes/verify‑checks all ready plugins, then logs counts and results. fileciteturn1file26L156-L191

> **Demo:** The sample program wires `PluginMounted` and calls `LoadPluginFolder` with a hard‑coded path targeting `net9.0` demo builds. fileciteturn1file33L20-L33

### 4) Unload (dispose) when done

```csharp
registry.Dispose();
```
This clears mounted plugins and unloads every collectible `AssemblyLoadContext`, logging successes/failures. fileciteturn1file26L183-L219

---

## Authoring a Plugin

1. **Define an interface** (your plugin contract) that extends `IPlugin`. The demo uses `IDemoPlugin : IPlugin` and adds a `HelloWorldHook`. fileciteturn1file36L1-L9  
2. **Implement the interface** in a concrete class that also derives from `BasePlugin` for convenient logging.  
3. **Annotate the class** with `[PluginIdentity(...)]` and optional `[PluginRequires(...)]`. See the two demo plugins that depend on each other by ID. fileciteturn1file37L5-L26 fileciteturn1file35L5-L23  
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

## Dependency Graph (What’s Happening Internally)

- Nodes are the plugin IDs.  
- Edges are the IDs listed in `[PluginRequires]`.  
- A plugin is considered **ready** when all of its dependencies are in a non‑error state (`Loaded | Mounted | Initialized`). fileciteturn1file27L112-L129  
- The registry iterates nodes to mount/initialize those not in `Unknown/Unloaded/Shutdown/Error`. fileciteturn1file26L168-L181

States tracked: `Unknown, Loaded, Mounted, Initialized, Shutdown, Unloaded, Error`. fileciteturn1file27L1-L12

---

## Logging From Plugins

Call `Log(level, message, args?)` from any `BasePlugin` method. The helper prefixes logs with your **class name** and the **calling member** for tidy tracing. fileciteturn1file29L13-L28

```csharp
Log(LogLevel.Information, "Connected to host with options: {0}", new object?[] { options });
```

Initialize the shared logger once in the host via `registry.InitializeLogger(...)`. fileciteturn1file24L9-L36

---

## Demo Layout (Quick Look)

- **URegistry.Core**: registry, base plugin, attributes, dependency graph, ALC wrapper, logging. fileciteturn1file26L1-L18 fileciteturn1file28L10-L24  
- **DemoPluginBridge**: `IDemoPlugin` interface used by host and plugins. fileciteturn1file36L1-L9  
- **DemoPlugin / DemoPluginReqTest**: sample plugins showing identity, dependency, and host callback usage. fileciteturn1file37L5-L26 fileciteturn1file35L5-L23  
- **URegistryDemo**: a simple console host loading `bin/Plugins/Debug/net9.0/`. fileciteturn1file33L26-L33

---

## Notes & Tips

- **Dependency IDs must match** the generated `IdPath.Name` of the target plugin (lower‑case, spaces → `_`). fileciteturn1file30L65-L73  
- **Verification (`Verify()`) is your last chance** to confirm the plugin is in a good state; return `true` to be marked ready. fileciteturn1file26L94-L106  
- **PluginSettings** currently exists as a placeholder for future configuration. fileciteturn1file25L1-L7

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

