using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class FloraObject : MonoBehaviour {

    public Gradient colorGradient;

	// Use this for initialization
	void Start () {
        SpriteRenderer spr = GetComponent<SpriteRenderer>();
        spr.color = colorGradient.Evaluate(Random.value);
	}

#if (UNITY_EDITOR)
    // Update is called once per frame
    void Update() {
        if (!Application.isPlaying)
        {
            SpriteRenderer spr = GetComponent<SpriteRenderer>();
            spr.color = colorGradient.Evaluate(Random.value);
        }
    }
#endif
}
