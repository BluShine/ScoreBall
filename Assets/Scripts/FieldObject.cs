﻿using UnityEngine;
using System.Collections.Generic;

public class FieldObject : MonoBehaviour {
	public static List<string> standardFieldObjects; //GameRuleSpawnableObjectRegistry will build this

    public string sportName = "object";
    public byte team = 0; //team 0 is neutral. 


    public virtual void setColor(Color color)
    {
        foreach (MeshRenderer mesh in GetComponents<MeshRenderer>()) {
            mesh.material.color = color;
        }
        foreach(SpriteRenderer sprite in GetComponents<SpriteRenderer>())
        {
            sprite.material.color = color;
        }
        foreach (MeshRenderer mesh in GetComponentsInChildren<MeshRenderer>())
        {
            mesh.material.color = color;
        }
        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>())
        {
            sprite.material.color = color;
        }
    }
}
