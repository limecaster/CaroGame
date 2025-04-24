using UnityEngine;
using UnityEngine.UI;

public class AIMenuManager: PhotonSingleton<AIMenuManager>
{

    // In AIMenuManager.cs
    public void LoadEasyAI()
    {
        PlayerPrefs.SetString("AILevel", "Easy");
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }
    public void LoadMediumAI()
    {
        PlayerPrefs.SetString("AILevel", "Medium");
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }
    public void LoadHardAI()
    {
        PlayerPrefs.SetString("AILevel", "Hard");
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }
}
