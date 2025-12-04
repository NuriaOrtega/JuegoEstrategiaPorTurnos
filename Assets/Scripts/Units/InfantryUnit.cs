using UnityEngine;

public class InfantryUnit : Unit
{
    void Awake()
    {
        unitType = UnitType.Infantry;
    }

    public override float GetTerrainMovementModifier(TerrainType terrain)
    {
        switch (terrain)
        {
            case TerrainType.Bosque:
                return 0.8f;
            case TerrainType.Montaña:
                return 1.2f;
            default:
                return 1.0f;
        }
    }
}
