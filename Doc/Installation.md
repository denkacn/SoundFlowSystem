# Installation

[README](../README.md) · [Integration](Integration.md) · [Reference](Reference.md) · [Troubleshooting](Migration.md)

SoundFlowSystem is a Unity audio plugin distributed as a UPM-compatible package.
Its package ID is `com.puzikgames.soundflowsystem`; its display name is **Sound Flow System**.

## Requirements

- Unity: the manifest declares 2022.1 as the minimum. The stabilized runtime was validated in 6000.0.59f2.
- Odin Inspector: install it in your Unity project separately before importing this package.
  It supplies the inspector attributes used by SoundFlowSystem and is not bundled or installed automatically.
- Git: required for Git URL installation. Ensure `git --version` works in a terminal.
- UniTask: not required by the current SoundFlowSystem runtime. Other project code may still need it.

Choose exactly one installation method below. Keep your sound collections, source prefabs, mixer assets,
and integration scripts in your own project folders so package updates do not replace your content.

## Option A: Install from Git

1. Open your Unity project and the Package Manager window.
2. Open its **+** menu.
3. Select **Install package from git URL**; older editors may call it **Add package from git URL**.
4. Enter:

   ```text
   https://github.com/denkacn/SoundFlowSystem.git
   ```

5. Select Install and wait for package resolution and compilation.

On Windows, Git must be on PATH. If you just installed Git, restart Unity so it sees the updated environment.
See [Unity's Git installation instructions](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-ui-giturl.html).

The repository itself contains package.json at its root. Do not append the host project's
`Assets/SoundFlowSystem` directory as a Git URL subpath.

The URL installs code published in the remote repository. Local changes are unavailable to other users
until they are committed and pushed. Updating the manifest's version does not publish code or create a Git tag.

## Option B: Install a local checkout

Use this when developing the package or using a downloaded copy.

1. Clone or extract the repository to a dedicated directory, such as `D:/UnityPackages/SoundFlowSystem`.
2. In Unity's Package Manager, open **+ → Install package from disk**.
3. Select `D:/UnityPackages/SoundFlowSystem/package.json`.
4. Wait for import and compilation.

Unity references that directory, so keep it available. Editing its source changes the local package.
For the standard local-package workflow, store it outside Assets to avoid loading the same scripts twice.
See [Unity's local package instructions](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-ui-local.html).

In this repository's development host project, the package is already under
`Assets/SoundFlowSystem`. It is loaded directly from Assets; do not add it again through Package Manager.

## Option C: Copy into Assets

1. Copy the package directory to `Assets/SoundFlowSystem`.
2. Preserve its .meta files.
3. Wait for Unity to import the scripts and resolve the assembly.
4. If migrating from a UPM installation, remove that installation before keeping the Assets copy.

Copy only the package, not a host project's Library, Temp, Logs, or ProjectSettings directories.
The package root contains package.json, SoundFlowSystem.Runtime.asmdef, and Runtime.

For this method, future updates are managed by replacing the package files yourself.
Keep custom integration code and content outside the package directory.

## Connect your game assembly

If your scripts use their own .asmdef file:

1. Select that assembly definition in Unity.
2. Add `SoundFlowSystem.Runtime` to its Assembly Definition References.
3. Apply the change and wait for compilation.

Scripts in the default Assembly-CSharp normally see the package automatically.
Do not create a second runtime assembly with the same name.

## Verify installation and play the first sound

Confirm that:

- There are no missing Sirenix.OdinInspector namespace errors.
- SoundFlowManagerSettings can be added to a GameObject.
- The Create menu contains Tools → SoundFlowSystem → SoundsCollection.

Installation alone does not play audio. Follow the [README setup](../README.md) to create a collection,
assign a clip and an AudioSource template, configure the scene settings, and initialize GameAudio.
Include an active AudioListener in the scene.

## Update or remove the package

For Git installations, use your editor's Package Manager update controls or switch the Git reference
to a published commit/tag. Do not edit cached package files under Library/PackageCache.
A package version string is not a Git reference; only use a tag when that tag actually exists.

For local installations, update the checkout or extracted package directory.
For Assets installations, replace the package folder while preserving your separately stored content.

Before changing installation methods, remove the previous copy.
For UPM, remove Sound Flow System through Package Manager.
For an Assets copy, remove its package folder and matching folder .meta after migrating dependent scripts
and components. Package removal does not automatically remove your game code's references to its API.

Read [migration notes](Migration.md) before updating an existing integration.

## Installation problems

| Symptom | Resolution |
| --- | --- |
| Git executable not found | Install Git, add it to PATH, and restart Unity |
| Git authentication or 403 error | Check access to the repository and the Git account used by the machine |
| Missing Sirenix.OdinInspector | Install/import Odin Inspector and wait for its compilation |
| Duplicate types or assembly names | Remove the duplicate Assets or UPM installation |
| Game scripts cannot resolve SoundFlowSystem | Add the runtime assembly reference to your game asmdef |
| New version not visible from Git | Confirm that changes are pushed and the installed reference resolves to that commit |
| Local package disappears | Restore its directory or update the local package reference |
