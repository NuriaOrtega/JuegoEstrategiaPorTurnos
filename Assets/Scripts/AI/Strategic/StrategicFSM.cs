using UnityEngine;
using System.Collections.Generic;

public class StrategicFSM
{
    private StrategicStateType currentState;
    private Dictionary<StrategicStateType, IStrategicState> states;
    private StrategicContext context;

    public StrategicStateType CurrentState => currentState;

    public StrategicFSM(StrategicContext context)
    {
        this.context = context;
        states = new Dictionary<StrategicStateType, IStrategicState>
        {
            { StrategicStateType.Aggressive, new AggressiveState() },
            { StrategicStateType.Defensive, new DefensiveState() },
            { StrategicStateType.Economic, new EconomicState() },
            { StrategicStateType.Balanced, new BalancedState() }
        };
        currentState = StrategicStateType.Balanced;
    }

    public void Update()
    {
        StrategicStateType newState = EvaluateTransitions();
        if (newState != currentState)
        {
            Debug.Log($"[FSM] State transition: {currentState} -> {newState}");
            states[currentState].OnExit(context);
            currentState = newState;
            states[currentState].OnEnter(context);
        }
    }

    public void Execute()
    {
        states[currentState].Execute(context);
    }

    public (float aggression, float economy) GetCurrentWeights()
    {
        return states[currentState].GetWeights();
    }

    private StrategicStateType EvaluateTransitions()
    {
        float numericalAdvantage = context.NumericalAdvantage;
        float resourceAdvantage = context.ResourceAdvantage;
        bool baseThreatened = context.IsBaseThreatened;
        float territorialControl = context.TerritorialControl;

        if (baseThreatened)
        {
            return StrategicStateType.Defensive;
        }

        if (numericalAdvantage > 1.8f && territorialControl > 0.5f)
        {
            return StrategicStateType.Aggressive;
        }

        if (numericalAdvantage < 0.5f)
        {
            return StrategicStateType.Defensive;
        }

        if (resourceAdvantage < 0.5f && numericalAdvantage > 0.8f)
        {
            return StrategicStateType.Economic;
        }

        if (numericalAdvantage > 1.3f)
        {
            return StrategicStateType.Aggressive;
        }

        return StrategicStateType.Balanced;
    }
}

public class StrategicContext
{
    public float NumericalAdvantage { get; set; }
    public float ResourceAdvantage { get; set; }
    public bool IsBaseThreatened { get; set; }
    public float TerritorialControl { get; set; }
    public int FriendlyUnitCount { get; set; }
    public int EnemyUnitCount { get; set; }
    public int Resources { get; set; }
    public List<Unit> FriendlyUnits { get; set; }
    public List<Unit> EnemyUnits { get; set; }
}

public interface IStrategicState
{
    void OnEnter(StrategicContext context);
    void Execute(StrategicContext context);
    void OnExit(StrategicContext context);
    (float aggression, float economy) GetWeights();
}

public class AggressiveState : IStrategicState
{
    public void OnEnter(StrategicContext context)
    {
        Debug.Log("[FSM] Entering AGGRESSIVE state - Focus on attack!");
    }

    public void Execute(StrategicContext context)
    {
    }

    public void OnExit(StrategicContext context)
    {
    }

    public (float aggression, float economy) GetWeights()
    {
        return (0.8f, 0.1f);
    }
}

public class DefensiveState : IStrategicState
{
    public void OnEnter(StrategicContext context)
    {
        Debug.Log("[FSM] Entering DEFENSIVE state - Protect the base!");
    }

    public void Execute(StrategicContext context)
    {
    }

    public void OnExit(StrategicContext context)
    {
    }

    public (float aggression, float economy) GetWeights()
    {
        return (0.2f, 0.3f);
    }
}

public class EconomicState : IStrategicState
{
    public void OnEnter(StrategicContext context)
    {
        Debug.Log("[FSM] Entering ECONOMIC state - Gather resources!");
    }

    public void Execute(StrategicContext context)
    {
    }

    public void OnExit(StrategicContext context)
    {
    }

    public (float aggression, float economy) GetWeights()
    {
        return (0.3f, 0.6f);
    }
}

public class BalancedState : IStrategicState
{
    public void OnEnter(StrategicContext context)
    {
        Debug.Log("[FSM] Entering BALANCED state - Maintain equilibrium");
    }

    public void Execute(StrategicContext context)
    {
    }

    public void OnExit(StrategicContext context)
    {
    }

    public (float aggression, float economy) GetWeights()
    {
        return (0.5f, 0.3f);
    }
}
