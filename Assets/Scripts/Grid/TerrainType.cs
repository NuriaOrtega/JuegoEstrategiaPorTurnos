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
}
