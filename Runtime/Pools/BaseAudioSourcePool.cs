using System;
using System.Collections.Generic;
using SoundFlowSystem.Data;
using UnityEngine;

namespace SoundFlowSystem.Pools
{
    public class BaseAudioSourcePool : IAudioSourcePool, IDisposable
    {
        private readonly AudioSource _prefab;
        private readonly Transform _parent;
        private readonly int _maxSize;
        private readonly List<AudioSource> _sources = new List<AudioSource>();
        private readonly HashSet<AudioSource> _leased = new HashSet<AudioSource>();
        private bool _disposed;

        public string Id => SoundFlowConstantsData.DefaultPoolId;
        public int Count => _sources.Count;
        public int LeasedCount => _leased.Count;

        public BaseAudioSourcePool(AudioSource audioSourcePrefab) : this(audioSourcePrefab, 5, 32, null) { }

        public BaseAudioSourcePool(AudioSource prefab, int initialSize, int maxSize, Transform parent = null)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (initialSize < 0 || maxSize < 1 || initialSize > maxSize) throw new ArgumentOutOfRangeException(nameof(initialSize));
            _prefab = prefab;
            _parent = parent;
            _maxSize = maxSize;
            try
            {
                for (int i = 0; i < initialSize; i++)
                {
                    var source = CreatePooledSource();
                    source.gameObject.SetActive(false);
                    _sources.Add(source);
                }
            }
            catch { Dispose(); throw; }
        }

        public AudioSource Get()
        {
            ThrowIfDisposed();
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                var source = _sources[i];
                if (source == null)
                {
                    _leased.Remove(source);
                    _sources.RemoveAt(i);
                }
                else if (TryAcquire(source)) return source;
            }
            if (_sources.Count >= _maxSize) return null;
            var created = CreatePooledSource();
            _sources.Add(created);
            _leased.Add(created);
            return created;
        }

        public bool Contains(AudioSource source) => _sources.Contains(source);

        public bool TryAcquire(AudioSource source)
        {
            ThrowIfDisposed();
            if (source == null || !_sources.Contains(source) || _leased.Contains(source)) return false;
            ResetSource(source);
            source.gameObject.SetActive(true);
            _leased.Add(source);
            return true;
        }

        public void Release(AudioSource source)
        {
            if (!_leased.Remove(source)) return;
            if (source == null) return;
            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
        }

        // This dedicated source is caller-owned, not pooled.
        public AudioSource CreateAudioSource()
        {
            ThrowIfDisposed();
            return InstantiateSource(null);
        }

        private AudioSource CreatePooledSource()
        {
            var source = InstantiateSource(_parent);
            source.gameObject.name = $"{_prefab.name}_Pooled";
            return source;
        }

        private AudioSource InstantiateSource(Transform parent)
        {
            if (_prefab == null) throw new InvalidOperationException("The audio source template has been destroyed.");
            // Instantiate inactive so playOnAwake cannot start a template clip.
            var staging = new GameObject("SoundFlowSourceStaging");
            staging.SetActive(false);
            try
            {
                var source = UnityEngine.Object.Instantiate(_prefab, staging.transform);
                ResetSource(source);
                source.transform.SetParent(parent, false);
                source.gameObject.SetActive(true);
                return source;
            }
            finally { DestroyObject(staging); }
        }

        private void ResetSource(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.playOnAwake = false;
            source.enabled = true;
            source.volume = _prefab.volume;
            source.pitch = _prefab.pitch;
            source.loop = _prefab.loop;
            source.outputAudioMixerGroup = _prefab.outputAudioMixerGroup;
            source.spatialBlend = _prefab.spatialBlend;
            source.dopplerLevel = _prefab.dopplerLevel;
            source.spread = _prefab.spread;
            source.rolloffMode = _prefab.rolloffMode;
            source.minDistance = _prefab.minDistance;
            source.maxDistance = _prefab.maxDistance;
            source.mute = _prefab.mute;
            source.priority = _prefab.priority;
            source.panStereo = _prefab.panStereo;
            source.ignoreListenerPause = _prefab.ignoreListenerPause;
            source.ignoreListenerVolume = _prefab.ignoreListenerVolume;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(BaseAudioSourcePool));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var source in _sources)
                if (source != null)
                {
                    source.Stop();
                    source.gameObject.SetActive(false);
                    DestroyObject(source.gameObject);
                }
            _sources.Clear();
            _leased.Clear();
        }

        private static void DestroyObject(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
