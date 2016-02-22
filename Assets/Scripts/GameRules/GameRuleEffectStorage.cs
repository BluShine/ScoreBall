using UnityEngine;
using System.Collections;

public class GameRuleEffectStorage : MonoBehaviour {
	public static GameRuleEffectStorage instance;

	public GameObject freeze;
	public GameObject bouncy;
	public GameObject dizzy;

	public void Start() {
		instance = this;
	}
}
