using UnityEngine;
using System.Collections;

public class SportsBanana : SportsObject {

    // Use this for initialization
    void OnCollisionEnter(Collision collision)
    {
        TeamPlayer tPlayer = collision.gameObject.GetComponent<TeamPlayer>();
        if (tPlayer != null)
        {
            tPlayer.tackle(new Vector3(0, 10, 0), 1f);
            if (!soundSource.isPlaying)
            {
                soundSource.clip = hitSounds[Random.Range(0, hitSounds.Count)];
                soundSource.Play();
            }
        }

        handleCollision(collision.gameObject);
    }
}
