# SoundFlowSystem

**Version 1.0.21**

An audio system for Unity with sound collections, key-based playback, reusable AudioSources,
delayed playback, loops, pause/resume, and custom playback conditions.

## Documentation

- [Installation](Doc/Installation.md)
- [Integration guide](Doc/Integration.md)
- [Settings and API reference](Doc/Reference.md)
- [Migration and troubleshooting](Doc/Migration.md)

## Install the plugin

SoundFlowSystem is distributed as a Unity package. **Install Odin Inspector separately first.**
UniTask is no longer required by this package.

The package manifest declares Unity 2022.1 as its minimum version.
The stabilized runtime was validated with Unity 6000.0.59f2; earlier versions were not revalidated.

### Unity Package Manager

1. Install Git and ensure the `git` command is available on your PATH.
2. Open the Package Manager window in Unity.
3. Open the **+** menu and select **Install package from git URL**
   (called **Add package from git URL** in some editor versions).
4. Paste this URL and select **Install**:

   ```text
   https://github.com/denkacn/SoundFlowSystem.git
   ```

5. Wait for import and compilation. Find **Sound Flow System** in the installed package list.

These steps follow the [Unity Git package installation guide](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-ui-giturl.html).
The package manifest is at the repository root, so no `?path=` suffix is needed.

### Local installation

Clone or download the repository, open Package Manager, choose **Install package from disk**,
and select the package's `package.json`. Keep this local package outside your project's Assets folder.
Alternatively, copy the package folder into `Assets/SoundFlowSystem` for a direct Assets installation.

Choose one method; installing both copies produces duplicate assemblies.
If your game scripts use an assembly definition, add `SoundFlowSystem.Runtime` to its references.
See the [installation guide](Doc/Installation.md) for local paths, updates, removal, and installation errors.

## Set up your first sound

1. Import an AudioClip and make sure the scene has an active AudioListener.
2. Create a simple prefab containing an AudioSource. Disable Play On Awake.
   Use a dedicated GameObject without gameplay scripts as the pool template.
3. Select **Create → Tools → SoundFlowSystem → SoundsCollection**.
4. Add one entry to Sounds Data:
   - Key: `ui_click`;
   - Pool Id: `base_pool`;
   - Clips: one assigned AudioClip;
   - Spatial Blend: `0` for a 2D interface sound;
   - Vloume: `1`, Pitch: `1`, Is Loop: off, Delay: `0`;
   - Conditions: an empty array.
5. Add `SoundFlowManagerSettings` to a scene object. Assign the collection to Sounds Collections
   and the prefab's AudioSource to Base Audio Source.
6. Start with Initial Pool Size = 5 and Max Pool Size = 32.
7. Add the component below and assign its Settings field.

Keys must be unique across **all collections assigned to the same manager**.
Remove empty entries and unassigned clip slots: invalid configuration is rejected during construction.

## Scene component

Save this script as `GameAudio.cs` in your game project:

```csharp
using SoundFlowSystem.Managers;
using SoundFlowSystem.Settings;
using UnityEngine;

public sealed class GameAudio : MonoBehaviour
{
    [SerializeField] private SoundFlowManagerSettings settings;

    public ISoundFlowManager Sounds { get; private set; }

    private void Awake()
    {
        Sounds = new SoundFlowManager(settings);
    }

    // Assign this method to a UI Button's onClick event.
    public void PlayUiClick()
    {
        Sounds?.Play("ui_click");
    }

    private void OnDestroy()
    {
        Sounds?.Dispose();
        Sounds = null;
    }
}
```

Enter Play Mode and invoke PlayUiClick, for example from a UI button.
Use your project's existing UI setup; this package does not create a Canvas or buttons.

SoundFlowManager is a plain C# object. Create it once for the service owner, not for every sound.
Its internal SoundFlowRunner is created automatically; do not add that component manually.

## Common playback scenarios

In the following snippets, `sounds` is an initialized `ISoundFlowManager`.
Add the referenced keys and clips to a collection before using them.

### One-shot 2D sound

```csharp
sounds.Play("ui_click");
```

Set SpatialBlend = 0 for this entry.

### Sound at a world position

```csharp
sounds.PlayInPosition("explosion", hitPosition);
```

Use SpatialBlend = 1 and configure MinDistance and MaxDistance.
The source stays at that position. To follow a moving object, pass its own AudioSource:

```csharp
sounds.Play("engine", engineAudioSource);
```

Play preserves a supplied source's position.
**PlayInPosition moves its Transform**, including a character's GameObject if the source is on that object.

### Music or another loop

Set IsLoop = true and SpatialBlend = 0 for `music_main`.

```csharp
var music = sounds.Play("music_main");
if (music != null)
{
    sounds.Pause(music.Id);
    sounds.Resume(music.Id);
    sounds.Stop(music.Id);
}
```

These demonstrate separate controls: call each from the corresponding gameplay event.
A loop does not finish on its own; retain its ID and stop it explicitly.

### Completion callback

```csharp
var voice = sounds.Play("voice_intro", onFinished: () =>
{
    Debug.Log("The voice line finished.");
});

if (voice == null)
{
    Debug.Log("Playback did not start.");
}
```

The callback runs after natural completion, after releasing the source.
Stop, StopAll, replacement on the same source, and Dispose do not invoke it.
Handle cancellation separately if gameplay needs a cancellation notification.

### Preserve an external source's settings

```csharp
sounds.Play("engine", engineAudioSource, isOverwriteSettings: false);
```

This preserves the external AudioSource's audio settings, including volume, pitch, and loop.
The clip, Delay, and Conditions still come from SoundData.

## Pool limits and cleanup

- Delayed and paused playbacks retain their source reservation.
- At MaxPoolSize, a new pooled request returns null and logs a warning; existing sounds keep playing.
- Check the result of Play/PlayInPosition before accessing its Id.
- Use the manager's Pause/Resume methods instead of calling AudioSource.Pause directly.
- Time.timeScale = 0 does not pause audio. Use per-playback pause or AudioListener.pause.
- StopAll cancels every active playback owned by that manager.
- Dispose stops playback, destroys pooled sources, and detaches the network synchronizer.
- External sources and sources returned by Create must be destroyed by their owner.

The manager belongs to the scene containing its internal driver. Create a new manager after that scene
unloads. Applying DontDestroyOnLoad only to GameAudio does not make the internal driver persistent.

## Generate sound keys

In the collection Inspector, set Path To Save to `/Generated/Audio/`, Prefix to `Game`,
and select Generate Constant.

The key `ui_click` produces `UiClick` in `GameSoundsCollectionConstants`:

```csharp
using App.SoundFlowSystem.Libraries;

// Use after generating the constants file:
sounds.Play(GameSoundsCollectionConstants.UiClick);
```

Files are generated inside Assets. Edit the collection and regenerate instead of editing generated values.
Give different collections different prefixes or output directories to avoid replacing the same file.

## Further integration

The [integration guide](Doc/Integration.md) covers dedicated sources, custom conditions,
scene ownership, mixer routing, and a network transport adapter.
The [reference](Doc/Reference.md) describes each setting and method.
The [migration guide](Doc/Migration.md) documents compatibility changes and common errors.

Automated tests are not included.
`Runtime/Tests/SoundFlowManagerTest.cs` is the original manual Odin button example;
it requires a configured sound with the key `test_sfx_play`.
