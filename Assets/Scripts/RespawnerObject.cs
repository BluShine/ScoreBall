using UnityEngine;
using System.Collections;

public class RespawnerObject : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnTriggerEnter(Collider collision)
    {
        SportsObject sportsObj = collision.gameObject.GetComponent<SportsObject>();
        if(sportsObj != null)
        {
            sportsObj.Respawn();
        }
    }
}
