using UnityEngine;
using System.Collections.Generic;

////////////////Conditions for when an object is in a zone////////////////
public class GameRuleZoneCondition : GameRuleCondition {
	public GameRuleRequiredObject zoneType;
	public GameRuleSelector selector;
	public Zone conditionZone; //assigned by GameRules after generating this rule
	public GameRuleZoneCondition(GameRuleRequiredObject zt, GameRuleSelector s) {
		zoneType = zt;
		selector = s;
	}
	public override bool checkCondition(SportsObject triggeringObject) {
		Debug.Log("Zone checking for " + triggeringObject + " returning " + (triggeringObject is TeamPlayer && conditionZone.objectsInZone.Contains(triggeringObject)));
		return triggeringObject is TeamPlayer && conditionZone.objectsInZone.Contains(triggeringObject);
	}
	public override string ToString() {
		return selector.ToString() + " is in the " + getZoneNameString() + " zone";
	}
	public string getZoneNameString() {
		if (zoneType == GameRuleRequiredObject.BoomerangZone)
			return "boomerang";
		else
			throw new System.Exception("Bug: no zone name for " + zoneType);
	}
	public override void addRequiredObjects(List<GameRuleRequiredObject> requiredObjectsList) {
		requiredObjectsList.Add(zoneType);
	}
	public override void addIcons(List<Sprite> iconList) {
		if (zoneType == GameRuleRequiredObject.BoomerangZone)
			iconList.Add(GameRuleIconStorage.instance.boomerangZone);
		else
			throw new System.Exception("Bug: no zone icon for " + zoneType);
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_CONDITION_BIT_SIZE, 2);
		serializer.packToString(zoneType, Zone.standardZoneTypes);
		selector.packToString(serializer);
	}
	public static new GameRuleZoneCondition unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleRequiredObject zt = deserializer.unpackFromString(Zone.standardZoneTypes);
		GameRuleSelector s = GameRuleSelector.unpackFromString(deserializer);
		return new GameRuleZoneCondition(zt, s);
	}
}
