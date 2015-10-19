using UnityEngine;
using System.Collections.Generic;

//base class for dynamic objects with rigidbodies: players, balls, etc. 
//includes methods for respawning, duplicating, un-duplicating, and other behaviors that need to work for all dynamic objects
public class SportsObject : MonoBehaviour {

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
    [HideInInspector]
    public GameRules gameRules;

    [HideInInspector]
    public bool spawned = false;

    [HideInInspector]
    public float freezeTime { get; private set; }
    bool usesFreezing = true;

    public string sportName = "object";

    // Use this for initialization
    public void Start () {
        if (!spawned)
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation.eulerAngles;
            spawnScale = transform.localScale;
            duplicates = new List<SportsObject>();
            duplicates.Add(this);
            freezeTime = 0;
        }
        body = GetComponent<Rigidbody>();
        gameRules = GameObject.Find("GameRules").GetComponent<GameRules>();
    }

    public void useDefaultFreezing(bool useDefFreeze)
    {
        usesFreezing = useDefFreeze;
    }
	
	// Update is called once per frame
	public virtual void FixedUpdate () {
        freezeTime -= Time.fixedDeltaTime;
        freezeTime = Mathf.Max(0, freezeTime);
        if (usesFreezing)
        {
            if (freezeTime > 0)
            {
                body.constraints = RigidbodyConstraints.FreezeAll;
            }
            else
            {
                body.constraints = RigidbodyConstraints.None;
            }
        }
	}

    public virtual void Respawn()
    {
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
        for(int i = 0; i < times; i++)
        {
            SportsObject dupe = Instantiate<SportsObject>(this);
            //manually "spawn" our duplicate so that it has our same spawnposition, etc.
            dupe.spawnPosition = spawnPosition;
            dupe.spawnRotation = spawnRotation;
            dupe.spawnScale = spawnScale;
            dupe.duplicates = duplicates;
            dupe.Freeze(freezeTime);
            dupe.spawned = true;
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
        freezeTime = Mathf.Max(freezeTime, duration);
    }
}
