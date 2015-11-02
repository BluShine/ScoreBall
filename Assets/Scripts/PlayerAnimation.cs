using UnityEngine;
using System.Collections;

public class PlayerAnimation : MonoBehaviour {

    Animator anim;

    float turnTime = 0;
    static float TURNMINTIME = 0.1f;
    public Vector3 offset = new Vector3(0,0,0.5f);

    enum Direction { left, right, towards, away}
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
            transform.rotation = Quaternion.Euler(35, 0, 0);
            transform.position = transform.parent.position + offset;
        }

        turnTime -= Time.deltaTime;
        turnTime = Mathf.Max(0, turnTime);
        RaycastHit ray;
        Physics.Raycast(transform.position, Vector3.down, out ray);
        shadow.transform.position = ray.point + new Vector3(0, .01f, 0);
	}

    public void UpdateMotion(Vector2 motion)
    {
        if (turnTime > 0 || motion.sqrMagnitude == 0)
            return;
        //going horizontal or vertical
        if(Mathf.Abs(motion.x) + .001 >= Mathf.Abs(motion.y)) //a little bit biased towards horizontal animations
        {
            //going right or left?
            if(motion.x > 0)
            {
                if (lastDirection != Direction.right)
                {
                    lastDirection = Direction.right;
                    //use negative scale to flip the sprite right
                    transform.localScale = new Vector3(
                        -Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                    turnTime = TURNMINTIME;
                    anim.SetInteger("state", 0);
                }
            } else
            {
                if (lastDirection != Direction.left)
                {
                    lastDirection = Direction.left;
                    transform.localScale = new Vector3(
                        Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                    turnTime = TURNMINTIME;
                    anim.SetInteger("state", 0);
                }
            }
        } else
        {
            if(motion.y > 0)
            {
                if(lastDirection != Direction.away)
                {
                    lastDirection = Direction.away;
                    transform.localScale = new Vector3(
                        Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                    turnTime = TURNMINTIME;
                    anim.SetInteger("state", 2);
                }
            } else
            {
                if (lastDirection != Direction.towards)
                {
                    lastDirection = Direction.towards;
                    transform.localScale = new Vector3(
                        Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                    turnTime = TURNMINTIME;
                    anim.SetInteger("state", 1);
                }
            }
        }
    }
}
