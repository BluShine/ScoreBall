using UnityEngine;
using System.Collections;

public class CameraFacer : MonoBehaviour {

    Camera c;

	// Use this for initialization
	void Start () {
        //save the camera reference
        c = Camera.main;
	}
	
	// Update is called once per frame
	void Update () {
        //rotate ourself to face towards the camera.
        transform.rotation = c.transform.rotation;
	}
}
