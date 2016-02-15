using UnityEngine;
using System.Collections.Generic;

//a class for field objects which sports objects can enter and exit
//rules may check whether a sports object is in a zone
public class Zone : FieldObject {
	const float MESH_ICON_HALF_SIZE = 2.0f;

	public static List<GameRuleRequiredObject> standardZoneTypes = new List<GameRuleRequiredObject>(new GameRuleRequiredObject[] {
		GameRuleRequiredObject.BoomerangZone
	});

	[HideInInspector]
	public List<SportsObject> objectsInZone = new List<SportsObject>();

	public void buildZone(GameRuleRequiredObject zoneType) {
		Mesh collisionMesh = new Mesh();
        Mesh displayMesh = new Mesh();

		//build up a list of triangles
		//for now our floor vertex list is just a square
		Vector2[] groundPolygon = new Vector2[] {
			new Vector2(-8, -8),
			new Vector2(-8, 8),
			new Vector2(8, 8),
			new Vector2(8, -8)
		};

        Vector2[] UVs = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0)
        };

        float groundY = GameRules.instance.floor.GetComponent<Collider>().bounds.extents.y + 0.01f;
		//build the vertex list for the collision mesh
		//also keep track of the center of the zone for the render mesh
		Vector3[] vertices = new Vector3[groundPolygon.Length * 2];
        Vector3[] displayVertices = new Vector3[groundPolygon.Length];
        float minX = float.PositiveInfinity, maxX = 0.0f;
		float minZ = float.PositiveInfinity, maxZ = 0.0f;
		for (int i = 0; i < groundPolygon.Length; i++) {
			float vx = groundPolygon[i].x;
			float vz = groundPolygon[i].y; //Vector2 stores x and y but we want to use z instead of y
			vertices[i] = new Vector3(vx, groundY, vz);
            displayVertices[i] = new Vector3(vx, groundY, vz);
            vertices[i + groundPolygon.Length] = new Vector3(vx, 100, vz);
			minX = Mathf.Min(minX, vx);
			maxX = Mathf.Max(maxX, vx);
			minZ = Mathf.Min(minZ, vz);
			maxZ = Mathf.Max(maxZ, vz);
		}
        displayMesh.vertices = displayVertices;
        displayMesh.uv = UVs;
        displayMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };

        collisionMesh.vertices = vertices;
		collisionMesh.triangles = new int[] {
			0, 1, 2,
			0, 2, 3
		};
		GetComponent<MeshCollider>().sharedMesh = collisionMesh;
		GetComponent<MeshFilter>().mesh = displayMesh;
	}
	public GameObject getIconForZoneType(GameRuleRequiredObject zoneType) {
		if (zoneType == GameRuleRequiredObject.BoomerangZone)
			return GameRuleIconStorage.instance.boomerangZone;
		else
			throw new System.Exception("Bug: could not get zone icon for " + zoneType);
	}
}
