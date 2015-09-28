using UnityEngine;
using System.Collections;

public class TeamPlayer : MonoBehaviour {

    public float moveSpeed = 10; //movement speed in pixels/sec
    public float strafeSpeed = 5; //"strafing" speed in pixels/sec
    public float turnSpeed = 720; //rotation speed in pixels/sec

    [Range(0.01f, 0.99f)]
    public float strafeThreashold = .6f; //input threashold for strafing only. 
    [Range(0.01f, 0.99f)]
    public float turnThreashold = .3f; //input threashold for turning only.

    public string xAxis = "Horizontal";
    public string yAxis = "Vertical";

    Rigidbody body;

	// Use this for initialization
	void Start () {
        body = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        //get input vector from joystick/keys
        Vector2 input = new Vector2(Input.GetAxis(xAxis), Input.GetAxis(yAxis));
        if (input.magnitude > 1)
        {
            input.Normalize();
        }
        float inputMag = input.magnitude;

        //rotate towards joystick
        if (inputMag != 0)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.Euler(0, 90 -Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg, 0),
                Mathf.Min(1, inputMag / turnThreashold) * turnSpeed * Time.fixedDeltaTime);
        }
        //movement
        Vector3 movingVel = new Vector3(0, body.velocity.y, 0);
        //strafing
        if (inputMag > turnThreashold)
        {
            movingVel += Mathf.Min(1, inputMag / strafeThreashold) * strafeSpeed * Time.fixedDeltaTime * new Vector3(input.x, 0, input.y);
        }
        //running
        if (inputMag > strafeThreashold)
        {
            Vector3 playerRot = transform.forward;
            movingVel += inputMag * Time.fixedDeltaTime * moveSpeed * new Vector3(playerRot.x, 0, playerRot.z);
            Debug.Log(playerRot);
        }
        body.velocity = movingVel;
	}
}
