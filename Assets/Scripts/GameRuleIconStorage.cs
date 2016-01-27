using UnityEngine;
using System.Collections.Generic;

//this really could be a part of GameRules but I put it in its own class to prevent clutter
public class GameRuleIconStorage : MonoBehaviour {
	//so we have access to all the icons
	public static GameRuleIconStorage instance;

	//all the various icon images
	//player selectors
	public Sprite playerIcon;
	public Sprite opponentIcon;

	//ball selectors
	public Sprite genericBallIcon;
	public Sprite soccerBallIcon;
	public Sprite beachBallIcon;
	public Sprite ultimateBallIcon;
	public Sprite fishIcon;

	//sports object types
	public Sprite genericSportsObjectIcon;
	public Sprite bananaIcon;
	public Sprite catIcon;

	//field object types
	public Sprite soccerGoalIcon;
	public Sprite backboardHoopIcon;
	public Sprite wallIcon;
	public Sprite goalpostsIcon;
	public Sprite boundaryIcon;

	//events
	public Sprite stealIcon;
	public Sprite bumpIcon;
	public Sprite scoreGoalIcon;
	public Sprite smackIcon;
	public Sprite grabIcon;
	public Sprite kickIcon;

	//effects
	public Sprite duplicatedIcon;
	public Sprite frozenIcon;
	public Sprite dizzyIcon;
	public Sprite bouncyIcon;
	public Sprite charPlusIcon;
	public Sprite charMinusIcon;
	public Sprite[] charDigitIcons;

	//icons to join other icons
	public Sprite clockIcon;
	public Sprite gainsEffectIcon;
	public Sprite resultsInIcon;

	//zone icons
	public Sprite oppositeZone;

	void Start () {
		instance = this;
	}
	public void addDigitIcons(int value, List<Sprite> iconList) {
		List<int> digits = new List<int>();
		for (; value > 0; value /= 10)
			digits.Add(value % 10);
		//gotta put them in most-significant-digit first
		for (int i = digits.Count - 1; i >= 0; i--)
			iconList.Add(charDigitIcons[digits[i]]);
	}
}
