using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public void StartGameScene(){
            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }

    public void StartReglasScene(){
            UnityEngine.SceneManagement.SceneManager.LoadScene("ReglasScene");
    }

    public void StartCreditosScene(){
            UnityEngine.SceneManagement.SceneManager.LoadScene("CreditosScene");
    }

    public void StartMenuScene(){
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
