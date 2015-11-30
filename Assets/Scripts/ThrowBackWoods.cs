using UnityEngine;
using System.Collections;

public class ThrowBackWoods : MonoBehaviour {

    public Vector3 target = new Vector3(0, -100, 0); //target that sportsobjects will be launched towards
    public float throwVelocity = 20f;
    public float playerTackleTime = 2;

    //if a sports object enters, shoot it back towards 
    void OnTriggerEnter(Collider collision)
    {
        GameObject obj = collision.gameObject;
        TeamPlayer tPlayer = obj.GetComponent<TeamPlayer>();
        if(tPlayer != null)
        {
            tPlayer.tackle(throwVelocity * (target - tPlayer.transform.position).normalized, playerTackleTime);
            return;
        }
        SportsObject sportsObj = obj.GetComponent<SportsObject>();
        if(sportsObj != null)
        {
            sportsObj.body.velocity = (target - sportsObj.transform.position).normalized * throwVelocity;
        }
    }
}
