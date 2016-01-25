using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UITextUpdater : MonoBehaviour {

    Text text;
    public string title = " points";

	// Use this for initialization
	void Start () {
        text = GetComponent<Text>();
	}
	
    public void updateSlider(Slider slider)
    {
        text.text = slider.value + title;
    }
}
