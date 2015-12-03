using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class FloraObject : MonoBehaviour {

    public Gradient colorGradient;
    public bool fliprandomly = false;

	// Use this for initialization
	void Start () {
        SpriteRenderer spr = GetComponent<SpriteRenderer>();
        spr.color = colorGradient.Evaluate(Random.value);
        if (fliprandomly && Random.value > .5f)
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
	}

/*#if (UNITY_EDITOR)
    // Update is called once per frame
    void Update() {
        if (!Application.isPlaying)
        {
            SpriteRenderer spr = GetComponent<SpriteRenderer>();
            spr.color = colorGradient.Evaluate(Random.value);
            if (fliprandomly && Random.value > .5f)
                transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        }
    }
#endif*/
}
