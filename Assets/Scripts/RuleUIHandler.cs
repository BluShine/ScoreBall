using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RuleUIHandler : MonoBehaviour {

    GameRuleCondition condition;
    GameRuleSelector actionSelector;
    GameRuleActionAction aAction;

    public Slider pointslider;
    public Slider effectSlider;

	// Use this for initialization
	void Start () {
        actionSelector = new GameRulePlayerSelector();
        condition = new GameRuleEventHappenedCondition(
            GameRuleEventType.PlayerHitPlayer, 
            new GameRulePlayerSelector(), "");
	}
	
	// Update is called once per frame
	void Update () {
	    
	}

    public void selectEffect(Dropdown d)
    {
        switch (d.value)
        {
            case 0:
                //dizzy
                aAction = new GameRuleDizzyActionAction(
            new GameRuleActionFixedDuration(
                Mathf.FloorToInt(effectSlider.value)));
                break;
            case 1:
                //frozen
                aAction = new GameRuleFreezeActionAction(
            new GameRuleActionFixedDuration(
                Mathf.FloorToInt(effectSlider.value)));
                break;
            case 2:
                //bouncy
                aAction = new GameRuleBounceActionAction(
            new GameRuleActionFixedDuration(
                Mathf.FloorToInt(effectSlider.value)));
                break;
        }
        
    }

    public void selectPoints()
    {
        aAction = new GameRulePointsPlayerActionAction(Mathf.FloorToInt(pointslider.value));
    }

    public void selectDuplicate()
    {
        aAction = new GameRuleDuplicateActionAction();
    }

    public void SendRule()
    {
        if (condition == null || actionSelector == null || aAction == null)
            return;
        GameRule r = new GameRule(condition, new GameRuleAction(actionSelector, aAction), true);
        Debug.Log(GameRuleSerializer.packRuleToString(r));
    }
}
