using UnityEngine;
using System.Collections;

public class Ball : MonoBehaviour {

    //handling
    public float noTakebacksCooldown = .2f; //time in seconds

    //private
    Rigidbody body;
    TeamPlayer currentPlayer;
    TeamPlayer previousPlayer;
    bool isHeld;

	// Use this for initialization
	void Start () {
        body = GetComponent<Rigidbody>();
	}
	
	// FixedUpdate is called at a fixed rate
	void FixedUpdate () {
        
	}
}
