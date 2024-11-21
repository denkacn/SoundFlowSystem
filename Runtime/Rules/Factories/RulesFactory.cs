using System;
using System.Collections.Generic;
using SoundFlowSystem.Rules.Checkers;
using SoundFlowSystem.Rules.Conditions;

namespace SoundFlowSystem.Rules.Factories
{
    public class RulesFactory : IRulesFactory
    {
        private readonly Dictionary<Type, IPlayConditionChecker> _rules = new Dictionary<Type, IPlayConditionChecker>();

        public void Add(Type rulesType, IPlayConditionChecker checker)
        {
            _rules.Add(rulesType, checker);
        }
        
        public IPlayConditionChecker Get(IPlayCondition playCondition)
        {
            var cType = playCondition.GetType();
            return _rules.GetValueOrDefault(cType);
        }
    }
}