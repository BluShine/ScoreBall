using UnityEngine;
using System.Collections.Generic;

//this really could be a part of GameRules but I put it in its own class to prevent clutter
public class GameRuleIconStorage : MonoBehaviour {
	//so we have access to all the icons
	public static GameRuleIconStorage instance;

	//all the various icon images
	//player selectors
	public GameObject playerIcon;
	public GameObject opponentIcon;

	//ball selectors
	public GameObject genericBallIcon;
	public GameObject soccerBallIcon;
	public GameObject beachBallIcon;
	public GameObject ultimateBallIcon;
	public GameObject fishIcon;

	//sports object types
	public GameObject genericSportsObjectIcon;
	public GameObject bananaIcon;
	public GameObject catIcon;

	//field object types
	public GameObject soccerGoalIcon;
	public GameObject backboardHoopIcon;
	public GameObject wallIcon;
	public GameObject goalpostsIcon;
	public GameObject boundaryIcon;

	//events
	public GameObject stealIcon;
	public GameObject bumpIcon;
	public GameObject scoreGoalIcon;
	public GameObject smackIcon;
	public GameObject grabIcon;
	public GameObject kickIcon;

	//effects
	public GameObject duplicatedIcon;
	public GameObject frozenIcon;
	public GameObject dizzyIcon;
	public GameObject bouncyIcon;
	public GameObject charPlusIcon;
	public GameObject charMinusIcon;
	public GameObject[] charDigitIcons;
	public GameObject charAmpersandIcon;
	public GameObject charSlashIcon;

	//icons to join other icons
	public GameObject clockIcon;
	public GameObject gainsEffectIcon;
	public GameObject resultsInIcon;

	//zone icons
	public GameObject boomerangZone;

	void Start () {
		instance = this;
	}
	public void addDigitIcons(int value, List<GameObject> iconList) {
		List<int> digits = new List<int>();
		for (; value > 0; value /= 10)
			digits.Add(value % 10);
		//gotta put them in most-significant-digit first
		for (int i = digits.Count - 1; i >= 0; i--)
			iconList.Add(charDigitIcons[digits[i]]);
	}
}
