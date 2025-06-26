using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnerManager : MonoBehaviour
{
    public FishManager fishManager;
    public GameObject originPrefab;
    public int count;
    
    public void Start()
    {
        Application.targetFrameRate = 560;
        fishManager.fishMovements.Clear();
        for (var i = 0; i < count; i++)
        {
            var pos = new Vector2(Random.Range(-10f, 10f), Random.Range(-10f, 10f));
            var direction = Random.Range(-90, 90f);
            var rotation = Quaternion.Euler(0, 0, direction);
            if (Random.Range(0f, 1f) < 0.5f)
            {
                rotation = Quaternion.Euler(0, 180, direction);
            }
            var go = Instantiate(originPrefab, pos, rotation, transform);
            var fish = go.GetComponent<FishMovement>();
            fish.Init(fishManager);
            fishManager.fishMovements.Add(fish);
        }
    }
}