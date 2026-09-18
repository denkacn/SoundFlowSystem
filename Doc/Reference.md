# Settings and API reference

[README](../README.md) · [Installation](Installation.md) · [Integration](Integration.md) · [Migration](Migration.md)

## SoundFlowManagerSettings

| Field | Default | Purpose |
| --- | --- | --- |
| SoundsCollections | Empty array | Collections indexed during manager construction |
| BaseAudioSource | Unassigned | Required AudioSource template |
| NetworkSynchronizer | Unassigned | Optional network adapter |
| InitialPoolSize | 5 | Number of preallocated pooled sources |
| MaxPoolSize | 32 | Shared source pool limit |
| Rules | Empty dictionary | Exact condition type to checker mapping |

Require 0 ≤ InitialPoolSize ≤ MaxPoolSize and MaxPoolSize ≥ 1.
An empty collection list is allowed, but contains no sounds to play.
Null elements within the collection list are invalid.

Rules is a regular Dictionary. Register checkers through AddConditionChecker in code;
do not rely on standard Unity field serialization to save that dictionary.

## SoundData

| Field | Default | Behavior |
| --- | --- | --- |
| Key | Unassigned | Non-empty unique key; case-sensitive |
| PoolId | base_pool | The only pool currently registered by the manager |
| Clips | Empty array | At least one clip required; null elements are invalid |
| IsLoop | false | Repeat until cancelled |
| IsRandom | false | true selects a random clip; false selects the first |
| Delay | 0 | Non-negative start delay in seconds |
| Vloume | 1 | Volume from 0 to 1; original spelling retained |
| Pitch | 1 | Value from -3 to 3 passed to AudioSource.pitch |
| Group | null | AudioMixerGroup; null means no assigned group |
| SpatialBlend | 1 | 0 is 2D, 1 is 3D, intermediate values blend both |
| DopplerLevel | 1 | Value from 0 to 5 |
| Spread | 0 | Integer value from 0 to 360 |
| RolloffMode | Linear | AudioSource attenuation mode |
| MinDistance | 1 | Non-negative minimum distance |
| MaxDistance | 5 | Must be at least MinDistance |
| Conditions | Empty array | Conditions that must permit playback |

Validated float settings must be finite: NaN and Infinity are rejected.
Pitch = 0 is not a manager pause command; use Pause(id). Start with Pitch = 1 for normal playback.
For Custom rolloff, the curve comes from the source; SoundData has no separate curve field.

Volume, pitch, loop, mixer group, spatial blend, Doppler, spread, rolloff, and distances are applied
when isOverwriteSettings = true. Key, Clips, IsRandom, Delay, and Conditions are used regardless of this flag.

## ISoundFlowManager

| Member | Result and purpose |
| --- | --- |
| Play(key, source = null, onFinished = null, isOverwriteSettings = true) | Starts playback; returns PlayProcessData or null |
| PlayInPosition(key, position, source = null, onFinished = null, isOverwriteSettings = true) | Starts playback and explicitly moves the source |
| Stop(id) | Cancels one playback; unknown or null IDs are ignored |
| StopAll() | Cancels all manager-owned playbacks |
| Pause(id) | Pauses one playback while keeping its source reserved |
| Resume(id) | Resumes playback, including its remaining delay |
| ActivePlaybackCount | Count of playing, delayed, and paused records |
| Create() | Creates a dedicated, caller-owned AudioSource |
| PlayNetwork(key) | Calls the adapter with Vector3.zero |
| PlayNetworkInPosition(key, position) | Calls the adapter with the supplied position |
| SetNetworkSynchronizer(adapter) | Installs/replaces the adapter; null detaches it |
| Dispose() | Ends the manager lifetime and releases its resources |

The table abbreviates audioSource to source and soundKey to key.
Use the full parameter names for named arguments.

### When playback returns null

- Empty or unknown key: logs an Error.
- Sound data became invalid after construction: logs an Error.
- A condition checker returned false: normal rejection without an Error.
- The supplied external source is disabled or inactive: logs an Error.
- The shared pool is full: logs a Warning.

Rejected requests do not invoke onFinished.
Constructor errors, use after Dispose, and custom checker exceptions are not converted to null.

### Source position and external control

Play without a source rents one and positions it at Vector3.zero.
Play with a source preserves its current position.
PlayInPosition always assigns the source Transform's world position.

Do not control the same external source from multiple managers or directly through other
Play/Stop/Pause calls while it is managed.
Direct AudioSource changes bypass the manager's lifecycle contract.
The driver detects a destroyed or disabled source and cancels its record.

### PlayProcessData

| Field | Purpose |
| --- | --- |
| Id | Identity of one playback |
| SoundData | Reference to the original sound settings |
| AudioSource | Source used for that playback |

Fields remain public for compatibility, but treat the handle as read-only.
Keeping a PlayProcessData object does not mean playback is still active.
After completion, its pooled source may already be playing another sound.
Passing an old ID to Stop is safe and does not stop a newer playback.

Natural completion updates ActivePlaybackCount during the driver's Update.
This is a count of manager records, not hardware audio voices.

## Source ownership

| Source origin | Who destroys it | Caller must Release |
| --- | --- | --- |
| Play without an AudioSource | Manager | No |
| Supplied scene object | Object owner | No |
| sounds.Create() | Caller | No; it is not pooled |
| Direct pool.Get() | Pool during Dispose | Yes, through pool.Release(source) |

Do not manually remove or reparent internal pooled objects.
Most integrations should use the manager rather than operating a pool directly.

## IAudioSourcePool and BaseAudioSourcePool

Direct pool usage requires explicit ownership management:

```csharp
var pool = new SoundFlowSystem.Pools.BaseAudioSourcePool(
    template, initialSize: 2, maxSize: 8);

var source = pool.Get(); // Can return null.
if (source != null)
{
    source.clip = clip;
    source.Play();

    // Later, when your code ends this source's lease:
    source.Stop();
    pool.Release(source);
}

// At the end of the owner's lifetime, not immediately after starting:
pool.Dispose();
```

The pool itself does not monitor clip completion. A source remains leased until Release,
even when isPlaying is false. Automatic playback monitoring belongs to SoundFlowManager.

The single-argument constructor uses initialSize = 5 and maxSize = 32.
Count and LeasedCount are exposed by BaseAudioSourcePool.
TryAcquire and Contains are helpers on that concrete type.
SoundFlowManager currently has no public API for registering additional pools.

## Constant generation

SoundsCollectionConstantGenerator.GenerateClassFile(path, prefix, soundsData)
validates input and writes C# source inside Assets.
GenerateSource(prefix, soundsData) returns source text without writing a file.

For path = `/Generated/Audio/`, prefix = `Game`, and Key = `ui_click`:

- File: `Assets/Generated/Audio/GameSoundsCollectionConstants.cs`.
- Namespace: `App.SoundFlowSystem.Libraries`.
- Class: `GameSoundsCollectionConstants`.
- Field: `UiClick`, with value `ui_click`.

Generation uses invariant casing and escaped string literals.
Leading digits receive an underscore prefix; Unicode letters are supported.
Keys such as hit and HIT collide as generated identifiers even though playback keys are case-sensitive.
Collisions, duplicate keys, empty names, and invalid prefixes are rejected before overwriting a file.
