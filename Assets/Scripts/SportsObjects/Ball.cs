using UnityEngine;
using System.Collections.Generic;

public class Ball : SportsObject {

    //handling
    public float noTakebacksCooldown = .2f; //time in seconds
    float takeTimer = 0;
    public float carryRadius = .5f;
    public float tackleDuration = .2f;
    public bool holdable = true;//if true, players can grab the ball
    public bool ultimate = false;//if true, players can't move while holding the ball

    //state
    public TeamPlayer currentPlayer; //player who most recently touched the ball, possibly still holding it
    public TeamPlayer previousPlayer; //player who last touched the ball before currentPlayer
    bool isHeld = false;
    public bool stuns = false;

    //sound
    public List<AudioClip> kickSounds;
    public List<AudioClip> catchSounds;

	// Use this for initialization
	new void Start () {
        base.Start();
        soundSource = GetComponent<AudioSource>();
    }
	
	// FixedUpdate is called at a fixed rate
	protected override void FixedUpdate () {
        base.FixedUpdate();
        takeTimer -= Time.fixedDeltaTime;
        takeTimer = Mathf.Max(0, takeTimer);
        if (isHeld)
        {
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }
        else if (freezeTime == 0)
        {
            body.constraints = RigidbodyConstraints.None;
        }
	}

    public Vector3 getTackleVector()
    {
        return body.velocity;
    }

	public override void handleBallCollision(Ball hitBall) {
		gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.BallHitBall, bl: this, bl2: hitBall));
    }

	public override void handleSportsCollision(SportsObject sObject) {
		gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.BallHitSportsObject, bl: this, so: sObject));
    }

	public override void handleFieldCollision(FieldObject fObject) {
		gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.BallHitFieldObject, bl: this, fo: fObject));
    }

    //try to grab the ball. Returns true if you got it.
    public bool grabBall(TeamPlayer player)
    {
        if (holdable && takeTimer == 0)
        {
            bool wasHeld = isHeld;
            //give control of the ball to the player who 
            if (currentPlayer != null)
            {
                currentPlayer.removeBall(this);
            }
            takeTimer = noTakebacksCooldown;
            previousPlayer = currentPlayer;
            currentPlayer = player;
            isHeld = true;
            //send event
            if (wasHeld)
                gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerStealBall, tp: currentPlayer, vct: previousPlayer, bl: this));
            else
                gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerGrabBall, tp: currentPlayer, bl: this));
            //play sound
            soundSource.clip = catchSounds[Random.Range(0, catchSounds.Count)];
            soundSource.Play();
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
        //remove the player's control over the ball
        currentPlayer.removeBall(this);
        //send event
        gameRules.SendEvent(new GameRuleEvent(GameRuleEventType.PlayerShootBall, tp: currentPlayer, bl: this));
        //play sound
        soundSource.clip = catchSounds[Random.Range(0, kickSounds.Count)];
        soundSource.Play();
    }
}
