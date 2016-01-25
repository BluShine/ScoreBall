using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class sceneswitcher : MonoBehaviour {

	public void switchScene(int scene)
    {
        SceneManager.LoadScene(scene);
    }

    public void quit()
    {
        Application.Quit();
    }
}
