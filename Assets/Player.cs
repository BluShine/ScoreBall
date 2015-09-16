using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

    static float moveVel = 200;

    Rigidbody body;

	// Use this for initialization
	void Start () {
        body = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
        body.AddForce(new Vector3(
            Input.GetAxis("Horizontal") * moveVel * Time.deltaTime,
            0, Input.GetAxis("Vertical") * moveVel * Time.deltaTime), 
            ForceMode.Acceleration);
	}
}
