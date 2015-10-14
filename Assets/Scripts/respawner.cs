using UnityEngine;
using System.Collections;

public class respawner : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnTriggerEnter(Collider collision)
    {
        TeamPlayer player = collision.gameObject.GetComponent<TeamPlayer>();
        if(player != null)
        {
            player.Respawn();
        }
        Ball ball = collision.gameObject.GetComponent<Ball>();
        if(ball != null)
        {
            ball.Respawn();
        }
    }
}
