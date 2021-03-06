﻿using UnityEngine;
using System.Collections.Generic;

//a class for field objects which sports objects can enter and exit
//rules may check whether a sports object is in a zone
public class Zone : FieldObject {
	const float MAIN_TEXTURE_UV_TOP = 0.0f;
	const float MAIN_TEXTURE_UV_LEFT = 0.0f;
	const float MAIN_TEXTURE_UV_WIDTH = 8.0f;
	const float MAIN_TEXTURE_UV_HEIGHT = 8.0f;
	const float ZONE_LINE_HALF_WIDTH = 0.375f;
	const float MESH_HEIGHT_EPSILON = 0.01f;
	const float ZONE_LINE_CORNER_DEGREES = 11.25f; //8 triangles for 90 degrees
	const float DEGREES_TO_RADIANS = Mathf.PI / 180.0f;

	public static List<GameRuleRequiredObjectType> standardZoneTypes =
		new List<GameRuleRequiredObjectType>(new GameRuleRequiredObjectType[] {
			GameRuleRequiredObjectType.BoomerangZone
		});

	[HideInInspector]
	public List<SportsObject> objectsInZone = new List<SportsObject>();

	public void buildZone(GameRuleRequiredObjectType zoneType) {
		Mesh collisionMesh = new Mesh();
		Mesh displayMesh = new Mesh();
		Mesh lineMesh = new Mesh();

		//build up a list of triangles, going clockwise
		//pick 4 random locations along lines of an X
		Vector2[] groundPolygon = new Vector2[] {
			new Vector2(-1, -1),
			new Vector2(-1, 1),
			new Vector2(1, 1),
			new Vector2(1, -1)
		};
		for (int i = 0; i < groundPolygon.Length; i++) {
			groundPolygon[i] *= (4.0f + Random.Range(0.0f, 8.0f));
		}

		//map the UVs for the main texture
		Vector2[] displayUVs = new Vector2[groundPolygon.Length];
		for (int i = 0; i < displayUVs.Length; i++) {
			displayUVs[i] = new Vector2(
				(groundPolygon[i].x - MAIN_TEXTURE_UV_LEFT) / MAIN_TEXTURE_UV_WIDTH,
				(groundPolygon[i].y - MAIN_TEXTURE_UV_TOP) / MAIN_TEXTURE_UV_HEIGHT
			);
		}

		float groundY = GameRules.instance.floor.GetComponent<Collider>().bounds.extents.y + MESH_HEIGHT_EPSILON;
		//build the vertex list for the collision mesh
		//also keep track of the center of the zone for the render mesh
		Vector3[] vertices = new Vector3[groundPolygon.Length * 2];
		Vector3[] displayVertices = new Vector3[groundPolygon.Length];
		for (int i = 0; i < groundPolygon.Length; i++) {
			float vx = groundPolygon[i].x;
			float vz = groundPolygon[i].y; //Vector2 stores x and y but we want to use z instead of y
			displayVertices[i] = new Vector3(vx, groundY, vz);
			vertices[i] = new Vector3(vx, groundY, vz);
			vertices[i + groundPolygon.Length] = new Vector3(vx, 100, vz);
		}
		displayMesh.vertices = displayVertices;
		displayMesh.uv = displayUVs;
		displayMesh.triangles = new int[] {0, 1, 2, 0, 2, 3};

		collisionMesh.vertices = vertices;
		collisionMesh.triangles = new int[] {0, 1, 2}; //this isn't actually used but meshes need an array of triangles
		GetComponent<MeshCollider>().sharedMesh = collisionMesh;
		GetComponent<MeshFilter>().mesh = displayMesh;
		//zones always render behind all sprites
		GetComponent<MeshRenderer>().sortingOrder = -32768;

		//now we need to build the mesh for the line
		groundY += MESH_HEIGHT_EPSILON;
		List<Vector3> lineVertices = new List<Vector3>();
		List<Vector2> lineUVs = new List<Vector2>();
		List<int> lineTriangles = new List<int>();
		//store the multiplier to get the right UV x-values; y goes from 0 to 1 but x is much smaller to accomodate the texture
		Transform zoneLine = transform.GetChild(0);
		MeshRenderer zoneLineRenderer = zoneLine.GetComponent<MeshRenderer>();
		Texture zoneLineTexture = zoneLineRenderer.material.mainTexture;
		float zoneLineUVXMultiplier = (float)zoneLineTexture.height / (zoneLineTexture.width * ZONE_LINE_HALF_WIDTH * 2);
		float zoneLineUVXPos = 0.0f;
		//build the mesh from the lines created by groundPolygon
		for (int i = 0; i < groundPolygon.Length; i++) {
			//first, add the triangles just for the line
			//find the line we'll be covering
			Vector2 point1 = groundPolygon[i];
			Vector2 point2 = groundPolygon[((i + 1) % groundPolygon.Length)];
			Vector2 lineVector = point2 - point1;
			float lineVectorLength = lineVector.magnitude;
			float extrudeMultiplier = ZONE_LINE_HALF_WIDTH / lineVectorLength;
			float extrudeX = -lineVector.y * extrudeMultiplier;
			float extrudeY = lineVector.x * extrudeMultiplier;
			//add them bottom to top, right to left
			lineVertices.Add(new Vector3(point1.x - extrudeX, groundY, point1.y - extrudeY));
			lineVertices.Add(new Vector3(point1.x + extrudeX, groundY, point1.y + extrudeY));
			lineVertices.Add(new Vector3(point2.x - extrudeX, groundY, point2.y - extrudeY));
			lineVertices.Add(new Vector3(point2.x + extrudeX, groundY, point2.y + extrudeY));
			//add UVS too
			lineUVs.Add(new Vector2(zoneLineUVXPos, 0.0f));
			lineUVs.Add(new Vector2(zoneLineUVXPos, 1.0f));
			zoneLineUVXPos += lineVectorLength * zoneLineUVXMultiplier;
			lineUVs.Add(new Vector2(zoneLineUVXPos, 0.0f));
			lineUVs.Add(new Vector2(zoneLineUVXPos, 1.0f));
			//add the triangles
			int vertexCount = lineVertices.Count;
			lineTriangles.AddRange(new int[] {
				vertexCount - 4, vertexCount - 3, vertexCount - 1,
				vertexCount - 4, vertexCount - 1, vertexCount - 2
			});

			//then, add the vertices, UVS, and triangles for the corner
			//we want to add a triangle every certain amount of degrees
			Vector2 point3 = groundPolygon[((i + 2) % groundPolygon.Length)];
			float cornerAngle = Vector2.Angle(lineVector, point3 - point2);
			int cornerSegments = Mathf.RoundToInt(cornerAngle / ZONE_LINE_CORNER_DEGREES);
			//go through and add stuff for the corners
			//to draw them, we do a double rotation- one for the inside point and one for the next outside point
			//then we just take a triangle out of the texture to fit on it
			//each rotation is slightly more than 180 degrees
			float rotateAngleRadians = (180.0f - cornerAngle / (cornerSegments * 2.0f)) * DEGREES_TO_RADIANS;
			for (int j = 0; j < cornerSegments; j++) {
				//rotate once for the inner point
				lineVertices.Add(rotate(point2, lineVertices[lineVertices.Count - 1], rotateAngleRadians));
				//rotate again for the outer point
				lineVertices.Add(rotate(point2, lineVertices[lineVertices.Count - 1], rotateAngleRadians));
				//add the UVs
				vertexCount = lineVertices.Count;
				float cornerEdgeUVLength =
					(lineVertices[vertexCount - 1] - lineVertices[vertexCount - 3]).magnitude * zoneLineUVXMultiplier;
				lineUVs.Add(new Vector2(zoneLineUVXPos + cornerEdgeUVLength * 0.5f, 0.0f));
				zoneLineUVXPos += cornerEdgeUVLength;
				lineUVs.Add(new Vector2(zoneLineUVXPos, 1.0f));
				//and finally add the triangle
				lineTriangles.AddRange(new int[] {
					vertexCount - 3, vertexCount - 1, vertexCount - 2
				});
			}
		}
		lineMesh.vertices = lineVertices.ToArray();
		lineMesh.uv = lineUVs.ToArray();
		lineMesh.triangles = lineTriangles.ToArray();
		zoneLine.GetComponent<MeshFilter>().mesh = lineMesh;
		//stick this one just after the zone bottom
		zoneLineRenderer.sortingOrder = -32767;
	}
	public static GameObject getIconForZoneType(GameRuleRequiredObjectType zoneType) {
		if (zoneType == GameRuleRequiredObjectType.BoomerangZone)
			return GameRuleIconStorage.instance.boomerangZoneIcon;
		else
			throw new System.Exception("Bug: could not get zone icon for " + zoneType);
	}
	public static Vector3 rotate(Vector2 center, Vector3 point, float angleRadians) {
		float rotateSin = Mathf.Sin(angleRadians);
		float rotateCos = Mathf.Cos(angleRadians);
		float diffX = point.x - center.x;
		float diffY = point.z - center.y;
		return new Vector3(
			diffX * rotateCos - diffY * rotateSin + center.x,
			point.y,
			diffX * rotateSin + diffY * rotateCos + center.y
		);
	}
}
