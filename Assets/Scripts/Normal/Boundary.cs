using UnityEngine;

namespace Normal
{
    public class Boundary : MonoBehaviour
    {
        private float YLimit => Camera.main.orthographicSize + 1;
        private float XLimit => YLimit * Screen.width / Screen.height + 1;

        private void Update()
        {
            CheckScreenBounds();
        }

        private void CheckScreenBounds()
        {
            Vector3 newPosition = transform.position;
            bool wrapX = false;
            bool wrapY = false;

            if (transform.position.x > XLimit)
            {
                newPosition.x = -XLimit + 0.1f;
                wrapX = true;
            }
            else if (transform.position.x < -XLimit)
            {
                newPosition.x = XLimit - 0.1f;
                wrapX = true;
            }

            if (transform.position.y > YLimit)
            {
                newPosition.y = -YLimit + 0.1f;
                wrapY = true;
            }
            else if (transform.position.y < -YLimit)
            {
                newPosition.y = YLimit - 0.1f;
                wrapY = true;
            }

            if (wrapX || wrapY)
            {
                transform.position = newPosition;
            }
        }
    }
}