using System;

public class BTAction : BTNode
{
    private Func<NodeState> action;

    public BTAction(Func<NodeState> action)
    {
        this.action = action;
    }

    public override NodeState Evaluate()
    {
        if (action == null)
        {
            state = NodeState.Failure;
            return state;
        }

        state = action();
        return state;
    }
}
