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
        private readonly Dictionary<string, PlayProcessData> _playProcesses = new Dictionary<string, PlayProcessData>();
        private readonly bool _isInitialized = false;
        private readonly BaseNetworkAudioSynchronizer _networkAudioSynchronizer;
        private readonly IRulesFactory _rulesFactory;
        
        public SoundFlowManager(SoundFlowManagerSettings soundFlowManagerSettings)
        {
            _rulesFactory = new RulesFactory();
            foreach (var pair in soundFlowManagerSettings.Rules)
            {
                _rulesFactory.Add(pair.Key, pair.Value);
            }
            
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

        public PlayProcessData Play(string soundKey, AudioSource audioSource = null, Action onFinished = null, bool isOverwriteSettings = true)
        {
            if (!_isInitialized) return null;
            
            return PlayIt(soundKey, Vector3.zero, audioSource, onFinished, isOverwriteSettings);
        }

        public PlayProcessData PlayInPosition(string soundKey, Vector3 position, AudioSource audioSource = null, Action onFinished = null, bool isOverwriteSettings = true)
        {
            if (!_isInitialized) return null;
            
            return PlayIt(soundKey, position, audioSource, onFinished, isOverwriteSettings);
        }

        public void Stop(string playProcessId)
        {
            if (_playProcesses.TryGetValue(playProcessId, out var playProcessData))
            {
                StopIt(playProcessData);
            }
        }

        public AudioSource Create()
        {
            var pool = _audioSourcePools.Find(p => p.Id == SoundFlowConstantsData.DefaultPoolId);
            return pool.CreateAudioSource();
        }
        
        public void PlayNetwork(string soundKey)
        {
            _networkAudioSynchronizer.PlayNetwork(soundKey, Vector3.zero);
        }

        public void PlayNetworkInPosition(string soundKey, Vector3 position)
        {
            _networkAudioSynchronizer.PlayNetwork(soundKey, position);
        }

        private PlayProcessData PlayIt(string soundKey, Vector3 position, AudioSource audioSource, Action onFinished, bool isOverwriteSettings)
        {
            var soundData = GetSoundData(soundKey);
            if (soundData == null)
            {
                Debug.LogError("[SoundFlowManager] PlayIt Not fount soundKey: " + soundKey);
                return null;
            }

            if (soundData.Clips.Length == 0)
            {
                Debug.LogError("[SoundFlowManager] PlayIt soundData.Clips Is Empty");
                return null;
            }

            if (!CheckPlayRules(soundData)) return null;

            if (audioSource == null)
            {
                var pool = _audioSourcePools.Find(p => p.Id == soundData.PoolId);
                audioSource = pool.Get(); 
            }

            audioSource.transform.position = position;
            
            audioSource.clip = GetClip(soundData);

            if (isOverwriteSettings) PreparingAudioSource(audioSource, soundData);
            
            if(soundData.Delay == 0) 
                audioSource.Play();
            else 
                audioSource.PlayDelayed(soundData.Delay);
            
            var playProcessData = AddToPlayProcesses(audioSource, soundData);
            
            if (onFinished != null) _ = ReturnToPoolAfterPlayback(audioSource, playProcessData.Id, onFinished);

            return playProcessData;
        }

        private void StopIt(PlayProcessData playProcessData)
        {
            playProcessData.AudioSource.Stop();
            _playProcesses.Remove(playProcessData.Id);
        }
        
        private void PreparingAudioSource(AudioSource audioSource, SoundData soundData)
        {
            audioSource.volume = soundData.Vloume;
            audioSource.loop = soundData.IsLoop;
            audioSource.outputAudioMixerGroup = soundData.Group;
            audioSource.spatialBlend = soundData.SpatialBlend;
            audioSource.dopplerLevel = soundData.DopplerLevel;
            audioSource.spread = soundData.Spread;
            audioSource.rolloffMode = soundData.RolloffMode;
            audioSource.minDistance = soundData.MinDistance;
            audioSource.maxDistance = soundData.MaxDistance;
        }

        private PlayProcessData AddToPlayProcesses(AudioSource audioSource, SoundData soundData)
        {
            var playProcessId = Guid.NewGuid().ToString();
            var playProcessData = new PlayProcessData(playProcessId, soundData, audioSource);
            
            _playProcesses.Add(playProcessId, playProcessData);

            return playProcessData;
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
        
        private async UniTask ReturnToPoolAfterPlayback(AudioSource audioSource, string playProcessId, Action onFinished)
        {
            await UniTask.WaitWhile(() => audioSource.isPlaying);

            Stop(playProcessId);
            
            onFinished?.Invoke();
        }
    }
}