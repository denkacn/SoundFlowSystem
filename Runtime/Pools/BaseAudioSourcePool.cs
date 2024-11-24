using System.Collections.Generic;
using System.Linq;
using SoundFlowSystem.Data;
using UnityEngine;

namespace SoundFlowSystem.Pools
{
    public class BaseAudioSourcePool : IAudioSourcePool
    {
        private const int InitialSize = 5;
        
        private readonly AudioSource _audioSourcePrefab;
        private readonly List<AudioSource> _audioSources = new List<AudioSource>();

        public string Id => SoundFlowConstantsData.DefaultPoolId;

        public BaseAudioSourcePool(AudioSource audioSourcePrefab)
        {
            _audioSourcePrefab = audioSourcePrefab;

            for (var i = 0; i < InitialSize; i++) CreateAudioSourceInPool();
        }

        public AudioSource Get()
        {
            var availableSource = _audioSources.FirstOrDefault(source => !source.isPlaying);
            return availableSource ?? CreateAudioSourceInPool();
        }
        
        public AudioSource CreateAudioSource()
        {
            var audioSource = GameObject.Instantiate(_audioSourcePrefab, null);
            audioSource.gameObject.name = $"{_audioSourcePrefab.name}_Pooled";
            
            return audioSource;
        }

        private AudioSource CreateAudioSourceInPool()
        {
            var audioSource = CreateAudioSource();
            
            _audioSources.Add(audioSource);

            return audioSource;
        }
        
        
    }
}