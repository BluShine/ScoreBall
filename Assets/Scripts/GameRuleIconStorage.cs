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

	void Start () {
		instance = this;
	}
	public void addDigitIcons(int value, List<Sprite> iconList) {
		for (; value > 0; value /= 10)
			iconList.Add(charDigitIcons[value % 10]);
	}
}
