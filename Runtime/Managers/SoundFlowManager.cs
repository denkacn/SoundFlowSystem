using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SoundFlowSystem.Data;
using SoundFlowSystem.Network;
using SoundFlowSystem.Pools;
using SoundFlowSystem.Rules.Factories;
using SoundFlowSystem.Settings;
using SoundFlowSystem.Tools;
using UnityEngine;

namespace SoundFlowSystem.Managers
{
    public class SoundFlowManager : ISoundFlowManager
    {
        private readonly List<IAudioSourcePool> _audioSourcePools = new List<IAudioSourcePool>();
        private readonly Dictionary<string, SoundData> _soundsLibrary = new Dictionary<string, SoundData>();
        private readonly bool _isInitialized = false;
        private readonly BaseNetworkAudioSynchronizer _networkAudioSynchronizer;
        private readonly IRulesFactory _rulesFactory;
        
        public SoundFlowManager(SoundFlowManagerSettings soundFlowManagerSettings)
        {
            _rulesFactory = new RulesFactory();
            
            _audioSourcePools.Add(new BaseAudioSourcePool(soundFlowManagerSettings.BaseAudioSource));

            _networkAudioSynchronizer = soundFlowManagerSettings.NetworkSynchronizer;
            _networkAudioSynchronizer.Init(this);
            
            foreach (var collection in soundFlowManagerSettings.SoundsCollections)
            {
                foreach (var soundData in collection.Get())
                {
                    if (!_soundsLibrary.TryAdd(soundData.Key, soundData))
                    {
                        Debug.LogError("[SoundFlowManager] Preparing Duplicate sound key names " + soundData.Key);
                    }
                }
            }

            _isInitialized = true;
        }

        public void Play(string soundKey, AudioSource audioSource = null, Action onFinished = null)
        {
            if (!_isInitialized) return;
            
            PlayIt(soundKey, Vector3.zero, audioSource, onFinished);
        }

        public void PlayInPosition(string soundKey, Vector3 position, AudioSource audioSource = null, Action onFinished = null)
        {
            if (!_isInitialized) return;
            
            PlayIt(soundKey, position, audioSource, onFinished);
        }

        public void PlayNetwork(string soundKey)
        {
            _networkAudioSynchronizer.PlayNetwork(soundKey, Vector3.zero);
        }

        public void PlayNetworkInPosition(string soundKey, Vector3 position)
        {
            _networkAudioSynchronizer.PlayNetwork(soundKey, position);
        }

        private void PlayIt(string soundKey, Vector3 position, AudioSource audioSource, Action onFinished)
        {
            var soundData = GetSoundData(soundKey);
            if (soundData == null)
            {
                Debug.LogError("[SoundFlowManager] PlayIt Not fount soundKey: " + soundKey);
                return;
            }

            if (soundData.Clips.Length == 0)
            {
                Debug.LogError("[SoundFlowManager] PlayIt soundData.Clips Is Empty");
                return;
            }

            if (!CheckPlayRules(soundData)) return;

            if (audioSource == null)
            {
                var pool = _audioSourcePools.Find(p => p.Id == soundData.PoolId);
                audioSource = pool.Get(); 
            }

            audioSource.transform.position = position;

            audioSource.clip = GetClip(soundData);

            PreparingAudioSource(audioSource, soundData);
            
            if(soundData.Delay == 0) 
                audioSource.Play();
            else 
                audioSource.PlayDelayed(soundData.Delay);

            if (onFinished != null) _ = ReturnToPoolAfterPlayback(audioSource, onFinished);
        }

        private void PreparingAudioSource(AudioSource audioSource, SoundData soundData)
        {
            audioSource.volume = soundData.Vloume;
            audioSource.loop = soundData.IsLoop;
            audioSource.outputAudioMixerGroup = soundData.Group;
            audioSource.spatialBlend = soundData.SpatialBlend;
            audioSource.minDistance = soundData.MinDistance;
            audioSource.maxDistance = soundData.MaxDistance;
        }

        private AudioClip GetClip(SoundData soundData)
        {
            return soundData.IsRandom ? soundData.Clips.PickRandom() : soundData.Clips[0];
        }

        private SoundData GetSoundData(string soundKey)
        {
            if (_soundsLibrary.TryGetValue(soundKey, out var soundData)) return soundData;

            Debug.LogError("[SoundFlowManager] GetSoundData Not Found " + soundKey);
            return null;
        }

        private bool CheckPlayRules(SoundData soundData)
        {
            foreach (var condition in soundData.Conditions)
            {
                var checker = _rulesFactory.Get(condition);
                if (!checker.Check(condition)) return false;
            }

            return true;
        }
        
        private async UniTask ReturnToPoolAfterPlayback(AudioSource audioSource, Action onFinished)
        {
            await UniTask.WaitWhile(() => audioSource.isPlaying);
            onFinished?.Invoke();
        }
    }
}