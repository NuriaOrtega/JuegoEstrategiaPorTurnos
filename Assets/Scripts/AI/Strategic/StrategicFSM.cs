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

 
    public void ForceInitialState()
    {
        states[currentState].OnEnter(context);
    }

    public void Update()
    {
        // Cada estado evalua sus propias transiciones
        StrategicStateType? newState = states[currentState].CheckTransitions(context);

        if (newState.HasValue && newState.Value != currentState)
        {
            Debug.Log($"[FSM] Transition: {currentState} -> {newState.Value}");
            states[currentState].OnExit(context);
            currentState = newState.Value;
            states[currentState].OnEnter(context);  // Aqui se asignan los pesos
        }
    }
}

public class StrategicContext
{
    public float NumericalAdvantage { get; set; }
    public float ResourceAdvantage { get; set; }
    public bool IsBaseThreatened { get; set; }
    public float TerritorialControl { get; set; }

    // Pesos estrategicos - asignados por OnEnter() de cada estado
    public float AggressionLevel { get; set; }
    public float EconomicFocus { get; set; }
}

public interface IStrategicState
{
    void OnEnter(StrategicContext context);
    void OnExit(StrategicContext context);
    StrategicStateType? CheckTransitions(StrategicContext context);
}

public class AggressiveState : IStrategicState
{
    public void OnEnter(StrategicContext context)
    {
        context.AggressionLevel = 0.8f;
        context.EconomicFocus = 0.1f;
        Debug.Log("[FSM] Entering AGGRESSIVE - Weights: Aggression=0.8, Economy=0.1");
    }

    public void OnExit(StrategicContext context)
    {
        Debug.Log("[FSM] Exiting AGGRESSIVE state");
    }

    public StrategicStateType? CheckTransitions(StrategicContext context)
    {
        // Salir de Aggressive si:
        if (context.IsBaseThreatened)
            return StrategicStateType.Defensive;

        if (context.NumericalAdvantage < 0.8f)
            return StrategicStateType.Balanced;

        if (context.ResourceAdvantage < 0.3f)
            return StrategicStateType.Economic;

        return null;  // Permanecer en este estado
    }
}

public class DefensiveState : IStrategicState
{
    public void OnEnter(StrategicContext context)
    {
        context.AggressionLevel = 0.2f;
        context.EconomicFocus = 0.3f;
        Debug.Log("[FSM] Entering DEFENSIVE - Weights: Aggression=0.2, Economy=0.3");
    }

    public void OnExit(StrategicContext context)
    {
        Debug.Log("[FSM] Exiting DEFENSIVE state");
    }

    public StrategicStateType? CheckTransitions(StrategicContext context)
    {
        // Salir de Defensive si:
        if (!context.IsBaseThreatened && context.NumericalAdvantage > 1.5f)
            return StrategicStateType.Aggressive;

        if (!context.IsBaseThreatened && context.NumericalAdvantage > 1.0f)
            return StrategicStateType.Balanced;

        return null;
    }
}

public class EconomicState : IStrategicState
{
    public void OnEnter(StrategicContext context)
    {
        context.AggressionLevel = 0.3f;
        context.EconomicFocus = 0.6f;
        Debug.Log("[FSM] Entering ECONOMIC - Weights: Aggression=0.3, Economy=0.6");
    }

    public void OnExit(StrategicContext context)
    {
        Debug.Log("[FSM] Exiting ECONOMIC state");
    }

    public StrategicStateType? CheckTransitions(StrategicContext context)
    {
        // Salir de Economic si:
        if (context.IsBaseThreatened)
            return StrategicStateType.Defensive;

        if (context.ResourceAdvantage > 1.5f && context.NumericalAdvantage > 1.3f)
            return StrategicStateType.Aggressive;

        if (context.ResourceAdvantage > 1.0f)
            return StrategicStateType.Balanced;

        return null;
    }
}

public class BalancedState : IStrategicState
{
    public void OnEnter(StrategicContext context)
    {
        context.AggressionLevel = 0.5f;
        context.EconomicFocus = 0.3f;
        Debug.Log("[FSM] Entering BALANCED - Weights: Aggression=0.5, Economy=0.3");
    }

    public void OnExit(StrategicContext context)
    {
        Debug.Log("[FSM] Exiting BALANCED state");
    }

    public StrategicStateType? CheckTransitions(StrategicContext context)
    {
        // Transiciones desde Balanced:
        if (context.IsBaseThreatened)
            return StrategicStateType.Defensive;

        if (context.NumericalAdvantage > 1.8f && context.TerritorialControl > 0.5f)
            return StrategicStateType.Aggressive;

        if (context.NumericalAdvantage < 0.5f)
            return StrategicStateType.Defensive;

        if (context.ResourceAdvantage < 0.5f && context.NumericalAdvantage > 0.8f)
            return StrategicStateType.Economic;

        if (context.NumericalAdvantage > 1.3f)
            return StrategicStateType.Aggressive;

        return null;
    }
}
