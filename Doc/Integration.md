# Integration guide

[README](../README.md) · [Installation](Installation.md) · [API reference](Reference.md) · [Migration](Migration.md)

This guide describes **version 1.0.21**. Examples use the package's public API.
Place integration scripts, collections, prefabs, and mixers in your own project folders.

## 1. Prepare the project

[Install the package](Installation.md) and Odin Inspector.
If your game uses an assembly definition, reference SoundFlowSystem.Runtime.

| Asset or object | Purpose |
| --- | --- |
| AudioClip | Audio data |
| SoundsCollection | Sound keys, clips, and playback settings |
| Prefab with AudioSource | Template used to create sources |
| SoundFlowManagerSettings | Collections, template, pool sizes, and optional synchronizer |
| Your owner component | Creates and disposes the manager |
| AudioListener | Receives scene audio |

Use a simple source prefab: the pool clones its GameObject.
Avoid character scripts, cameras, or other components with unrelated Awake/OnEnable behavior.
Configure source defaults such as priority on this template. The pool disables Play On Awake on clones.

## 2. Organize collections

You can keep UI, SFX, Music, and Voice in separate collections and assign them to one manager.
This organizes content; it does not create separate pools.

| Example key | Suggested setup |
| --- | --- |
| ui_click | One clip, SpatialBlend = 0, IsLoop = false |
| footstep | Multiple clips, IsRandom = true, SpatialBlend = 1 |
| engine | IsLoop = true, SpatialBlend = 1 |
| music_main | IsLoop = true, SpatialBlend = 0 |
| voice_intro | IsLoop = false, SpatialBlend = 0 |

Keys are case-sensitive and must be unique across all assigned collections.
Every Clips entry must be assigned. IsRandom = false selects the first clip, not the next clip in sequence.
Random selection may repeat the same clip on consecutive calls.

The manager indexes keys at construction. Changing collection membership or Key afterward does not rebuild
the index. Construct a new manager for a different collection set.
Existing SoundData settings are read on each new playback. Changing shared SoundData affects subsequent
requests; it is not a per-call override.

## 3. Ownership and initialization order

The GameAudio component in the README creates the manager in Awake and disposes it in OnDestroy.
Pass ISoundFlowManager to consumers through your bootstrap or dependency injection setup.

If consumers access GameAudio through a scene reference, use it in Start or after explicit initialization.
Do not assume another object's Awake has already run.
Avoid creating multiple managers for the same service: each manager allocates its own pool.

Call all APIs on Unity's main thread. Dispatch network callbacks and background work to that thread first.
Do not Dispose immediately after Play: disposal cancels playback.
Use Stop/StopAll to cancel sounds and Dispose to end the service's lifetime.

## 4. Attach sound to a moving object

Use PlayInPosition for an effect at a fixed world position.
Pass a dedicated AudioSource for an engine or another effect that follows a moving object.

Save this example as EngineAudio.cs. It uses GameAudio from the README:

```csharp
using SoundFlowSystem.Data;
using SoundFlowSystem.Managers;
using UnityEngine;

public sealed class EngineAudio : MonoBehaviour
{
    [SerializeField] private GameAudio audioService;
    [SerializeField] private AudioSource engineSource;

    private ISoundFlowManager sounds;
    private PlayProcessData playback;

    private void Start()
    {
        sounds = audioService.Sounds;
        // Configure engine with IsLoop = true in the collection.
        playback = sounds.Play("engine", engineSource);
    }

    private void OnDestroy()
    {
        if (playback != null)
            sounds?.Stop(playback.Id);
    }
}
```

The source remains on its original object and follows that Transform.
A source supports one managed playback at a time: starting another sound on it cancels the previous
playback without invoking its completion callback.

Disabling the source's GameObject cancels its playback. Re-enable and start it again explicitly;
the manager does not automatically resume sounds after SetActive.

## 5. Create a dedicated source

