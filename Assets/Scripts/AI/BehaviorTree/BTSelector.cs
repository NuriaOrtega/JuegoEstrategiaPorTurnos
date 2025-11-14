using System.Collections.Generic;

public class BTSelector : BTNode
{
    private List<BTNode> children = new List<BTNode>();

    public BTSelector(List<BTNode> children)
    {
        this.children = children;
    }

    public override NodeState Evaluate()
    {

        foreach (BTNode child in children)
        {
            NodeState childState = child.Evaluate();

            switch (childState)
            {
                case NodeState.Success:
                    state = NodeState.Success;
                    return state;

                case NodeState.Running:
                    state = NodeState.Running;
                    return state;

                case NodeState.Failure:
                    continue;
            }
        }
        state = NodeState.Failure;
        return state;
    }
}
