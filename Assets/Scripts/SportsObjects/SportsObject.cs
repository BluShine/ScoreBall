using UnityEngine;
using System.Collections.Generic;

//base class for dynamic objects with rigidbodies: players, balls, etc. 
//includes methods for respawning, duplicating, un-duplicating, and other behaviors that need to work for all dynamic objects
//collisions are handled here, but ignored; subclasses will provide functionality for handling collisions
public class SportsObject : FieldObject {

    [HideInInspector]
    public Vector3 spawnPosition { get; private set; }
    [HideInInspector]
    public Vector3 spawnRotation { get; private set; }
    [HideInInspector]
    public Vector3 spawnScale { get; private set; }
    [HideInInspector]
    public Rigidbody body;
    [HideInInspector]
    public List<SportsObject> duplicates { get; private set; }
    static int MAXDUPLICATES = 100;
    static float DUPLICATIONCOOLDOWN = .15f;
    float dupeCoolTimer = .15f;
    public float jumpSpeed = 10; //velocity when you jump

    static float DUPELICATELIFETIME = 20;
    public float lifeTime = 0;
    public bool expires;

    [HideInInspector]
    public GameRules gameRules;

    [HideInInspector]
    public bool spawned = false;

    [HideInInspector]
    public float freezeTime { get; private set; }
    bool usesFreezing = true;
    [HideInInspector]
    public float dizzyTime { get; private set; }
    bool usesDizzy = true;
    static Vector3 DIZZYSPINVECTOR = new Vector3(0, 30, 0);

	[HideInInspector]
	public float bounceTime { get; private set; }
	protected bool isOnGround = false;
	protected bool preJump = false; //indicates the time between starting a jump and leaving the ground

    //sound
    public List<AudioClip> hitSounds;
    [HideInInspector]
    public AudioSource soundSource;

    private RigidbodyConstraints startingConstraints;

