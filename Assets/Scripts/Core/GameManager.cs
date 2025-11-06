using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Estado del Juego")]
    public int currentPlayerTurn = 0; // 0 = Jugador, 1 = IA

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnCellSelected(HexCell cell)
    {
        if (cell == null) return;

        Debug.Log($"Celda seleccionada en posición: {cell.gridPosition}");

        // TODO: Implementar lógica de selección de celda
        // - Comprobar si la celda tiene una unidad
        // - Manejar selección de unidad
        // - Manejar comandos de movimiento/ataque
    }

  
    public void EndTurn()
    {
        currentPlayerTurn = (currentPlayerTurn + 1) % 2;
        Debug.Log($"Turno terminado. Ahora es el turno del Jugador {currentPlayerTurn}.");

        // TODO: Implementar lógica de fin de turno
        // - Resetear flags de movimiento/ataque de unidades
        // - Otorgar recursos
        // - Comprobar condiciones de victoria
        // - Ejecutar turno de IA si currentPlayerTurn == 1
    }
}
