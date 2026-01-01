using System.Collections;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject prefab;
    public int spawnCount = 100;
    public float spawnDelay = 1;
    void Start()
    {
        StartCoroutine(SpawnSequence());
    }

    private IEnumerator SpawnSequence()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Spawn();    
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private void Spawn()
    {
        Instantiate(prefab, transform.position, Quaternion.identity, transform);
    }
}
