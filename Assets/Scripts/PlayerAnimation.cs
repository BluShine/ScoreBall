using UnityEngine;
using System.Collections;

public class PlayerAnimation : MonoBehaviour {

    Animator anim;

    float turnTime = 0;
    static float TURNMINTIME = 0.1f;
    public Vector3 offset = new Vector3(0,0,0.5f);
    public Vector3 shadowOffset = new Vector3(0, .01f, .5f);
    public float rotation = 35;

    enum Direction { NW, SW, NE, SE}
    Direction lastDirection;

    public bool stunned = false;

    Camera cam;

    public SpriteRenderer shadow;

	// Use this for initialization
	void Start () {
        anim = GetComponent<Animator>();
        cam = FindObjectOfType<Camera>();
	}
	
	// Update is called once per frame
	void Update () {
        if (!stunned)
        {
            transform.rotation = Quaternion.Euler(rotation, 0, 0);
            transform.position = transform.parent.position + offset;
        }

        turnTime -= Time.deltaTime;
        turnTime = Mathf.Max(0, turnTime);
        RaycastHit ray;
        Physics.Raycast(transform.position, Vector3.down, out ray);
        shadow.transform.position = ray.point + shadowOffset;
	}

    public void UpdateMotion(Vector2 motion)
    {
        //if (turnTime > 0)
        //    return;


        //going horizontal or vertical
        if(motion.y > 0) //running "North" aka "away from the camera"
        {
            //going right or left?
            if(motion.x > 0)
            {
                if (lastDirection != Direction.NW)
                {
                    lastDirection = Direction.NW;
                    //use negative scale to flip the sprite right
                    transform.localScale = new Vector3(
                        -Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                    turnTime = TURNMINTIME;
                    anim.SetTrigger("run_b");
                }
            } else
            {
                if (lastDirection != Direction.NE)
                {
                    lastDirection = Direction.NE;
                    transform.localScale = new Vector3(
                        Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                    turnTime = TURNMINTIME;
                    anim.SetTrigger("run_b");
                }
            }
        } else if (motion.y <= 0)
        {
            //going right or left?
            if (motion.x > 0)
            {
                if (lastDirection != Direction.SW)
                {
                    lastDirection = Direction.SW;
                    //use negative scale to flip the sprite right
                    transform.localScale = new Vector3(
                        -Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                    turnTime = TURNMINTIME;
                    anim.SetTrigger("run");
                }
            }
            else if (motion.x < 0 || motion.y != 0)
            {
                if (lastDirection != Direction.SE)
                {
                    lastDirection = Direction.SE;
                    transform.localScale = new Vector3(
                        Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                    turnTime = TURNMINTIME;
                    anim.SetTrigger("run");
                }
            } else
            {
                anim.SetTrigger("idle");
            }
        } else if (motion.y == 0 && motion.x == 0)
        {
            anim.SetTrigger("idle");
        }
    }
}
