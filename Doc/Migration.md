# Migration and troubleshooting

[README](../README.md) · [Installation](Installation.md) · [Integration](Integration.md) · [Reference](Reference.md)

## Version 1.0.21

The package version increases from 1.0.20 to 1.0.21.
This update adds an expanded user guide and detailed English installation and integration documentation.
Automated tests introduced during stabilization have been removed, including their assembly definition.
The original Runtime/Tests/SoundFlowManagerTest remains as a manual Odin button example.

## Moving from the original implementation to the stabilized API

These changes may already be present if you use the stabilized implementation.
For integrations based on the earlier API:

1. ISoundFlowManager now extends IDisposable and exposes StopAll, Pause, Resume,
   and ActivePlaybackCount. Update custom implementations accordingly.
2. IAudioSourcePool extends IDisposable and exposes Release.
   Pair direct Get calls with explicit Release calls.
3. Dispose the manager in its owner's OnDestroy or equivalent service shutdown.
4. Stop and source replacement no longer invoke onFinished. Handle cancellation separately.
5. Invalid collections fail during construction. Fix empty keys, missing clips,
   duplicates, and unregistered condition checkers.
6. PoolId must currently be base_pool; changing the field does not register another pool.
7. Handle null from Play because the shared pool has a MaxPoolSize limit.
8. Play with an external source preserves position; SoundData.Pitch is now applied.
9. UniTask is no longer required by this runtime. Remove it from a project only if no other code needs it.
10. Pass null to SetNetworkSynchronizer to disconnect the current adapter.

Existing Play/PlayInPosition/Stop/Create signatures are retained.
Vloume keeps its original spelling to preserve serialized assets and source compatibility.
Do not manually rename that field just to correct the spelling.

## Common problems

| Symptom or message | What to check |
| --- | --- |
| Missing Sirenix.OdinInspector | Install Odin Inspector and finish importing its assemblies |
| Game assembly cannot find SoundFlowSystem | Reference SoundFlowSystem.Runtime in the game asmdef |
| BaseAudioSource is required | Assign an AudioSource template in Settings |
| Each sound must have a non-empty key | Remove or fill empty collection entries |
| Duplicate sound key | Check keys across all assigned collections |
| at least one clip is required / a clip is missing | Fill Clips and remove unassigned slots |
| unknown pool | Use PoolId = base_pool |
| condition or its registered checker is missing | Register before construction; remove null Conditions entries |
| Unknown sound key | Check spelling, case, and assigned collections |
| Pool limit reached | Check remaining loops/pauses and choose an appropriate MaxPoolSize |
| ObjectDisposedException | Check driver scene lifetime and references to an old manager |
| Play returned null without an Error | A custom condition may have rejected playback |
| No audible sound | Check the listener, clip, source settings, and mixer output |
| 3D sound is near the world origin | Use PlayInPosition; pooled Play starts at Vector3.zero |
| Character moved when playing audio | Avoid PlayInPosition with a source on the character's Transform |
| Sound does not follow the character | Pass the character's own AudioSource to Play |
| Callback did not run after Stop | Expected cancellation behavior; onFinished is for natural completion |
| Sound completed after AudioSource.Pause | Use the manager's Pause/Resume methods |
| Sound continues at Time.timeScale = 0 | Use Pause(id) or AudioListener.pause |
| Network call does not play locally | PlayNetwork only invokes the adapter; implement delivery and local playback |
| SimpleNetworkAudioSynchronizer does nothing | It is an empty transport example |
| Different keys fail constant generation | Their identifiers may collide, for example hit and HIT |

See [installation troubleshooting](Installation.md) for Git, Package Manager, and duplicate-package issues.

Do not use isPlaying as the only measure of whether a pooled source is free.
Delayed and paused playback still owns the source.

## Original manual example

Runtime/Tests/SoundFlowManagerTest is a MonoBehaviour with Inspector buttons, not an NUnit test suite.

1. Create a valid collection containing test_sfx_play with an assigned clip.
2. Configure SoundFlowManagerSettings and assign it to the example component.
3. Enter Play Mode and allow Start to initialize the manager.
4. Select Test Play Sound or Test Play Sound In Position.

The position example uses (10, 10, 10). Check distance to the AudioListener,
or set SpatialBlend = 0 for a 2D sound.
The example creates its own manager; do not add it beside your production audio service unless you
intentionally want a separate pool.
