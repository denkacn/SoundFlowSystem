using System;
using System.Collections.Generic;
using SoundFlowSystem.Data;
using SoundFlowSystem.Libraries;
using SoundFlowSystem.Network;
using SoundFlowSystem.Pools;
using SoundFlowSystem.Rules.Factories;
using SoundFlowSystem.Settings;
using UnityEngine;

namespace SoundFlowSystem.Managers
{
    public class SoundFlowManager : ISoundFlowManager, IDisposable
    {
        // Identity and ownership are independent of the mutable legacy PlayProcessData handle.
        private sealed class Playback
        {
            public string Id;
            public AudioSource Source;
            public bool Pooled;
            public Action OnFinished;
            public double StartTime;
            public int StartFrame;
            public bool Paused;
            public double RemainingDelay;
        }

        private readonly Dictionary<string, SoundData> _soundsLibrary = new Dictionary<string, SoundData>();
        private readonly Dictionary<string, Playback> _playProcesses = new Dictionary<string, Playback>();
        private readonly Dictionary<AudioSource, Playback> _sourceProcesses = new Dictionary<AudioSource, Playback>();
        private readonly List<Playback> _finished = new List<Playback>();
        private readonly RulesFactory _rulesFactory = new RulesFactory();
        private BaseAudioSourcePool _pool;
        private SoundFlowRunner _runner;
        private BaseNetworkAudioSynchronizer _networkAudioSynchronizer;
        private bool _disposed;

        public int ActivePlaybackCount => _playProcesses.Count;

        public SoundFlowManager(SoundFlowManagerSettings soundFlowManagerSettings)
        {
            var settings = soundFlowManagerSettings;
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.BaseAudioSource == null)
                throw new ArgumentException("BaseAudioSource is required.", nameof(settings));
            if (settings.InitialPoolSize < 0 || settings.MaxPoolSize < 1 || settings.InitialPoolSize > settings.MaxPoolSize)
                throw new ArgumentException("Pool sizes must satisfy 0 <= InitialPoolSize <= MaxPoolSize and MaxPoolSize > 0.", nameof(settings));

            if (settings.Rules != null)
                foreach (var pair in settings.Rules) _rulesFactory.Add(pair.Key, pair.Value);
            foreach (var collection in settings.SoundsCollections ?? Array.Empty<SoundsCollection>())
            {
                if (collection == null) throw new ArgumentException("SoundsCollections contains a missing collection.", nameof(settings));
                foreach (var sound in collection.Get())
                {
                    ValidateSound(sound);
                    if (_soundsLibrary.ContainsKey(sound.Key))
                        throw new ArgumentException($"Duplicate sound key '{sound.Key}'.", nameof(settings));
                    _soundsLibrary.Add(sound.Key, sound);
                }
            }

            // Validate configuration before allocating scene objects.
            var host = new GameObject("SoundFlowSystem");
            _runner = host.AddComponent<SoundFlowRunner>();
            try
            {
                _pool = new BaseAudioSourcePool(settings.BaseAudioSource, settings.InitialPoolSize, settings.MaxPoolSize, host.transform);
                _runner.Initialize(this);
                SetNetworkSynchronizer(settings.NetworkSynchronizer);
            }
            catch { Dispose(); throw; }
        }

        public PlayProcessData Play(string soundKey, AudioSource audioSource = null, Action onFinished = null, bool isOverwriteSettings = true)
            => PlayIt(soundKey, null, audioSource, onFinished, isOverwriteSettings);

        public PlayProcessData PlayInPosition(string soundKey, Vector3 position, AudioSource audioSource = null, Action onFinished = null, bool isOverwriteSettings = true)
            => PlayIt(soundKey, position, audioSource, onFinished, isOverwriteSettings);

        public void Stop(string playProcessId)
        {
            if (playProcessId != null && _playProcesses.TryGetValue(playProcessId, out var playback))
                Complete(playback, false);
        }

        public void StopAll()
        {
            var active = new List<Playback>(_playProcesses.Values);
            foreach (var playback in active) Complete(playback, false);
        }

