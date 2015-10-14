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
    float stunnedTimer = 0;
    public float tackleDuration = 1f; //how long you stun opponents when you tackle them
    public float tacklePower = 10f; //forward velocity of people you tackle
    public float tackleLaunchPower = 3f; //upwards velocity of people you tackle
    static float TACKLESPINNINESS = 100; //how fast you spin when you're tackled
    //ball handling
    public float ballHoldDistance = 1;
    public float ballShootPower = 1000;
    public bool butterFingers = false; //if true, player will drop the ball if we run	face-first into a wall
	public bool dashWhileCarrying = false;
    public bool dashStopByWall = true;
    public bool dashStopByPlayer = true;

    //input
    public string xAxis = "Horizontal";
    public string yAxis = "Vertical";
    public string shootButton = "Fire";
	public string dashButton = "Fire";

    Rigidbody body;

	//game state
    Ball carriedBall;
	public int score = 0;
    Vector3 spawnPosition;
    public LayerMask BALLMASK;
	public TeamPlayer opponent;

	GameRules gameRules;

    public byte team;

	// Use this for initialization
	void Start () {
        spawnPosition = transform.position;
        body = GetComponent<Rigidbody>();
		gameRules = GameObject.Find("GameRules").GetComponent<GameRules>();
	}

    public void Respawn()
    {
        transform.position = spawnPosition;
    }

    public void ScorePoints(int points)
    {
        score += points;
        gameRules.updateScore();
    }
	
	// FixedUpdate is called at a fixed rate
	void FixedUpdate () {
        
        	
		//MOVEMENT--------------------------------------------------------------------
		//increment dash timers
		dashTimer -= Time.fixedDeltaTime;
		dashTimer = Mathf.Max(0, dashTimer);
		dashCooldownTimer -= Time.fixedDeltaTime;
		dashCooldownTimer = Mathf.Max(0, dashCooldownTimer);
        stunnedTimer -= Time.fixedDeltaTime;
        stunnedTimer = Mathf.Max(0, stunnedTimer);
        bool stunned = stunnedTimer > 0;
        if(!stunned)
        {
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }
		//dash input
		if(dashCooldownTimer == 0 && Input.GetButtonDown(dashButton) &&
			(dashWhileCarrying || carriedBall == null)) {
			dashTimer = dashDuration;
			dashCooldownTimer = dashCooldownDuration;
		}
        bool dashing = dashTimer > 0;
		//normal movement if we aren't dashing.
        if (stunned)
        {
            
        }
        else if (dashing)
        {
            //check if ball will hit wall and stop dash.
            if (carriedBall != null)
            {
                Ray fRay = new Ray(transform.position, transform.forward);
                float sDist = carriedBall.carryRadius + ballHoldDistance + 0.5f;
                if (Physics.SphereCast(fRay, .4f, sDist, BALLMASK))
                {
                    dashTimer = 0;
                }
            }

            //dash movement
            body.velocity = new Vector3(0, body.velocity.y, 0) + transform.forward * dashSpeed;
        }
        else {
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

        //BALL HANDLING---------------------------------------------------------
        if (carriedBall != null)
        {
            bool butterShot = false;
            //stop and move away from walls if there's not enough space to hold the ball
            Ray ballForwardRay = new Ray(transform.position, transform.forward);
            RaycastHit ballForwardCast;
            float rayDist = (ballHoldDistance + carriedBall.carryRadius) + .1f;
            //detect wall hit
            if (Physics.SphereCast(ballForwardRay, 0.4f, out ballForwardCast, rayDist, BALLMASK))
            {
                gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerHitObject, tp: this, col:ballForwardCast.collider));
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
                if(shootButton == dashButton)
                {
                    dashCooldownTimer = dashCooldownDuration;
                }
            }
        }
	}

    void OnCollisionStay(Collision collision)
    {
        if (checkBallCollision(collision))
        {
            //should probably send a message about this to the rules manager
        }
        else if (checkPlayerCollision(collision))
        {
            //also send the rules manager a message about this
        }
    }

    void tackle(Vector3 launch, float duration)
    {
        body.constraints = RigidbodyConstraints.None;
        body.velocity = launch;
        body.angularVelocity = new Vector3(Random.value, Random.value, Random.value).normalized * 
            TACKLESPINNINESS;
        stunnedTimer = duration;
    }

    bool checkBallCollision(Collision collision)
    {
        //check if the collision is a ball
        Ball collidedBall = collision.gameObject.GetComponent<Ball>();
        if (collidedBall != null)
        {
            if (collidedBall.grabBall(this))
            {
                carriedBall = collidedBall;
            }
            else
            {
                //we didn't dodge the ball!
                gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerHitInTheFaceByBall, tp:this, bl: collidedBall));
                tackle(collidedBall.getTackleVector(), collidedBall.tackleDuration);
            }
            return true;
        }
        return false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (checkBallCollision(collision))
        {
            
        }
        else if (checkPlayerCollision(collision))
        {

        }
        //handle collision with walls and objects
        else
        {
            gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerHitObject, tp: this, col:collision.collider));
            if (dashTimer > 0 && dashStopByWall)
            {
                dashTimer = 0;
            }
        }
    }

    bool checkPlayerCollision(Collision collision)
    {
        //check if the collision is a player
        TeamPlayer collidedPlayer = collision.gameObject.GetComponent<TeamPlayer>();
        if (collidedPlayer != null)
        {
            gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerHitPlayer, tp: this, vct: collidedPlayer));
            if (dashTimer > 0)
            {
                //steal the ball
                if (collidedPlayer.carriedBall != null)
                {
                    Ball stolenBall = collidedPlayer.carriedBall;
                    if (collidedPlayer.carriedBall.grabBall(this))
                    {
                        carriedBall = stolenBall;
                        gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerStealBall, tp: this, vct: collidedPlayer, bl:stolenBall));
                    }
                }
                //tackle them
                Vector3 tackleVector = transform.forward * tacklePower +
                    Vector3.up * tackleLaunchPower;
                collidedPlayer.tackle(tackleVector, tackleDuration);
                gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerTacklePlayer, tp: this, vct: collidedPlayer));
            }
            if (dashStopByPlayer)
            {
                dashTimer = 0;
            }
            return true;
        }
        return false;
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
