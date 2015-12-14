using UnityEngine;
using System.Collections;

public class RestartButton : MonoBehaviour {

    public string button;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	    if(Input.GetButtonDown(button))
        {
            Application.LoadLevel(Application.loadedLevel);
        }
	}
}
