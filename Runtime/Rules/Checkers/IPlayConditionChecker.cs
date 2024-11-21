using SoundFlowSystem.Rules.Conditions;

namespace SoundFlowSystem.Rules.Checkers
{
    public interface IPlayConditionChecker
    {
        bool Check(IPlayCondition condition);
    }
}