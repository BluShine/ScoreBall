using UnityEngine;
using System.Collections.Generic;

public class TextureAnimator : MonoBehaviour {

    public float animSpeed = .5f;
    float animTimer = 0;
    public int frames = 3;
    int currentFrame = 0;
    MeshFilter mFilter;
    Mesh m;
    List<Vector2[]> uvFrames;

	// Use this for initialization
	void Start () {
        mFilter = GetComponent<MeshFilter>();
        m = mFilter.mesh;
        uvFrames = new List<Vector2[]>();
        Vector2[] uv = m.uv;
        for (int i = 0; i < frames; i++)
        {
            Vector2[] newUV = new Vector2[uv.Length];
            for(int j = 0; j < uv.Length; j++)
            {
                newUV[j] = new Vector2(uv[j].x + i, uv[j].y);
            }
            uvFrames.Add(newUV);
        }
	}
	
	// Update is called once per frame
	void Update () {
        animTimer += Time.deltaTime;
        if(animTimer >= animSpeed)
        {
            animTimer -= animSpeed;
            currentFrame = (currentFrame + 1) % frames;
            mFilter.mesh.uv = uvFrames[currentFrame];
        }
	}
}
