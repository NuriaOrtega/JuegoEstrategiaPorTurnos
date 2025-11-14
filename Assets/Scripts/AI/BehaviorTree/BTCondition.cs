using System;

public class BTCondition : BTNode
{
    private Func<bool> condition;

    public BTCondition(Func<bool> condition)
    {
        this.condition = condition;
    }

    public override NodeState Evaluate()
    {
        if (condition == null)
        {
            state = NodeState.Failure;
            return state;
        }

        state = condition() ? NodeState.Success : NodeState.Failure;
        return state;
    }
}
