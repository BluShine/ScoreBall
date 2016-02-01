using UnityEngine;
using System.Collections;

public class linkylinky : MonoBehaviour {

    public string link;

    public void OpenLink()
    {
        Application.OpenURL(link);
    }
}
