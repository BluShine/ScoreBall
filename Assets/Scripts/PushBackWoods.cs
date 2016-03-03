using UnityEngine;
using System.Collections;

public class PushBackWoods : MonoBehaviour {

    public Vector3 target = new Vector3(0, -100, 0); //target that sportsobjects will be launched towards
    public float pushVelocity = 20f;

    //if a sports object enters, shoot it back towards 
    void OnTriggerEnter(Collider collision)
    {
        GameObject obj = collision.gameObject;
        TeamPlayer tPlayer = obj.GetComponent<TeamPlayer>();
        if(tPlayer != null)
        {
            return;
        }
        SportsObject sportsObj = obj.GetComponent<SportsObject>();
        if(sportsObj != null)
        {
            sportsObj.body.AddForce((target - sportsObj.transform.position).normalized * pushVelocity, ForceMode.Acceleration);
        }
    }

    void OnTriggerStay(Collider collision)
    {
        GameObject obj = collision.gameObject;
        TeamPlayer tPlayer = obj.GetComponent<TeamPlayer>();
        if (tPlayer != null)
        {
            return;
        }
        SportsObject sportsObj = obj.GetComponent<SportsObject>();
        if (sportsObj != null)
        {
            sportsObj.body.AddForce((sportsObj.transform.position - target).normalized * pushVelocity, ForceMode.Acceleration);
        }
    }
}
