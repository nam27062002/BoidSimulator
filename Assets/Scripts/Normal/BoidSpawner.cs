using UnityEngine;
namespace Normal
{
    public class BoidSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int boidCount;
        [SerializeField] private ListBoidVariable listBoidVariable;
        private float _minX, _maxX, _minY, _maxY;

        public void Awake()
        {
#if UNITY_EDITOR
            boidCount = 100;
#endif
            listBoidVariable.boidMovements.Clear();
            var cam = Camera.main;
            if (cam != null)
            {
                var bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
                var topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));

                _minX = bottomLeft.x;
                _maxX = topRight.x;
                _minY = bottomLeft.y;
                _maxY = topRight.y;
            }

            for (var i = 0; i < boidCount; i++)
            {
                var pos = new Vector2(Random.Range(_minX, _maxX), Random.Range(_minY, _maxY));
                float direction = Random.Range(0f, 360f);
                GameObject boid = Instantiate(prefab, pos, Quaternion.Euler(Vector3.forward * direction) * prefab.transform.localRotation, transform);
                listBoidVariable.boidMovements.Add(boid.GetComponent<BoidMovement>());
            }
        }
    }
}