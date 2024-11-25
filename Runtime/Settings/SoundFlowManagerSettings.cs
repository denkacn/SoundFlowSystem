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
        public SoundsCollection[] SoundsCollections;
        public AudioSource BaseAudioSource;
        public BaseNetworkAudioSynchronizer NetworkSynchronizer;
        public Dictionary<Type, IPlayConditionChecker> Rules = new Dictionary<Type, IPlayConditionChecker>();

        public void AddConditionChecker(IPlayCondition playCondition, IPlayConditionChecker conditionChecker)
        {
            Rules.Add(playCondition.GetType(), conditionChecker);
        }
    }
}