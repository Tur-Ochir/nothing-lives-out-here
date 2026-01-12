using System.Collections;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject prefab;
    public int spawnCount = 100;
    public float spawnDelay = 1;
    public float radius = 10;
    public float innerRadius = 5;
    void Start()
    {
        SpawnRandomCircle();
    }

    private IEnumerator SpawnSequence()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Spawn(transform.position);    
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private void Spawn(Vector3 pos)
    {
        var a = Instantiate(prefab, pos, Quaternion.identity, transform);
        a.GetComponent<Argal>().ActivateRandomChild();
    }

    private void SpawnRandomCircle()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 r = Random.insideUnitCircle * radius;
            r = r.normalized * Random.Range(innerRadius, radius);
            r.x += transform.position.x;
            r.y += transform.position.z;
            
            Spawn(new Vector3(r.x, 0.5f, r.y));
        }
    }
}
