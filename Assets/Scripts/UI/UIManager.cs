using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI resourcesText;

    void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("UIManager: No se encontró GameManager en la escena!");
            }
        }

        if (turnText == null)
            Debug.LogWarning("UIManager: turnText no está asignado!");

        if (resourcesText == null)
            Debug.LogWarning("UIManager: resourcesText no está asignado!");

        Debug.Log("UIManager iniciado correctamente");
    }

    void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("UIManager: gameManager es null, no se puede actualizar UI");
            return;
        }

        int currentPlayer = gameManager.currentPlayerTurn;

        if (turnText != null)
        {
            string newText = $"Turno: Jugador {currentPlayer}";
            if (turnText.text != newText)
            {
                turnText.text = newText;
                Debug.Log($"UIManager: Texto de turno actualizado a '{newText}'");
            }
        }

        if (resourcesText != null)
        {
            string newText = "Recursos: 0";
            if (resourcesText.text != newText)
            {
                resourcesText.text = newText;
                Debug.Log($"UIManager: Texto de recursos actualizado a '{newText}'");
            }
        }
    }

    public void ProduceInfantry()
    {
        
    }

    public void ProduceCavalry()
    {
       
    }

public void ProduceArtillery()
    {
       
    }

}
