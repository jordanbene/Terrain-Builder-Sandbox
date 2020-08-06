using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class EndlessTerrain : MonoBehaviour
{
    const float scale = 5f;
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public LODInfo[] detailLevels;
    public Material mapMaterial;
    public static float maxViewDistance;
    public Transform viewer;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisableInViewDistance;

    Dictionary<Vector2, TerrainChunk> TerrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static  List<TerrainChunk> terrainChunksVisableLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        maxViewDistance =  detailLevels[detailLevels.Length - 1].visableDstThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisableInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);

        UpdateVisableChunks();

    }

    void Update()
    {

        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisableChunks();

        }
    }

    void UpdateVisableChunks()
    {
        for (int i = 0; i < terrainChunksVisableLastUpdate.Count; i++)
        {
            terrainChunksVisableLastUpdate[i].SetVisable(false);
        }
        terrainChunksVisableLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisableInViewDistance; yOffset <= chunksVisableInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisableInViewDistance; xOffset <= chunksVisableInViewDistance; xOffset++)
            {
                Vector2 viewedChunkecCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (TerrainChunkDictionary.ContainsKey(viewedChunkecCoord))
                {
                    TerrainChunkDictionary[viewedChunkecCoord].UpdateTerrainChunk();
                }
                else
                {
                    TerrainChunkDictionary.Add(viewedChunkecCoord, new TerrainChunk(viewedChunkecCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }
    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;
        MapData mapData;
        bool mapDataRecieved;

        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        int previousLODIndex = -1;
        public TerrainChunk(Vector2 coord, int size,LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;
            position = coord * size;

            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
           

            meshObject = new GameObject("Terrain Chunk");
            meshObject.transform.localScale = Vector3.one * scale;
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * scale;
           
            meshObject.transform.parent = parent;
            SetVisable(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i ++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }


            mapGenerator.RequestMapData(position, OnMapDataRecieved);
        }

        void OnMapDataRecieved(MapData mapData)
        {
            this.mapData = mapData;
            mapDataRecieved = true;
            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

       

        public void UpdateTerrainChunk()
        {
            if (mapDataRecieved)
            {
                float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visable = viewerDistanceFromNearestEdge <= maxViewDistance;

                if (visable)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDistanceFromNearestEdge > detailLevels[i].visableDstThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else break;
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                            meshCollider.sharedMesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                    terrainChunksVisableLastUpdate.Add(this);
                }

                SetVisable(visable);
            }
        }
        public void SetVisable(bool visable)
        {
            meshObject.SetActive(visable);
        }
        public bool IsVisable()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;

        }

        void OnMeshDataRecieved(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            updateCallback();
        }
        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataRecieved);
        }
    }
    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visableDstThreshold;
    }
}
