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
            if (rulesType == null) throw new ArgumentNullException(nameof(rulesType));
            if (!typeof(IPlayCondition).IsAssignableFrom(rulesType))
                throw new ArgumentException("The type must implement IPlayCondition.", nameof(rulesType));
            if (checker == null) throw new ArgumentNullException(nameof(checker));
            _rules[rulesType] = checker;
        }
        
        public IPlayConditionChecker Get(IPlayCondition playCondition)
        {
            if (playCondition == null) return null;
            return _rules.TryGetValue(playCondition.GetType(), out var checker) ? checker : null;
        }
    }
}
