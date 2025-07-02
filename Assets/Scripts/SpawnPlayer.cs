using UnityEngine;

public class SpawnPlayer : MonoBehaviour
{
    public GameObject playerPrefab;
    public MapGenerator mapGenerator;

    void Start()
    {
        SpawnPlayerAtCenter();
    }

    private void SpawnPlayerAtCenter()
    {
        MapData mapData = mapGenerator.previewMapData;

        float rawHeight = mapData.heightMap[0, 0];
        float adjustedHeight = mapGenerator.meshHeightCurve.Evaluate(rawHeight) * mapGenerator.meshHeightMultiplier;

        Vector3 spawnPosition = new Vector3(0f, adjustedHeight + 1f, 0f);
        Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        Debug.Log($"Player spawned at: {spawnPosition} (height: {adjustedHeight})");
    }
}
