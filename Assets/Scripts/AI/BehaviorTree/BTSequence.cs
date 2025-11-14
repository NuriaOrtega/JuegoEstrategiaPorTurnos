using System.Collections.Generic;

public class BTSequence : BTNode
{
    private List<BTNode> children = new List<BTNode>();

    public BTSequence(List<BTNode> children)
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
                case NodeState.Failure:
                    state = NodeState.Failure;
                    return state;

                case NodeState.Running:
                    state = NodeState.Running;
                    return state;

                case NodeState.Success:
                    continue;
            }
        }
        state = NodeState.Success;
        return state;
    }
}
