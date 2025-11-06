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
            gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (gameManager == null)
            return;

        int currentPlayer = gameManager.currentPlayerTurn;

        if (turnText != null)
        {
            turnText.text = $"Turno: Jugador {currentPlayer}";
        }

        if (resourcesText != null)
        {
            // TODO: Implementar sistema de recursos en GameManager
            resourcesText.text = "Recursos: 0";
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
