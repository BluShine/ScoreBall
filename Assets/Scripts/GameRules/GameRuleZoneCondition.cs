using UnityEngine;
using System.Collections.Generic;

//GameRuleCondition class definition in GameRuleComparisonCondition.cs

////////////////Conditions for when an object is in a zone////////////////
public class GameRuleZoneCondition : GameRuleCondition {
	public GameRuleRequiredObject zoneType;
	public GameRuleSourceSelector selector;
	public Zone conditionZone; //assigned by GameRules after generating this rule
	public GameRuleZoneCondition(GameRuleRequiredObject zt, GameRuleSourceSelector s) {
		zoneType = zt;
		selector = s;
	}
	public override bool checkCondition(SportsObject triggeringObject) {
		return triggeringObject.GetType() == selector.targetType() && conditionZone.objectsInZone.Contains(triggeringObject);
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
	public override void addIcons(List<GameObject> iconList) {
		iconList.Add(Zone.getIconForZoneType(zoneType));
	}
	public override void packToString(GameRuleSerializer serializer) {
		serializer.packByte(GAME_RULE_CONDITION_BIT_SIZE, GAME_RULE_ZONE_CONDITION_BYTE_VAL);
		serializer.packToString(zoneType, Zone.standardZoneTypes);
		selector.packToStringAsSourceSelector(serializer);
	}
	public static new GameRuleZoneCondition unpackFromString(GameRuleDeserializer deserializer) {
		GameRuleRequiredObject zt = deserializer.unpackFromString(Zone.standardZoneTypes);
		GameRuleSourceSelector s = GameRuleSourceSelector.unpackFromString(deserializer);
		return new GameRuleZoneCondition(zt, s);
	}
}