Create returns a template-based source outside the shared pool.
Use it for a dedicated channel or an object without a pre-existing AudioSource.

```csharp
using SoundFlowSystem.Data;
using SoundFlowSystem.Managers;
using UnityEngine;

public sealed class DedicatedAudio : MonoBehaviour
{
    [SerializeField] private GameAudio audioService;

    private ISoundFlowManager sounds;
    private AudioSource source;
    private PlayProcessData playback;

    private void Start()
    {
        sounds = audioService.Sounds;
        source = sounds.Create();
        source.transform.SetParent(transform, false);
        source.transform.localPosition = Vector3.zero;
        playback = sounds.Play("engine", source);
    }

    private void OnDestroy()
    {
        if (playback != null)
            sounds?.Stop(playback.Id);

        if (source != null)
            Destroy(source.gameObject);
    }
}
```

The manager stops managed playbacks on external sources but never destroys those objects.
MaxPoolSize does not limit Create or external AudioSources.
Use pooled Play calls for frequent one-shots instead of allocating through Create every time.

## 6. Music, delay, pause, and completion

For music, configure IsLoop = true and SpatialBlend = 0.
To change tracks, stop the previous ID and start the next sound.
Crossfade, fade-in/fade-out, and playlist scheduling are not built in.

Delay is expressed in seconds. The source is reserved immediately and starts through PlayScheduled.
Pausing during a delay preserves its remaining duration.

Use the manager's Pause/Resume for individual playbacks.
AudioListener.pause supports a global audio pause; sources with ignoreListenerPause are exceptions.
For straightforward behavior, leave ignoreListenerPause disabled, especially on delayed sources.

onFinished runs after natural completion and after releasing the source, so a callback can start another
sound even at pool capacity. Never continue controlling a completed pooled playback's AudioSource:
it may already belong to another sound.

Stop, StopAll, replacement, Dispose, and source destruction/disabling do not invoke onFinished.
Rejected playback requests do not invoke it either. Handle gameplay cancellation separately.

## 7. Route audio through a mixer

Create your project's AudioMixer and groups, such as SFX, Music, and Voice.
Assign the appropriate group to each SoundData.Group field.
With isOverwriteSettings = true, the manager sets outputAudioMixerGroup accordingly.

With isOverwriteSettings = false, an external source keeps its group.
A pooled source starts from its template's settings before each lease.

The package does not create mixer groups or store user volume preferences.
Implement master volume, mute controls, and settings persistence in your game audio service.
SoundData.Vloume controls the individual sound; its original spelling is retained for compatibility.

## 8. Implement custom playback conditions

A condition is serializable data implementing IPlayCondition.
A checker implements IPlayConditionChecker and evaluates current game state.

Example: allow SFX only while the user setting is enabled.

```csharp
using System;
using SoundFlowSystem.Rules.Checkers;
using SoundFlowSystem.Rules.Conditions;

[Serializable]
public sealed class SfxEnabledCondition : IPlayCondition
{
}

public sealed class SfxEnabledChecker : IPlayConditionChecker
{
    private readonly Func<bool> isEnabled;

    public SfxEnabledChecker(Func<bool> isEnabled)
    {
        this.isEnabled = isEnabled
            ?? throw new ArgumentNullException(nameof(isEnabled));
    }

    public bool Check(IPlayCondition condition)
    {
        return condition is SfxEnabledCondition && isEnabled();
    }
}
```

In GameAudio, register the checker **before** constructing the manager:

```csharp
// Add this field to GameAudio:
[SerializeField] private bool sfxEnabled = true;

// Replace the body of Awake with:
settings.AddConditionChecker(
    new SfxEnabledCondition(),
    new SfxEnabledChecker(() => sfxEnabled));

Sounds = new SoundFlowManager(settings);
```

Add a SfxEnabledCondition instance to each relevant sound's Conditions array in the Inspector.
Alternatively, assign the array in code before creating the manager:

