using System;
using Sirenix.OdinInspector;
using SoundFlowSystem.Rules.Conditions;
using UnityEngine;
using UnityEngine.Audio;

namespace SoundFlowSystem.Data
{
    [Serializable]
    public class SoundData
    {
        public string Key;
        [FoldoutGroup("Settings")] public string PoolId = SoundFlowConstantsData.DefaultPoolId;
        
        [FoldoutGroup("Settings")] public AudioClip[] Clips;
        
        [FoldoutGroup("Settings")] public bool IsLoop = false;
        [FoldoutGroup("Settings")] public bool IsRandom = false;
        [FoldoutGroup("Settings")] public float Delay = 0;
        
        [FoldoutGroup("Settings")] [Range(0f, 1f)] public float Vloume = 1f;
        [FoldoutGroup("Settings")] [Range(-3f, 3f)] public float Pitch = 1f;
        
        [FoldoutGroup("Settings")] public AudioMixerGroup Group;
        
        [FoldoutGroup("Settings")] [Range(0f, 1f)] public float SpatialBlend = 1f;
        
        [ShowIf("@this.SpatialBlend > 0")]
        [FoldoutGroup("Settings")] public float MinDistance = 1f;
        [ShowIf("@this.SpatialBlend > 0")]
        [FoldoutGroup("Settings")] public float MaxDistance = 5f;

        [SerializeReference] public IPlayCondition[] Conditions;
    }
}