using UnityEngine;
using System.Collections;

public class SportsCat : SportsObject {

    public SportsFish fish;
    public float accelleration = 5;
    public float maxSpeed = 10;
    public bool onGround = false;

    Vector3 target = Vector3.forward;

	// Use this for initialization
	override public void Start () {
        base.Start();
        fish = GameObject.FindObjectOfType<SportsFish>();
	}
	
	// Update is called once per frame
	protected override void FixedUpdate () {
	    if(fish != null)
        {
            //sparodically target the fish because cats are silly like that.
            if (Random.value < .1f)
            {
                target = fish.transform.position - transform.position;
                target = new Vector3(target.x, 0, target.z).normalized;
            }
        } else
        {
            //finding objects takes a lot of CPU and is not very important, 
            //so we only look for fish 1 out of 200 frames. 
            //This is random, so that all cats won't syncrhonize and create lag spikes every 200 frames.
            if(Random.value < 0.005f)
            {
                fish = GameObject.FindObjectOfType<SportsFish>();
            }
            //sparodically change directions because cats are random
            if(Random.value < .1f) {
                target = (target + new Vector3(2 * Random.value - 1, 0, 2 * Random.value - 1)).normalized;
            }
        }

        if (isOnGround)
        {
            body.constraints = RigidbodyConstraints.FreezeRotation;
            transform.rotation = Quaternion.Euler(0, 0, 0);
            if (target.x < 0)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), 
                    transform.localScale.y, transform.localScale.z);
            } else
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), 
                    transform.localScale.y, transform.localScale.z);
            }

            body.AddForce(target * accelleration, ForceMode.Acceleration);

            if (new Vector2(body.velocity.x, body.velocity.z).magnitude > maxSpeed)
            {
                Vector2 hAdjusted = new Vector2(body.velocity.x, body.velocity.z).normalized * maxSpeed;
                body.velocity = new Vector3(hAdjusted.x, body.velocity.y, hAdjusted.y);
            }
        } else
        {
            body.constraints = RigidbodyConstraints.None;
            body.AddTorque(100 * new Vector3(Random.value - .5f, Random.value - .5f, Random.value - .5f), ForceMode.Acceleration);
        }
	}
}