```csharp
soundData.Conditions = new SoundFlowSystem.Rules.Conditions.IPlayCondition[]
{
    new SfxEnabledCondition()
};
```

Conditions are evaluated before starting, not continuously.
Disabling sfxEnabled does not stop existing sounds; call Stop or StopAll separately.
Every condition must pass. An empty or null array allows playback.
A false result makes Play return null without logging an error.

Registration uses the condition's exact type. A base-type checker is not automatically used for subclasses.
Registering the same type again replaces the Settings entry.
The manager copies registrations during construction; later changes to Settings.Rules do not replace
its checkers. A checker can still observe changing game state, as the delegate above does.

Keep Check free of side effects. Exceptions thrown by custom checkers propagate to the caller;
they do not have the exception isolation provided for onFinished callbacks.

## 9. Connect a network transport

The package provides an adapter boundary, not an RPC implementation or synchronized network clock.
SimpleNetworkAudioSynchronizer is an empty example and transmits nothing.

Save this minimal transport bridge as GameNetworkAudio.cs:

```csharp
using System;
using SoundFlowSystem.Network;
using UnityEngine;

public sealed class GameNetworkAudio : BaseNetworkAudioSynchronizer
{
    // Subscribe your transport and send the key and position.
    public event Action<string, Vector3> SendRequested;

    public override void PlayNetwork(string soundKey, Vector3 inPosition)
    {
        SendRequested?.Invoke(soundKey, inPosition);
    }

    // Call on Unity's main thread after receiving a transport message.
    public void Receive(string soundKey, Vector3 position)
    {
        _soundFlowManager?.PlayInPosition(soundKey, position);
    }
}
```

1. Add the component to a scene object.
2. Assign Settings.NetworkSynchronizer before manager construction,
   or call sounds.SetNetworkSynchronizer(adapter).
3. Subscribe your transport to SendRequested and unsubscribe when its connection ends.
4. Validate incoming messages in the transport and invoke Receive on the main thread.
5. Call sounds.PlayNetworkInPosition(key, position) to request transmission.

Incoming messages must call local Play/PlayInPosition, not another network-send method.
PlayNetwork does not play locally by itself. Decide whether the sender receives its own event:
immediate local playback plus an unfiltered echoed event produces duplicate audio.

Clients need matching keys and content.
IsRandom selects clips locally, so it does not guarantee the same variant on every client.
Precise synchronized starts, clip selection, network loop cancellation, duplicate-message handling,
and event rate limits belong to your transport protocol.

SetNetworkSynchronizer(null) detaches the current adapter.
When overriding Init/Detach, invoke the base methods and clean up your own subscriptions.
Use a separate synchronizer instance for each manager.

## 10. Scene lifetime

Constructing a manager creates an internal SoundFlowSystem GameObject in the active scene,
containing the driver and pooled sources. Unloading that scene destroys the driver and disposes the manager.

For scene-based audio, create and dispose the manager with the scene owner.
For additive scenes, you can keep the service in a bootstrap scene that remains loaded:
make that scene active when constructing the manager.
Use a prefab asset as the template so unloading a different scene cannot destroy the template.

There is no built-in persistent global-manager mode.
DontDestroyOnLoad on your owner alone does not transfer the internal SoundFlowSystem object.
Do not retain and reuse a manager after its driver has been destroyed.

Stop/StopAll remain safe after Dispose.
Play, Create, network playback methods, and SetNetworkSynchronizer throw ObjectDisposedException afterward.

## 11. Check your integration manually

1. Play a 2D sound and confirm that it is audible.
2. Check a 3D source's position and distances relative to the AudioListener.
3. Start a loop and invoke Pause, Resume, and Stop from gameplay events.
4. Confirm that the caller handles null when the pool is full.
5. Unload the owner scene and confirm that its sounds stop.
6. For networking, confirm that local playback is not duplicated.

The package does not include automated tests or a test assembly definition.
