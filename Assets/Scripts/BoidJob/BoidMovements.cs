using UnityEngine;

namespace BoidJob
{
    public class BoidMovements : MonoBehaviour
    {
        [SerializeField] private ListBoidVariable listBoidVariable;
        private float YLimit => Camera.main.orthographicSize + 1;
        private float XLimit => YLimit * Screen.width / Screen.height + 1;
        public float speed = 3f;
        public float radius = 2f;
        public float visionAngle = 270f;
        public float turnSpeed = 10f;

        private void FixedUpdate()
        {
            for (int i = 0; i < listBoidVariable.boidDatas.Count; i++)
            {
                listBoidVariable.boidDatas[i].position += listBoidVariable.boidDatas[i].velocity * speed * Time.fixedDeltaTime;
                listBoidVariable.matrices[i] = Matrix4x4.TRS(
                    listBoidVariable.boidDatas[i].position, listBoidVariable.boidDatas[i].rotation, listBoidVariable.prefabScale);
                CheckScreenBounds(ref listBoidVariable.boidDatas[i].position);
            }
        }
        
        private void CheckScreenBounds(ref Vector3 position)
        {
            Vector3 newPosition = position;
            bool wrapX = false;
            bool wrapY = false;

            if (position.x > XLimit)
            {
                newPosition.x = -XLimit + 0.1f;
                wrapX = true;
            }
            else if (position.x < -XLimit)
            {
                newPosition.x = XLimit - 0.1f;
                wrapX = true;
            }

            if (position.y > YLimit)
            {
                newPosition.y = -YLimit + 0.1f;
                wrapY = true;
            }
            else if (position.y < -YLimit)
            {
                newPosition.y = YLimit - 0.1f;
                wrapY = true;
            }

            if (wrapX || wrapY)
            {
                position = newPosition;
            }
        }
    }
}