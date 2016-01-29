using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class TeamPlayer : SportsObject {

    //physics
    public float moveSpeed = 500; //movement speed in pixels/sec
    public float moveAccel = 1; //1 = reach full speed in 1 second. 10 = reach full speed in 1/10th of a second. Applies to move, dash, and strafe.
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
    public float tacklePower = 7f; //forward velocity of people you tackle
    public float tackleLaunchPower = 40f; //upwards velocity of people you tackle
    static float TACKLESPINNINESS = 150; //how fast you spin when you're tackled
    static float DIZZYSPINSPEED = .3262f;//rotations per sec
    //ball handling
    public float ballHoldDistance = 1;
    public float ballShootPower = 1000;
    public float ballLobPower = 1000;
    public bool butterFingers = false; //if true, player will drop the ball if we run	face-first into a wall
	public bool dashWhileCarrying = false;
    public bool jumpWhileCarrying = false;
    public bool dashStopByWall = true;
    public bool dashStopByPlayer = true;

    //input
    public string xAxis = "Horizontal";
    public string yAxis = "Vertical";
    public string shootButton = "Fire";
	public string dashButton = "Fire";
    public string hopButton = "Fire";
    public string lobButton = "Fire";

    bool shootPressed = false;
    bool dashPressed = false;
    bool hopPressed = false;
    bool lobPressed = false;

    //sound
    public List<AudioClip> tackleSounds;
    public List<AudioClip> dashSounds;
    public List<AudioClip> shootSounds;
    public List<AudioClip> lobSounds;
    public List<AudioClip> jumpSounds;
    public List<AudioClip> pointGainSounds;

    //game state
    Ball carriedBall;
	public int score = 0;
    public LayerMask BALLMASK;
	public TeamPlayer opponent;
    PlayerAnimation playerAnim;
    TrailRenderer line;
    ParticleSystem particles;

	// Use this for initialization
	new void Start () {
        base.Start();
        useDefaultFreezing(false);
        if(gameRules != null)
		    gameRules.RegisterPlayer(this);
        line = GetComponent<TrailRenderer>();
        particles = GetComponent<ParticleSystem>();
        playerAnim = GetComponentInChildren<PlayerAnimation>();
    }

    public void ScorePoints(int points)
    {
        score += points;
        gameRules.UpdateScore();
        switch(points)
        {
            case 1:
                soundSource.clip = pointGainSounds[0];
                break;
            case 2:
                soundSource.clip = pointGainSounds[1];
                break;
            case 3:
                soundSource.clip = pointGainSounds[2];
                break;
            case 4:
                soundSource.clip = pointGainSounds[3];
                break;
            case 5:
                soundSource.clip = pointGainSounds[4];
                break;
            case 6:
                soundSource.clip = pointGainSounds[5];
                break;
            case 7:
                soundSource.clip = pointGainSounds[6];
                break;
            case 8:
                soundSource.clip = pointGainSounds[7];
                break;
            case 9:
                soundSource.clip = pointGainSounds[8];
                break;
            case 10:
                soundSource.clip = pointGainSounds[9];
                break;
            default:
                soundSource.clip = pointGainSounds[0];
                break;
        }
        soundSource.Play();
    }
	
    protected void Update()
    {
        //update button inputs in here instead of fixedupdate
        shootPressed = shootPressed || Input.GetButtonDown(shootButton);
        dashPressed = dashPressed || Input.GetButtonDown(dashButton);
        hopPressed = hopPressed || Input.GetButtonDown(hopButton);
        lobPressed = lobPressed || Input.GetButtonDown(lobButton);
    }

	// FixedUpdate is called at a fixed rate
	protected override void FixedUpdate () {
        base.FixedUpdate();
        	
		//MOVEMENT--------------------------------------------------------------------
		//decrement dash timers
		dashTimer = Mathf.Max(0, dashTimer - Time.fixedDeltaTime);
		dashCooldownTimer = Mathf.Max(0, dashCooldownTimer - Time.fixedDeltaTime);
		//decrement stunnedTimer, freezeTime is already at least 0
		stunnedTimer = Mathf.Max(freezeTime, stunnedTimer - Time.fixedDeltaTime);
        bool stunned = stunnedTimer > 0;
        playerAnim.stunned = stunned;
        if (!stunned)
        {
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }
		//dash input
		if(dashCooldownTimer == 0 && dashPressed &&
			(dashWhileCarrying || carriedBall == null)) {
			dashTimer = dashDuration;
			dashCooldownTimer = dashCooldownDuration;
            //play sound
            soundSource.clip = dashSounds[Random.Range(0, dashSounds.Count)];
            soundSource.Play();
        }
        bool dashing = dashTimer > 0;
        Vector2 input = new Vector2(Input.GetAxis(xAxis), Input.GetAxis(yAxis));
        //normal movement if we aren't dashing.
        if (stunned)
        {
            line.enabled = true;
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
            /*
            //velocity movement
            body.velocity = new Vector3(0, body.velocity.y, 0) + transform.forward * dashSpeed;
            */

            //force movement
            body.AddForce(transform.forward * dashSpeed * moveAccel, ForceMode.Acceleration);
            if (body.velocity.magnitude > dashSpeed)
            {
                body.velocity = body.velocity.normalized * dashSpeed;
            }

            //effect
            line.enabled = true;
        }
        else if(Input.GetAxis(xAxis) == 0 && Input.GetAxis(yAxis) == 0 && dizzyTime == 0)
        {
            //jumping
            if (isOnGround && hopPressed)
            {
				Jump();
                //play sound
                soundSource.clip = jumpSounds[Random.Range(0, jumpSounds.Count)];
                soundSource.Play();
            }
            //apply force to stop when there's no input.
            Vector3 horizontalVelocity = new Vector3(body.velocity.x, 0, body.velocity.z);
            float horizontalMagnitude = horizontalVelocity.magnitude;
            if(horizontalMagnitude > (strafeSpeed + moveSpeed) * moveAccel * Time.fixedDeltaTime)
            {
                body.AddForce(-horizontalVelocity.normalized * (moveSpeed + strafeSpeed) * moveAccel, ForceMode.Acceleration);
            } else
            {
                body.velocity = new Vector3(0, body.velocity.y, 0);
            }
        }
        else
        {
            line.enabled = false;
            //get input vector from joystick/keys
            if (input.magnitude > 1)
            {
                input.Normalize();
            }
            if (dizzyTime > 0)
            {
                float spinAngle = (Time.time * 360 * DIZZYSPINSPEED) % 360;
                input = Quaternion.Euler(0, 0, spinAngle) * input;
            }
            float inputMag = input.magnitude;
            //rotate towards joystick
            if (inputMag != 0)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.Euler(0, 90 - Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg, 0),
                    Mathf.Min(1, inputMag / turnThreashold) * turnSpeed * Time.fixedDeltaTime);
            }
            //Debug.Log(new Vector2(transform.forward.x, transform.forward.z));
            //movement
            Vector3 movingVel = Vector3.zero;
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
            //spherecast forwards, ignoring the ball
            if (Physics.SphereCast(fRay, .4f, out fRHit, sDist, BALLMASK))
            {
                //get the angular difference between forward and the surface normal
                Vector3 tDir = Vector3.Cross(fRHit.normal, transform.up);
                //correct our movement left or right within a max angle of 0.5
                if (Vector3.Dot(tDir, movingVel) > .5f)
                {
                    movingVel = movingVel.magnitude * tDir;
                }
                else if (Vector3.Dot(-tDir, movingVel) > .5f)
                {
                    movingVel = movingVel.magnitude * -tDir;
                }
            }
            //Add movement force if our ball isn't ultimate
            if (carriedBall != null && carriedBall.ultimate)
            {

            }
            else
            {
                body.AddForce(movingVel * moveAccel, ForceMode.Acceleration);
            }
            //limit horizontal movement speed
            Vector3 horizontalVelocity = new Vector3(body.velocity.x, 0, body.velocity.z);
            if(horizontalVelocity.magnitude > moveSpeed + strafeSpeed)
            {
                Vector3 hReducedSpeed = horizontalVelocity.normalized * (moveSpeed + strafeSpeed);
                body.velocity = new Vector3(hReducedSpeed.x, body.velocity.y, hReducedSpeed.z);
            }
            
            //jumping
            if(isOnGround && hopPressed)
            {
				Jump();
                //play sound
                soundSource.clip = jumpSounds[Random.Range(0, jumpSounds.Count)];
                soundSource.Play();
            }
        }

        //update anim
        playerAnim.UpdateMotion(input);

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
                //gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerHitFieldObject, tp: this, fo:ballForwardCast.collider.GetComponent<FieldObject>()));
                //stop
                body.velocity = Vector3.zero;
				//stop dash
				dashTimer = 0;
                //calculate the distance to move away from the wall
                transform.position += ballForwardCast.normal * 
                    (Vector3.Dot(transform.forward, ballForwardCast.normal) *
                    (ballForwardCast.distance - rayDist)) * .5f;
                if (butterFingers)
                {
                    butterShot = true;
                }
            }

            //move the ball with us
            carriedBall.transform.position = transform.position + transform.forward * (ballHoldDistance + carriedBall.carryRadius);
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
            else if (shootPressed)
            {
                carriedBall.shoot(transform.forward * ballShootPower + body.velocity);
                if (shootButton == dashButton)
                {
                    dashCooldownTimer = dashCooldownDuration;
                }
                //play sound
                soundSource.clip = shootSounds[Random.Range(0, shootSounds.Count)];
                soundSource.Play();
            }
            else if (lobPressed)
            {
                carriedBall.shoot((transform.forward + transform.up * 1.5f).normalized * ballLobPower + body.velocity);
                if(lobButton == dashButton)
                {
                    dashCooldownTimer = dashCooldownDuration;
                }
                //play sound
                soundSource.clip = lobSounds[Random.Range(0, lobSounds.Count)];
                soundSource.Play();
            }

        }

        //clear button inputs
        shootPressed = false;
        dashPressed = false;
        hopPressed = false;
        lobPressed = false;
    }

	public override void Unfreeze() {
		base.Unfreeze();
		stunnedTimer = 0.0f;
	}

    void OnCollisionStay(Collision collision)
    {
		//right now continued collisions shouln't do anything
		/*
        if (checkBallCollision(collision.gameObject))
        {
            //should probably send a message about this to the rules manager
        }
		else if (checkPlayerCollision(collision.gameObject))
        {
            //also send the rules manager a message about this
        }
		*/
    }

    public void tackle(Vector3 launch, float duration)
    {
        body.constraints = RigidbodyConstraints.None;
        body.velocity = launch;
        body.angularVelocity = new Vector3(Random.value, Random.value, Random.value).normalized * 
            TACKLESPINNINESS;
        stunnedTimer = duration;
        //emit particles
        particles.Play();
        //play sound
        soundSource.clip = tackleSounds[Random.Range(0, tackleSounds.Count)];
        soundSource.Play();
    }

	public override void handleBallCollision(Ball collidedBall) {
		if (collidedBall.grabBall(this)) {
			carriedBall = collidedBall;
			if(carriedBall.ultimate) {
				body.velocity = Vector3.zero;
			}
			if(!dashWhileCarrying) {
				dashTimer = 0;
			}
			particles.Play();
		} else if (collidedBall.stuns) {
			//we didn't dodge the ball!
			tackle(collidedBall.getTackleVector(), collidedBall.tackleDuration);
			particles.Play();
		}
    }

	public override void handlePlayerCollision(TeamPlayer collidedPlayer) {
		gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerHitPlayer, tp: this, vct: collidedPlayer));
		if (dashTimer > 0) {
			//steal the ball
			if (collidedPlayer.carriedBall != null) {
				Ball stolenBall = collidedPlayer.carriedBall;
				if (collidedPlayer.carriedBall.grabBall(this))
					carriedBall = stolenBall;
			}
			//tackle them
			particles.Play();
			Vector3 tackleVector = transform.forward * tacklePower +
			Vector3.up * tackleLaunchPower;
			collidedPlayer.tackle(tackleVector, tackleDuration);
			gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerTacklePlayer, tp: this, vct: collidedPlayer));
		}
		if (dashStopByPlayer) {
			dashTimer = 0;
		}
    }

	public override void handleSportsCollision(SportsObject sObject) {
		gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerHitSportsObject, tp: this, so: sObject));
    }

	public override void handleFieldCollision(FieldObject fObject) {
        if (gameRules == null)
            return;
		gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerHitFieldObject, tp: this, fo: fObject));
    }

    public void removeBall(Ball rBall)
    {
        if (carriedBall == rBall)
        {
            carriedBall.transform.SetParent(null);
            carriedBall = null;
            particles.Play();
        }
    }
}
