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
	public GameObject genericEventIcon;
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
	public GameObject genericZoneIcon;
	public GameObject boomerangZoneIcon;

	void Start () {
		instance = this;
	}
	public void addDigitIcons(int value, List<GameObject> iconList) {
		if (value < 10)
			iconList.Add(charDigitIcons[value]);
		//gotta put them in most-significant-digit first, recursion helps
		else {
			addDigitIcons(value / 10, iconList);
			iconList.Add(charDigitIcons[value % 10]);
		}
	}
}