    // Use this for initialization
    public virtual void Start () {
        if (!spawned)
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation.eulerAngles;
            spawnScale = transform.localScale;
            duplicates = new List<SportsObject>();
            duplicates.Add(this);
            freezeTime = 0;
            dizzyTime = 0;
        }
        body = GetComponent<Rigidbody>();
        gameRules = GameObject.Find("GameRules").GetComponent<GameRules>();
        soundSource = GetComponent<AudioSource>();
        startingConstraints = body.constraints;
    }

    public void useDefaultFreezing(bool useDefFreeze)
    {
        usesFreezing = useDefFreeze;
    }

    public void useDefaultDizzy(bool useDefDizzy)
    {
        usesDizzy = useDefDizzy;
    }
	
	// Update is called once per frame
	public virtual void FixedUpdate () {
        dupeCoolTimer = Mathf.Max(0, dupeCoolTimer - Time.fixedDeltaTime);
        lifeTime = Mathf.Max(0, lifeTime - Time.fixedDeltaTime);
        if(expires && lifeTime == 0)
        {
            Destroy(this.gameObject);
            return;
        }
        freezeTime = Mathf.Max(0, freezeTime - Time.fixedDeltaTime);
        if (usesFreezing)
        {
            if (freezeTime > 0)
            {
                body.constraints = RigidbodyConstraints.FreezeAll;
            }
            else
            {
                body.constraints = startingConstraints;
            }
        }
        dizzyTime = Mathf.Max(0, dizzyTime - Time.fixedDeltaTime); 
        if (usesDizzy && dizzyTime > 0)
        {
            body.AddTorque(DIZZYSPINVECTOR, ForceMode.Acceleration);
        }

		bounceTime = Mathf.Max(0, bounceTime - Time.fixedDeltaTime);
		//not on ground, reset pre-jump
		if (!isOnGround)
			preJump = false;
		//object is on the ground and is bouncing
		else if (bounceTime > 0)
			Jump();
	}

    public virtual void Respawn()
    {
        if(expires)
        {
            Destroy(this.gameObject);
            return;
        }
        transform.position = spawnPosition;
        transform.rotation = Quaternion.Euler(spawnRotation);
        transform.localScale = spawnScale;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    public virtual void OnDestroy()
    {
        duplicates.Remove(this);
    }

    public virtual void Duplicate(int times)
    {
        if(dupeCoolTimer > 0) return;
        dupeCoolTimer = DUPLICATIONCOOLDOWN;
        for(int i = 0; i < times; i++)
        {
            if(duplicates.Count >= MAXDUPLICATES)
            {
                return;
            }
            SportsObject dupe = Instantiate<SportsObject>(this);
            //manually "spawn" our duplicate so that it has our same spawnposition, etc.
            dupe.spawnPosition = spawnPosition;
            dupe.spawnRotation = spawnRotation;
            dupe.spawnScale = spawnScale;
            dupe.duplicates = duplicates;
            dupe.Freeze(freezeTime);
            dupe.spawned = true;
            dupe.expires = true;
            dupe.lifeTime = DUPELICATELIFETIME;
            duplicates.Add(dupe);
        }
    }

    public virtual void UnDuplicateAll()
    {
        foreach (SportsObject dupe in duplicates)
        {
            if (dupe != this)
            {
                Destroy(dupe);
            }
        }
    }

    public virtual void Freeze(float duration)
    {
		freezeTime = Mathf.Max(duration, freezeTime);
    }

	public virtual void Unfreeze() {
		freezeTime = 0.0f;
	}

	public virtual void BeDizzy(float duration) {
		dizzyTime = Mathf.Max(duration, dizzyTime);
	}

	public virtual void StopBeingDizzy() {
		dizzyTime = 0.0f;
	}

	public virtual void StartBouncing(float duration) {
		bounceTime = Mathf.Max(duration, bounceTime);
	}

	public virtual void StopBouncing() {
		bounceTime = 0.0f;
	}

	public virtual void Jump() {
		//make sure we're not already trying to jump
		if (!preJump) {
			Debug.Log("bounce " + bounceTime);
			Vector3 velocity = body.velocity;
			velocity.y = Mathf.Max(velocity.y, jumpSpeed);
			body.velocity = velocity;
			preJump = true;
		}
	}

	//collision handling
	protected virtual void OnCollisionEnter(Collision collision) {
		handleCollision(collision.gameObject);
        //play a sound if we aren't already playing a more important sound
        if (!soundSource.isPlaying)
        {
            soundSource.clip = hitSounds[Random.Range(0, hitSounds.Count)];
            soundSource.Play();
        }
    }

	void OnTriggerEnter(Collider collider) {
        handleCollision(collider.gameObject);
	}

	protected void handleCollision(GameObject gameObject) {
		if (gameObject == gameRules.floor) {
			isOnGround = true;
		} else if (checkBallCollision(gameObject)) {
        } else if (checkPlayerCollision(gameObject)) {
        } else if (checkSportsCollision(gameObject)) {
        } else if (checkFieldCollision(gameObject)) {
        }
    }

	public bool checkPlayerCollision(GameObject gameObject) {
		//check if the collision is with a player
		TeamPlayer collidedPlayer = gameObject.GetComponent<TeamPlayer>();
		if (collidedPlayer != null) {
			handlePlayerCollision(collidedPlayer);
			return true;
		}
		return false;
	}

	public bool checkBallCollision(GameObject gameObject) {
		//check if the collision is with a ball
		Ball hitBall = gameObject.GetComponent<Ball>();
		if (hitBall != null) {
			handleBallCollision(hitBall);
			return true;
		}
		return false;
	}

	public bool checkSportsCollision(GameObject gameObject) {
		//check if the collision is with a sports object other than a ball or player
		SportsObject sObject = gameObject.GetComponent<SportsObject>();
		if (sObject != null) {
			handleSportsCollision(sObject);
			return true;
		}
		return false;
	}

	public bool checkFieldCollision(GameObject gameObject) {
		//check if the collision is with a field object
		FieldObject fObject = gameObject.GetComponent<FieldObject>();
		if (fObject != null) {
			handleFieldCollision(fObject);
			return true;
		}
		return false;
	}

	public virtual void handlePlayerCollision(TeamPlayer collidedPlayer) {}
	public virtual void handleBallCollision(Ball hitBall) {}
	public virtual void handleSportsCollision(SportsObject sObject) {}
	public virtual void handleFieldCollision(FieldObject fObject) {}

	void OnCollisionExit(Collision collision) {
		handleCollisionExit(collision.gameObject);
	}

	void OnTriggerExit(Collider collider) {
		handleCollisionExit(collider.gameObject);
	}

	void handleCollisionExit(GameObject gameObject) {
		if (gameObject == gameRules.floor)
			isOnGround = false;
	}
}
