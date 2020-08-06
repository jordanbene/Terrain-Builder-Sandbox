using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Grid : MonoBehaviour
{
	private Mesh mesh;
	private Vector3[] vertices;
	public int xSize, zSize;
	public float noiseScale = 1;
	public bool autoUpdate;

	private void Awake()
	{
		Generate();

	}

	public void Generate()
	{
		
		GetComponent<MeshFilter>().mesh = mesh = new Mesh();
		mesh.name = "Procedural Grid";

		vertices = new Vector3[(xSize + 1) * (zSize + 1)];
		Vector2[] uv = new Vector2[vertices.Length];
		for (int i = 0, z = 0; z <= zSize; z++)
		{
			for (int x = 0; x <= xSize; x++, i++)
			{
				//float y = Mathf.PerlinNoise(x * noiseScale, z * noiseScale) * 2f;
				float y = 0f;
				vertices[i] = new Vector3(x, y, z);
				uv[i] = new Vector2((float)x / xSize, (float)z / zSize);
			}
		}
		mesh.vertices = vertices;
		mesh.uv = uv;


		int[] triangles = new int[xSize * zSize * 6];
		for (int ti = 0, vi = 0, z = 0; z < zSize; z++, vi++) //ti = triangle index vi = vertex index
		{
			for (int x = 0; x < xSize; x++, ti += 6, vi++)
			{
				triangles[ti] = vi;
				triangles[ti + 3] = triangles[ti + 2] = vi + 1;
				triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
				triangles[ti + 5] = vi + xSize + 2;
			}
		}
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
	}

	
}
