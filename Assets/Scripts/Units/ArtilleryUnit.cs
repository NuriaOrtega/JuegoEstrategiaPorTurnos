using UnityEngine;

public class ArtilleryUnit : Unit
{
    void Awake()
    {
        unitType = UnitType.Artillery;
    }

    public override float GetTerrainMovementModifier(TerrainType terrain)
    {
        switch (terrain)
        {
            case TerrainType.Llanura:
                return 1.0f;
            case TerrainType.Bosque:
                return 1.5f;
            case TerrainType.Montaña:
                return 2.0f;
            default:
                return 1.0f;
        }
    }
}
