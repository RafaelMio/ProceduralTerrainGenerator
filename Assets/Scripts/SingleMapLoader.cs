using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class SingleMapLoader : MonoBehaviour
{
    public MapGenerator mapGenerator;
    public Material mapMaterial;
    public GameObject center;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private LayerMask groundLayer;

    private void Start()
    {
        mapGenerator.RequestMapData(Vector2.zero, OnMapDataReceived);
    }

    private void Update()
    {
        Debug.DrawRay(center.transform.position, Vector3.down * 200, Color.red);
    }

    private void OnMapDataReceived(MapData mapData)
    {
        GameObject meshObject = new GameObject("GeneratedMap");
        meshObject.transform.position = Vector3.zero;
        meshObject.transform.localScale = Vector3.one * 5f;
        meshObject.layer = 6; // Ground layer

        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.material = mapMaterial;
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();

        mapGenerator.RequestMeshData(mapData, 0, meshData =>
        {
            // Créer le mesh
            Mesh generatedMesh = meshData.CreateMesh();

            // Assigner le mesh au MeshFilter
            meshFilter.mesh = generatedMesh;

            // IMPORTANT : Assigner le MÊME mesh au MeshCollider
            MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = generatedMesh; // Utilise sharedMesh, pas mesh
            meshCollider.convex = false;

            // Texture
            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            // Spawner le joueur APRÈS que le MeshCollider soit configuré
            SpawnPlayerAtCenter();
        });
    }

    private void SpawnPlayerAtCenter()
    {
        StartCoroutine(SpawnPlayerCoroutine());
    }

    private System.Collections.IEnumerator SpawnPlayerCoroutine()
    {
        // Attendre plusieurs frames pour être sûr
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // Forcer la physique à se mettre à jour
        Physics.SyncTransforms();

        RaycastHit hit;
        Vector3 rayOrigin = center.transform.position;
        Vector3 rayDirection = Vector3.down;
        float rayDistance = 200f;

        Debug.Log($"Raycast from: {rayOrigin} direction: {rayDirection}");
        DebugMeshCollider();

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, groundLayer))
        {
            Debug.Log($"Hit found at: {hit.point}");
            Instantiate(playerPrefab, hit.point + Vector3.up * 1.5f, Quaternion.identity);
        }
        else
        {
            Debug.Log("No hit detected - trying alternative methods");
            TryAlternativeSpawn();
        }
    }

    private void TryAlternativeSpawn()
    {
        RaycastHit hit;
        Vector3 rayOrigin = center.transform.position;

        // Test 1: Sans LayerMask
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 200f))
        {
            Debug.Log($"Hit WITHOUT LayerMask at: {hit.point} on layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            Instantiate(playerPrefab, hit.point + Vector3.up * 1.5f, Quaternion.identity);
            return;
        }

        Debug.Log("No hit even WITHOUT LayerMask - using heightMap fallback");

        // Test 2: Utiliser les données de heightMap (accepter les hauteurs négatives)
        float terrainHeight = GetTerrainHeightAtPosition(Vector3.zero);
        Vector3 spawnPos = new Vector3(0, terrainHeight + 1.5f, 0);
        Debug.Log($"Spawning player at calculated height: {spawnPos}");
        Instantiate(playerPrefab, spawnPos, Quaternion.identity);
    }

    private float GetTerrainHeightAtPosition(Vector3 worldPosition)
    {
        // Récupérer les données de map depuis le MapGenerator
        if (mapGenerator.previewMapData.heightMap != null)
        {
            float[,] heightMap = mapGenerator.previewMapData.heightMap;

            // Conversion de la position world vers les coordonnées de la heightMap
            // Prendre en compte le scale de 5x
            float scaleAdjustment = 5f; // Ton transform.localScale

            // Convertir la position world en coordonnées de heightMap (0 à mapChunkSize-1)
            int mapSize = MapGenerator.mapChunkSize;

            // Le terrain est centré sur (0,0,0), donc on doit ajuster
            float normalizedX = (worldPosition.x / scaleAdjustment) + (mapSize / 2f);
            float normalizedZ = (worldPosition.z / scaleAdjustment) + (mapSize / 2f);

            // Clamper pour éviter les index out of bounds
            int x = Mathf.Clamp(Mathf.RoundToInt(normalizedX), 0, mapSize - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt(normalizedZ), 0, mapSize - 1);

            // Récupérer la hauteur normalisée (0-1) et la convertir en hauteur world
            float normalizedHeight = heightMap[x, z];
            float worldHeight = normalizedHeight * mapGenerator.meshHeightMultiplier * scaleAdjustment;

            // Appliquer la courbe de hauteur si elle existe
            if (mapGenerator.meshHeightCurve != null && mapGenerator.meshHeightCurve.keys.Length > 0)
            {
                worldHeight = mapGenerator.meshHeightCurve.Evaluate(normalizedHeight) * mapGenerator.meshHeightMultiplier * scaleAdjustment;
            }

            Debug.Log($"Height at position {worldPosition}: {worldHeight} (normalized: {normalizedHeight})");
            return worldHeight;
        }

        Debug.LogWarning("No heightMap data available, returning default height");
        return 0f;
    }

    private void SpawnPlayerRandomly()
    {
        // Récupérer tous les points valides à l'altitude 0
        List<Vector3> validSpawnPoints = GetValidSpawnPoints();

        if (validSpawnPoints.Count > 0)
        {
            // Choisir un point aléatoire
            int randomIndex = UnityEngine.Random.Range(0, validSpawnPoints.Count);
            Vector3 spawnPosition = validSpawnPoints[randomIndex];

            Debug.Log($"Spawning player at random position: {spawnPosition} (from {validSpawnPoints.Count} valid points)");
            Instantiate(playerPrefab, spawnPosition + Vector3.up * 1.5f, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("No valid spawn points found at altitude 0, using fallback");
            SpawnPlayerAtCenter(); // Fallback vers ton ancienne méthode
        }
    }

    private List<Vector3> GetValidSpawnPoints()
    {
        List<Vector3> validPoints = new List<Vector3>();

        if (mapGenerator.previewMapData.heightMap != null)
        {
            float[,] heightMap = mapGenerator.previewMapData.heightMap;
            int mapSize = MapGenerator.mapChunkSize;
            float scaleAdjustment = 5f; // Ton transform.localScale

            // Tolérance pour considérer qu'un point est à altitude 0
            float altitudeTolerance = 0.1f;

            // Parcourir la heightMap avec un pas pour optimiser (tous les 4 points par exemple)
            int step = 4; // Ajuste selon tes besoins de précision vs performance

            for (int x = 0; x < mapSize; x += step)
            {
                for (int z = 0; z < mapSize; z += step)
                {
                    float normalizedHeight = heightMap[x, z];
                    float worldHeight = CalculateWorldHeight(normalizedHeight);

                    // Vérifier si l'altitude est proche de 0
                    if (Mathf.Abs(worldHeight) <= altitudeTolerance)
                    {
                        // Convertir les coordonnées de heightMap vers world space
                        Vector3 worldPosition = HeightMapToWorldPosition(x, z, worldHeight, mapSize, scaleAdjustment);
                        validPoints.Add(worldPosition);
                    }
                }
            }
        }

        Debug.Log($"Found {validPoints.Count} valid spawn points at altitude ~0");
        return validPoints;
    }
    private float CalculateWorldHeight(float normalizedHeight)
    {
        float worldHeight = normalizedHeight * mapGenerator.meshHeightMultiplier * 5f; // scale adjustment

        // Appliquer la courbe de hauteur si elle existe
        if (mapGenerator.meshHeightCurve != null && mapGenerator.meshHeightCurve.keys.Length > 0)
        {
            worldHeight = mapGenerator.meshHeightCurve.Evaluate(normalizedHeight) * mapGenerator.meshHeightMultiplier * 5f;
        }

        return worldHeight;
    }

    private Vector3 HeightMapToWorldPosition(int mapX, int mapZ, float worldHeight, int mapSize, float scaleAdjustment)
    {
        // Convertir les coordonnées de heightMap (0 à mapSize-1) vers world space
        // Le terrain est centré sur (0,0,0)
        float worldX = (mapX - mapSize / 2f) * scaleAdjustment;
        float worldZ = (mapZ - mapSize / 2f) * scaleAdjustment;

        return new Vector3(worldX, worldHeight, worldZ);
    }

    private void DebugMeshCollider()
    {
        GameObject terrainObject = GameObject.Find("GeneratedMap");
        if (terrainObject != null)
        {
            MeshCollider meshCollider = terrainObject.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                Debug.Log($"MeshCollider found:");
                Debug.Log($"  - Enabled: {meshCollider.enabled}");
                Debug.Log($"  - Mesh assigned: {meshCollider.sharedMesh != null}");
                Debug.Log($"  - Mesh vertex count: {(meshCollider.sharedMesh ? meshCollider.sharedMesh.vertexCount : 0)}");
                Debug.Log($"  - Is Trigger: {meshCollider.isTrigger}");
                Debug.Log($"  - Convex: {meshCollider.convex}");
                Debug.Log($"  - Bounds: {meshCollider.bounds}");
            }
            else
            {
                Debug.LogError("No MeshCollider found on terrain object!");
            }
        }
        else
        {
            Debug.LogError("No terrain object found!");
        }
    }
}