        public void Pause(string playProcessId)
        {
            if (playProcessId == null || !_playProcesses.TryGetValue(playProcessId, out var playback) || playback.Paused || playback.Source == null) return;
            playback.RemainingDelay = Math.Max(0, playback.StartTime - AudioSettings.dspTime);
            playback.Paused = true;
            if (playback.RemainingDelay > 0) playback.Source.Stop();
            else playback.Source.Pause();
        }

        public void Resume(string playProcessId)
        {
            if (playProcessId == null || !_playProcesses.TryGetValue(playProcessId, out var playback) || !playback.Paused || playback.Source == null) return;
            playback.Paused = false;
            playback.StartFrame = Time.frameCount;
            playback.StartTime = AudioSettings.dspTime + playback.RemainingDelay;
            if (playback.RemainingDelay > 0) playback.Source.PlayScheduled(playback.StartTime);
            else playback.Source.UnPause();
        }

        // Dedicated sources belong to the caller, including destruction.
        public AudioSource Create()
        {
            ThrowIfDisposed();
            return _pool.CreateAudioSource();
        }

        public void PlayNetwork(string soundKey) => PlayNetworkInPosition(soundKey, Vector3.zero);

        public void PlayNetworkInPosition(string soundKey, Vector3 position)
        {
            ThrowIfDisposed();
            if (_networkAudioSynchronizer != null) _networkAudioSynchronizer.PlayNetwork(soundKey, position);
        }

        public void SetNetworkSynchronizer(BaseNetworkAudioSynchronizer networkAudioSynchronizer)
        {
            var synchronizer = networkAudioSynchronizer;
            ThrowIfDisposed();
            if (_networkAudioSynchronizer == synchronizer) return;
            if (_networkAudioSynchronizer != null) _networkAudioSynchronizer.Detach(this);
            _networkAudioSynchronizer = synchronizer;
            if (_networkAudioSynchronizer != null) _networkAudioSynchronizer.Init(this);
        }

