using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class spriteSorter : MonoBehaviour {

    SpriteRenderer spr;
    public GameObject parentObject;

	// Use this for initialization
	void Start () {
        spr = GetComponent<SpriteRenderer>();
        spr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
    }
	
	// Update is called once per frame
	void Update () {
#if (UNITY_EDITOR)
        if (spr == null)
        {
            spr = GetComponent<SpriteRenderer>();
            if(spr == null)
            {
                Debug.LogError("No SpriteRenderer attached.");
            }
        }
#endif
        if (parentObject == null)
        {
            spr.sortingOrder = Mathf.RoundToInt(-transform.position.z * 100);
        }
        else
        { 
            spr.sortingOrder = Mathf.RoundToInt(-transform.localPosition.z * 1000) + 
                Mathf.RoundToInt(-parentObject.transform.position.z * 100);
        }
    }
}
