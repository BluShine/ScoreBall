using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

    static float moveVel = 5;

    Rigidbody body;
    Networking net;

	// Use this for initialization
	void Start () {
        body = GetComponent<Rigidbody>();
        net = FindObjectOfType<Networking>();
        net.playerPos = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0)
        {
            transform.position = transform.position + new Vector3(
                Input.GetAxis("Horizontal") * moveVel * Time.deltaTime,
                0, Input.GetAxis("Vertical") * moveVel * Time.deltaTime);
            net.SendMove(transform.position);
        }
	}
}