        private PlayProcessData PlayIt(string key, Vector3? position, AudioSource source, Action onFinished, bool overwrite)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(key) || !_soundsLibrary.TryGetValue(key, out var sound))
            {
                Debug.LogError($"[SoundFlowManager] Unknown sound key '{key}'.");
                return null;
            }
            // Collections can be changed at runtime.
            try { ValidateSound(sound); }
            catch (ArgumentException exception)
            {
                Debug.LogError($"[SoundFlowManager] {exception.Message}");
                return null;
            }
            if (sound.Conditions != null)
                foreach (var condition in sound.Conditions)
                    if (!_rulesFactory.Get(condition).Check(condition)) return null;

            if (source != null && !_pool.Contains(source) && (!source.enabled || !source.gameObject.activeInHierarchy))
            {
                Debug.LogError($"[SoundFlowManager] Source for '{key}' must be active and enabled.");
                return null;
            }
            if (source != null && _sourceProcesses.TryGetValue(source, out var previous)) Complete(previous, false);
            bool suppliedSource = source != null;
            bool pooled = source == null || _pool.Contains(source);
            if (source == null) source = _pool.Get();
            else if (pooled && !_pool.TryAcquire(source)) return null;
            if (source == null)
            {
                Debug.LogWarning($"[SoundFlowManager] Pool limit reached; skipped '{key}'.");
                return null;
            }

            var playback = new Playback
            {
                Id = Guid.NewGuid().ToString("N"), Source = source, Pooled = pooled, OnFinished = onFinished,
                StartTime = AudioSettings.dspTime + sound.Delay, StartFrame = Time.frameCount
            };
            try
            {
                source.Stop();
                if (position.HasValue) source.transform.position = position.Value;
                else if (!suppliedSource) source.transform.position = Vector3.zero;
                source.clip = sound.Clips[sound.IsRandom ? UnityEngine.Random.Range(0, sound.Clips.Length) : 0];
                if (overwrite) PrepareAudioSource(source, sound);
                _playProcesses.Add(playback.Id, playback);
                _sourceProcesses.Add(source, playback);
                if (sound.Delay > 0) source.PlayScheduled(playback.StartTime);
                else source.Play();
                return new PlayProcessData(playback.Id, sound, source);
            }
            catch
            {
                Complete(playback, false);
                if (pooled) _pool.Release(source);
                throw;
            }
        }

        internal void Tick()
        {
            if (_disposed) return;
            _finished.Clear();
            foreach (var playback in _playProcesses.Values)
            {
                var source = playback.Source;
                if (source == null || !source.enabled || !source.gameObject.activeInHierarchy)
                    _finished.Add(playback);
                else if (!playback.Paused && !(AudioListener.pause && !source.ignoreListenerPause)
                         && Time.frameCount > playback.StartFrame && AudioSettings.dspTime >= playback.StartTime
                         && !source.isPlaying)
                    _finished.Add(playback);
            }
            // Callbacks may play/stop/dispose; never enumerate the active dictionary here.
            for (int i = 0; i < _finished.Count; i++)
            {
                var playback = _finished[i];
                bool notify = playback.Source != null && playback.Source.enabled && playback.Source.gameObject.activeInHierarchy;
                Complete(playback, notify);
            }
            _finished.Clear();
        }

        private void Complete(Playback playback, bool notify)
        {
            if (!_playProcesses.Remove(playback.Id)) return;
            _sourceProcesses.Remove(playback.Source);
            if (playback.Source != null) playback.Source.Stop();
            if (playback.Pooled) _pool.Release(playback.Source);
            if (notify && playback.OnFinished != null)
            {
                try { playback.OnFinished(); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }

        private static void PrepareAudioSource(AudioSource source, SoundData sound)
        {
            source.volume = sound.Vloume;
            source.pitch = sound.Pitch;
            source.loop = sound.IsLoop;
            source.outputAudioMixerGroup = sound.Group;
            source.spatialBlend = sound.SpatialBlend;
            source.dopplerLevel = sound.DopplerLevel;
            source.spread = sound.Spread;
            source.rolloffMode = sound.RolloffMode;
            source.minDistance = sound.MinDistance;
            source.maxDistance = sound.MaxDistance;
        }

        private void ValidateSound(SoundData sound)
        {
            if (sound == null || string.IsNullOrWhiteSpace(sound.Key)) throw new ArgumentException("Each sound must have a non-empty key.");
            void Invalid(string message) => throw new ArgumentException($"Sound '{sound.Key}': {message}");
            if (sound.PoolId != SoundFlowConstantsData.DefaultPoolId) Invalid($"unknown pool '{sound.PoolId}'.");
            if (sound.Clips == null || sound.Clips.Length == 0) Invalid("at least one clip is required.");
            foreach (var clip in sound.Clips) if (clip == null) Invalid("a clip is missing.");
            if (!InRange(sound.Delay, 0, float.MaxValue)) Invalid("Delay must be finite and non-negative.");
            if (!InRange(sound.Vloume, 0, 1)) Invalid("Volume must be between 0 and 1.");
            if (!InRange(sound.Pitch, -3, 3)) Invalid("Pitch must be between -3 and 3.");
            if (!InRange(sound.SpatialBlend, 0, 1) || !InRange(sound.DopplerLevel, 0, 5) || sound.Spread < 0 || sound.Spread > 360)
                Invalid("invalid spatial settings.");
            if (!InRange(sound.MinDistance, 0, float.MaxValue) || !InRange(sound.MaxDistance, sound.MinDistance, float.MaxValue))
                Invalid("distances must be finite and 0 <= MinDistance <= MaxDistance.");
            if (!Enum.IsDefined(typeof(AudioRolloffMode), sound.RolloffMode)) Invalid("invalid rolloff mode.");
            if (sound.Conditions != null)
                foreach (var condition in sound.Conditions)
                    if (condition == null || _rulesFactory.Get(condition) == null) Invalid("a condition or its registered checker is missing.");
        }

        private static bool InRange(float value, float minimum, float maximum)
            => !float.IsNaN(value) && !float.IsInfinity(value) && value >= minimum && value <= maximum;

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SoundFlowManager));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopAll();
            if (_networkAudioSynchronizer != null) _networkAudioSynchronizer.Detach(this);
            _networkAudioSynchronizer = null;
            _pool?.Dispose();
            _soundsLibrary.Clear();
            if (_runner != null)
            {
                _runner.Initialize(null);
                if (Application.isPlaying) UnityEngine.Object.Destroy(_runner.gameObject);
                else UnityEngine.Object.DestroyImmediate(_runner.gameObject);
            }
            _runner = null;
        }
    }
}
