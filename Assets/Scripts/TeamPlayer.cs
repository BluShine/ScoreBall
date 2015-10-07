using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TeamPlayer : MonoBehaviour {

    //physics
    public float moveSpeed = 500; //movement speed in pixels/sec
    public float strafeSpeed = 500; //"strafing" speed in pixels/sec
	public float dashSpeed = 1000; //dashing speed in pixels/sec (not additive with move/strafe)
	public float dashDuration = .7f; //duration of dash in seconds
	float dashTimer = 0;
	public float dashCooldownDuration = 2; //cooldown time between dashes in seconds
	float dashCooldownTimer = 0;
    public float turnSpeed = 720; //rotation speed in pixels/sec
    [Range(0.01f, 0.99f)]
    public float strafeThreashold = .6f; //input threashold for strafing only. 
    [Range(0.01f, 0.99f)]
    public float turnThreashold = .3f; //input threashold for turning only.
    //ball handling
    public float ballHoldDistance = 1;
    public float ballShootPower = 1000;
    public bool butterFingers = false; //if true, player will drop the ball if we run	face-first into a wall
	public bool dashWhileCarrying = false;

    //input
    public string xAxis = "Horizontal";
    public string yAxis = "Vertical";
    public string shootButton = "Fire";
	public string dashButton = "Fire";

    Rigidbody body;

	//game state
    Ball carriedBall;
	public int score = 0;
	public GameObject scoreDisplay;

    public LayerMask BALLMASK;

	GameRules gameRules;

	// Use this for initialization
	void Start () {
        body = GetComponent<Rigidbody>();
		gameRules = GameObject.Find("GameRules").GetComponent<GameRules>();
	}
	
	// FixedUpdate is called at a fixed rate
	void FixedUpdate () {
        	
		//MOVEMENT--------------------------------------------------------------------
		//increment dash timers
		dashTimer -= Time.fixedDeltaTime;
		dashTimer = Mathf.Max(0, dashTimer);
		dashCooldownTimer -= Time.fixedDeltaTime;
		dashCooldownTimer = Mathf.Max(0, dashCooldownTimer);
		//dash input
		if(dashCooldownTimer == 0 && Input.GetButtonDown(dashButton) &&
			(dashWhileCarrying || carriedBall == null)) {
			dashTimer = dashDuration;
			dashCooldownTimer = dashCooldownDuration;
		}
        bool dashing = dashTimer > 0;
		//normal movement if we aren't dashing.
		if(!dashing) {
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
				movingVel += Mathf.Min(1, inputMag / strafeThreashold) * strafeSpeed * new Vector3(input.x, 0, input.y);
			}
			//running
			if (inputMag > strafeThreashold)
			{
				Vector3 playerRot = transform.forward;
				movingVel += inputMag * moveSpeed * new Vector3(playerRot.x, 0, playerRot.z);
			}
			//rotate our velocity if we're sliding along a wall.
			Ray fRay = new Ray(transform.position, transform.forward);
			RaycastHit fRHit;
			//spherecast further if we're carrying a ball
			float sDist = .5f;
			if (carriedBall != null)
			{
				sDist += carriedBall.carryRadius + ballHoldDistance;
			}
			//Debug.DrawLine(transform.position, transform.position + transform.forward * sDist);
			//spherecast forwards
			if (Physics.SphereCast(fRay, .4f, out fRHit, sDist, BALLMASK))
			{
				Vector3 tDir = Vector3.Cross(fRHit.normal, transform.up);
				if (Vector3.Dot(tDir, movingVel) > .5f)
				{
					movingVel = movingVel.magnitude * tDir;
				}
				else if (Vector3.Dot(-tDir, movingVel) > .5f)
				{
					movingVel = movingVel.magnitude * -tDir;
				}
			}
			//Debug.DrawLine(transform.position, transform.position + body.velocity);
			body.velocity = movingVel;
		}
		else {
			//dash movement
			body.velocity = new Vector3(0, body.velocity.y, 0) + transform.forward * dashSpeed;
		}

        //BALL HANDLING---------------------------------------------------------
        if (carriedBall != null)
        {
            bool butterShot = false;
            //stop and move away from walls if there's not enough space to hold the ball
            Ray ballForwardRay = new Ray(transform.position, transform.forward);
            RaycastHit ballForwardCast;
            float rayDist = (ballHoldDistance + carriedBall.carryRadius) + .1f;
            Debug.DrawLine(transform.position, transform.position + transform.forward * rayDist);
            if (Physics.SphereCast(ballForwardRay, 0.4f, out ballForwardCast, rayDist, BALLMASK))
            {
                //stop
                body.velocity = Vector3.zero;
				//stop dash
				dashTimer = 0;
                //calculate the distance to move away from the wall
                Debug.Log(Vector3.Dot(transform.forward, ballForwardCast.normal));
                transform.position += ballForwardCast.normal * 
                    (Vector3.Dot(transform.forward, ballForwardCast.normal) *
                    (ballForwardCast.distance - rayDist)) * .5f;
                if (butterFingers)
                {
                    butterShot = true;
                }
            }

            //move the ball with us
            carriedBall.transform.position = transform.position + transform.forward * ballHoldDistance;
            //make sure that the ball is not getting stuck in the ground
            RaycastHit ballDownCast;
            Ray ballDownRay = new Ray(carriedBall.transform.position, Vector3.down);
            if (Physics.Raycast(ballDownRay, out ballDownCast, ballHoldDistance * 2 + carriedBall.carryRadius * 2, BALLMASK))
            {
                carriedBall.transform.position = ballDownCast.point + new Vector3(0, carriedBall.carryRadius, 0);
            }
            //give it equal velocity to our velocity
            carriedBall.GetComponent<Rigidbody>().velocity = body.GetPointVelocity(carriedBall.transform.position);
            //shoot it
            if (butterShot)
            {
                carriedBall.shoot((transform.forward + transform.up * .5f).normalized * ballShootPower * .5f);
            }
            else if (Input.GetButtonDown(shootButton))
            {
                carriedBall.shoot(transform.forward * ballShootPower);
				gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.BallShot, this));
            }
        }
		scoreDisplay.GetComponent<Text>().text = score.ToString();
	}

    void OnCollisionEnter(Collision collision)
    {
		//handle collision with balls
        Ball collidedBall = collision.gameObject.GetComponent<Ball>();
        if (collidedBall != null)
        {
            if (collidedBall.grabBall(this))
            {
				gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.BallGrabbed, this));
                carriedBall = collidedBall;
            }
        }
		//handle collision with players
		
    }

    public void removeBall(Ball rBall)
    {
        if (carriedBall == rBall)
        {
            carriedBall.transform.SetParent(null);
            carriedBall = null;
        }
    }
}
