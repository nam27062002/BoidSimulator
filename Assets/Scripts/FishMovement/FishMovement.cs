using UnityEngine;

public class FishMovement : MonoBehaviour
{
    public float speed = 5f;
    private Camera _mainCamera;
    private float YLimit => Camera.main.orthographicSize + 1;
    private float XLimit => YLimit * Screen.width / Screen.height + 1;
    private void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
            _mainCamera = FindFirstObjectByType<Camera>();
    }
    
    private void FixedUpdate()
    {
        transform.position += transform.right * speed * Time.fixedDeltaTime;
        CheckScreenBounds();
    }
    
    private void CheckScreenBounds()
    {
        if (Mathf.Abs(transform.position.x) > XLimit)
        {
            transform.position = transform.position.x > 0 ? new Vector3(-XLimit, transform.position.y, transform.position.z) : new Vector3(XLimit, transform.position.y, transform.position.z);
        }

        if (Mathf.Abs(transform.position.y) > YLimit)
        {
            transform.position = transform.position.y > 0 ? new Vector3(transform.position.x, -YLimit, transform.position.z) : new Vector3(transform.position.x, YLimit, transform.position.z);
        }
    }
}