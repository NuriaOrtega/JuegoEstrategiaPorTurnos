using UnityEngine;

public class CavalryUnit : Unit
{
    void Awake()
    {
        unitType = UnitType.Cavalry;
    }

    public override float GetTerrainMovementModifier(TerrainType terrain)
    {
        switch (terrain)
        {
            case TerrainType.Llanura:
                return 0.7f;
            case TerrainType.Bosque:
                return 1.5f;
            case TerrainType.Montaña:
                return 1.3f;
            default:
                return 1.0f;
        }
    }
}
