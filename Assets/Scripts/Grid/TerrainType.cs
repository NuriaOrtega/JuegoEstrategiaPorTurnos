using UnityEngine;

public enum TerrainType
{
    Llanura,
    Bosque,
    Montaña,
    Agua
}

public static class TerrainTypeExtensions
{
    public static float GetMovementCost(this TerrainType terreno)
    {
        return terreno switch
        {
            TerrainType.Llanura => 1.0f,
            TerrainType.Bosque => 2.0f,
            TerrainType.Montaña => 3.0f,
            TerrainType.Agua => 999f, //Impasable
            _ => 1.0f
        };
    }

    public static Color GetTerrainColor(this TerrainType terrain)
    {
        return terrain switch
        {
            TerrainType.Llanura => new Color(0.8f, 0.9f, 0.6f),
            TerrainType.Bosque => new Color(0.2f, 0.6f, 0.2f),
            TerrainType.Montaña => new Color(0.5f, 0.5f, 0.5f),
            TerrainType.Agua => new Color(0.3f, 0.5f, 0.9f),
            _ => Color.white
        };
    }
}
