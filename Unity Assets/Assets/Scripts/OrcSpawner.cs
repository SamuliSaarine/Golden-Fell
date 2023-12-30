using System.Collections;
using UnityEngine;

public class OrcSpawner : MonoBehaviour
{
    [SerializeField] GameObject orc;
    [SerializeField] int minInterval;
    [SerializeField] int maxInterval;

    public void GameStarted()
    {
        StartCoroutine(nameof(SpawnOrc));
    }

    IEnumerator SpawnOrc()
    {
        while(gameObject.activeSelf)
        {
            yield return new WaitForSeconds(Random.Range(minInterval,maxInterval));
            Instantiate(orc);
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
