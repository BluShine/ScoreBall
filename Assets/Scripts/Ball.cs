using UnityEngine;
using System.Collections;

public class Ball : MonoBehaviour {

    //handling
    public float noTakebacksCooldown = .2f; //time in seconds
    float takeTimer = 0;
    public float carryRadius = .5f;
    public float tackleDuration = .2f;

    //state
    Rigidbody body;
    TeamPlayer currentPlayer;
    TeamPlayer previousPlayer;
    bool isHeld = false;

	GameRules gameRules;

	// Use this for initialization
	void Start () {
        body = GetComponent<Rigidbody>();
		gameRules = GameObject.Find("GameRules").GetComponent<GameRules>();
	}
	
	// FixedUpdate is called at a fixed rate
	void FixedUpdate () {
        takeTimer -= Time.fixedDeltaTime;
        takeTimer = Mathf.Max(0, takeTimer);
        if (isHeld)
        {
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            body.constraints = RigidbodyConstraints.None;
        }
	}

    void OnCollisionEnter(Collision collision)
    {
        if(checkBallCollision(collision))
        {

        } else if(checkPlayerCollision(collision))
        {

        } else
        {
            gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.BallHitObject, bl: this, col:collision.collider));
        }
    }

    public Vector3 getTackleVector()
    {
        return body.velocity;
    }

    bool checkPlayerCollision(Collision collision)
    {
        //check if the collision is a player
        TeamPlayer collidedPlayer = collision.gameObject.GetComponent<TeamPlayer>();
        if (collidedPlayer != null)
        {
            //Don't do anything, we let the player handle player-ball interactions
            return true;
        }
        return false;
    }

    bool checkBallCollision(Collision collision)
    {
        Ball hitBall = collision.gameObject.GetComponent<Ball>();
        if(hitBall != null)
        {
            gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.BallHitBall, bl: this, bl2: hitBall));
            return true;
        }
        return false;
    }

    //try to grab the ball. Returns true if you got it.
    public bool grabBall(TeamPlayer player)
    {
        if (takeTimer == 0)
        {
            if (currentPlayer != null)
            {
                currentPlayer.removeBall(this);
            }
            takeTimer = noTakebacksCooldown;
            previousPlayer = currentPlayer;
            currentPlayer = player;
            isHeld = true;
            gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerGrabBall, tp: currentPlayer, bl: this));
            return true;
        }
        return false;
    }

    public void shoot(Vector3 shootVector)
    {
        isHeld = false;
        body.constraints = RigidbodyConstraints.None;
        takeTimer = noTakebacksCooldown;
        body.velocity = shootVector;
        currentPlayer.removeBall(this);
        gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerShootBall, tp: currentPlayer, bl: this));
    }
}
