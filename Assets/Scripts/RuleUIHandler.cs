using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RuleUIHandler : MonoBehaviour {
/*
    GameRuleCondition condition;
    GameRuleSelector actionSelector;
    GameRuleEffect effect;

    public Slider pointslider;
    public Slider effectSlider;

    public RuleNetworking ruleNetwork;

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
                effect = new GameRuleDizzyEffect(
            new GameRuleActionFixedDuration(
                Mathf.FloorToInt(effectSlider.value)));
                break;
            case 1:
                //frozen
                effect = new GameRuleFreezeEffect(
            new GameRuleActionFixedDuration(
                Mathf.FloorToInt(effectSlider.value)));
                break;
            case 2:
                //bouncy
                effect = new GameRuleBounceEffect(
            new GameRuleActionFixedDuration(
                Mathf.FloorToInt(effectSlider.value)));
                break;
        }
        
    }

    public void selectPoints()
    {
        effect = new GameRulePointsPlayerEffect(Mathf.FloorToInt(pointslider.value));
    }

    public void selectDuplicate()
    {
        effect = new GameRuleDuplicateEffect();
    }

    public void SendRule()
    {
		if (condition == null || actionSelector == null || effect == null)
            return;
		GameRule rule = new GameRule(condition, new GameRuleEffectAction(actionSelector, effect));
        ruleNetwork.sendRule(GameRuleSerializer.packRuleToString(rule));
    }
*/
}
