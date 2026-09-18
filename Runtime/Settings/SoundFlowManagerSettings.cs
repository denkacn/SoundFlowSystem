using System;
using System.Collections.Generic;
using SoundFlowSystem.Libraries;
using SoundFlowSystem.Network;
using SoundFlowSystem.Rules.Checkers;
using SoundFlowSystem.Rules.Conditions;
using UnityEngine;

namespace SoundFlowSystem.Settings
{
    public class SoundFlowManagerSettings : MonoBehaviour
    {
        public SoundsCollection[] SoundsCollections = Array.Empty<SoundsCollection>();
        public AudioSource BaseAudioSource;
        public BaseNetworkAudioSynchronizer NetworkSynchronizer;
        [Min(0)] public int InitialPoolSize = 5;
        [Min(1)] public int MaxPoolSize = 32;
        public Dictionary<Type, IPlayConditionChecker> Rules = new Dictionary<Type, IPlayConditionChecker>();

        public void AddConditionChecker(IPlayCondition playCondition, IPlayConditionChecker conditionChecker)
        {
            if (playCondition == null) throw new ArgumentNullException(nameof(playCondition));
            if (conditionChecker == null) throw new ArgumentNullException(nameof(conditionChecker));
            if (Rules == null) Rules = new Dictionary<Type, IPlayConditionChecker>();
            Rules[playCondition.GetType()] = conditionChecker;
        }
    }
}
