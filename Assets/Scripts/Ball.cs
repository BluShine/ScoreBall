using UnityEngine;
using System.Collections;
using GameRuleTypes;

public class Ball : MonoBehaviour {

    //handling
    public float noTakebacksCooldown = .2f; //time in seconds
    float takeTimer = 0;
    public float carryRadius = .5f;

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

    //try to grab the ball. Returns true if you got it.
    public bool grabBall(TeamPlayer player)
    {
        if (takeTimer == 0)
        {
            Debug.Log("Grabbed!");
            if (currentPlayer != null)
            {
                currentPlayer.removeBall(this);
            }
            takeTimer = noTakebacksCooldown;
            previousPlayer = currentPlayer;
            currentPlayer = player;
            isHeld = true;
            return true;
        }
        Debug.Log("Grab failed");
        return false;
    }

    public void shoot(Vector3 shootVector)
    {
        isHeld = false;
        body.constraints = RigidbodyConstraints.None;
        takeTimer = noTakebacksCooldown;
        body.velocity = shootVector;
        currentPlayer.removeBall(this);
    }
}
