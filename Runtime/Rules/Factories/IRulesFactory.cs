using System;
using SoundFlowSystem.Rules.Checkers;
using SoundFlowSystem.Rules.Conditions;

namespace SoundFlowSystem.Rules.Factories
{
    public interface IRulesFactory
    {
        void Add(Type rulesType, IPlayConditionChecker checker);
        IPlayConditionChecker Get(IPlayCondition playCondition);
    }
}