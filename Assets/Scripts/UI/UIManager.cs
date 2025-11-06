using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("UI Elements")]
    [SerializeField] private Text turnText;
    [SerializeField] private Text resourcesText;

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
