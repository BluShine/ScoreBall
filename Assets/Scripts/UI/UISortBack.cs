using UnityEngine;
using System.Collections;

public class UISortBack : MonoBehaviour {

	// Use this for initialization
	void Start () {
            int index = transform.GetSiblingIndex();
            transform.SetSiblingIndex(index - 1);
        }
}
