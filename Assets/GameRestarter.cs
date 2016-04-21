using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameRestarter : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	    if(gameObject.activeSelf)
        {
            if(Input.GetButton("Submit"))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
	}
}
