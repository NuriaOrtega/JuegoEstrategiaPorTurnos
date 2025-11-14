
public enum NodeState
{
    Success,  
    Failure,  
    Running   
}


public abstract class BTNode
{
    protected NodeState state;

    public NodeState State => state;

    public abstract NodeState Evaluate();
}
