using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns prefabs in a donut circle area and notifies ISpawnable components.
/// </summary>
public class Spawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject prefab;
    public int spawnCount = 100;
    public float spawnDelay = 1f;
    public float radius = 10f;
    public float innerRadius = 5f;

    private void Start()
    {
        SpawnRandomCircle();
    }

    public IEnumerator SpawnSequence()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Spawn(transform.position);    
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    public void Spawn(Vector3 pos)
    {
        if (prefab == null) return;

        var instance = Instantiate(prefab, pos, Quaternion.identity, transform);
        if (instance.TryGetComponent(out ISpawnable spawnable))
        {
            spawnable.OnSpawned();
        }
    }

    private void SpawnRandomCircle()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 circle = Random.insideUnitCircle;
            Vector2 offset = circle.normalized * Random.Range(innerRadius, radius);
            Vector3 spawnPos = new Vector3(transform.position.x + offset.x, 0.5f, transform.position.z + offset.y);

            Spawn(spawnPos);
        }
    }
}
