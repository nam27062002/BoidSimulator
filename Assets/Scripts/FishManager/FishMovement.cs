using System.Collections.Generic;
using UnityEngine;

public class FishMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float radius = 2f;
    [SerializeField] private float visionAngle = 270f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private float maxAcceleration = 10f;
    
    [Header("Group Behavior")]
    [SerializeField] private float separationWeight = 0.8f;
    [SerializeField] private float alignmentWeight = 0.3f;
    [SerializeField] private float cohesionWeight = 0.2f;
    [SerializeField] private float independence = 0.4f;
    [SerializeField] private int maxGroupSize = 5;
    
    [Header("Perturbation")]
    [SerializeField] private float perturbationIntensity = 0.5f;
    [SerializeField] private float perturbationFrequency = 0.3f;
    [SerializeField] private float perturbationDuration = 1f;
    
    private Camera _mainCamera;
    private float YLimit => _mainCamera.orthographicSize + 1;
    private float XLimit => YLimit * Screen.width / Screen.height + 1;
    private Vector3 Velocity { get; set; }
    private FishManager _fishManager;
    private Vector2 _smoothDampVelocity;
    private float _nextPerturbTime;
    private Vector2 _currentPerturbation;
    private float _perturbationEndTime;
    private float _personalSpace; 

    public void Init(FishManager fishManager)
    {
        var fishMovementData = fishManager.fishMovementData;
        _fishManager = fishManager;
        speed = fishMovementData.speed;
        radius = fishMovementData.radius;
        visionAngle = fishMovementData.visionAngle;
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Velocity = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * speed * 0.5f;
        _personalSpace = Random.Range(0.7f, 1.3f);
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
            _mainCamera = FindFirstObjectByType<Camera>();
    }

    private void FixedUpdate()
    {
        UpdatePerturbation();
        Vector2 targetVelocity = CalculateVelocity();
        Velocity = Vector2.SmoothDamp(
            Velocity, 
            targetVelocity, 
            ref _smoothDampVelocity, 
            smoothTime,
            maxAcceleration,
            Time.fixedDeltaTime
        );
        
        transform.position += Velocity * Time.fixedDeltaTime;
        CheckScreenBounds();
        LookRotation();
    }

    private void UpdatePerturbation()
    {
        if (Time.time > _nextPerturbTime && Random.value < perturbationFrequency)
        {
            _currentPerturbation = Random.insideUnitCircle * perturbationIntensity;
            _perturbationEndTime = Time.time + perturbationDuration;
            _nextPerturbTime = Time.time + Random.Range(1f, 3f);
        }
        
        if (Time.time < _perturbationEndTime)
        {
            _currentPerturbation = Vector2.Lerp(_currentPerturbation, Vector2.zero, 
                (perturbationDuration - (_perturbationEndTime - Time.time)) / perturbationDuration);
        }
        else
        {
            _currentPerturbation = Vector2.zero;
        }
    }

    private void LookRotation()
    {
        if (Velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(Velocity.y, Velocity.x) * Mathf.Rad2Deg;
            bool shouldFlip = angle > 90f && angle < 270f;
            Vector3 scale = transform.localScale;
            if (shouldFlip && scale.y > 0)
            {
                scale.y = -Mathf.Abs(scale.y);
                transform.localScale = scale;
            }
            else if (!shouldFlip && scale.y < 0)
            {
                scale.y = Mathf.Abs(scale.y);
                transform.localScale = scale;
            }
            if (shouldFlip)
            {
                angle = (angle + 180f) % 360f;
            }
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                turnSpeed * Time.fixedDeltaTime
            );
        }
    }

    private Vector2 CalculateVelocity()
    {
        List<FishMovement> fishesInRange = FishesInRange();
        Vector2 separationForce = Separation(fishesInRange) * separationWeight;
        Vector2 alignmentForce = Alignment(fishesInRange) * alignmentWeight;
        Vector2 cohesionForce = Cohesion(fishesInRange) * cohesionWeight;
        Vector2 independentDirection = transform.right;
        if (fishesInRange.Count > maxGroupSize)
        {
            independentDirection = (independentDirection + separationForce * 2f).normalized;
        }
        
        Vector2 combinedForce = (
            independentDirection * (1f - independence) + 
            (separationForce + alignmentForce + cohesionForce) * independence +
            _currentPerturbation
        ).normalized;
        
        return combinedForce * speed;
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

    public List<FishMovement> FishesInRange()
    {
        float sqrRadius = radius * radius;
        return _fishManager.fishMovements.FindAll(fish =>
            this != fish && 
            (transform.position - fish.transform.position).sqrMagnitude < sqrRadius &&
            InVisionCone(fish.transform.position)
        );
    }

    private bool InVisionCone(Vector2 position)
    {
        Vector2 directionToPosition = position - (Vector2)transform.position;
        float angle = Vector2.Angle(transform.right, directionToPosition);
        return angle <= visionAngle * 0.5f;
    }

    private Vector2 Separation(List<FishMovement> neighbors)
    {
        if (neighbors.Count == 0) return Vector2.zero;
        
        Vector2 steer = Vector2.zero;
        int count = 0;
        
        foreach (var fish in neighbors)
        {
            Vector2 diff = transform.position - fish.transform.position;
            float distance = diff.magnitude;
            
            float personalSpaceFactor = _personalSpace * fish._personalSpace;
            float desiredDistance = personalSpaceFactor * 0.5f;
            
            if (distance < desiredDistance)
            {
                float strength = Mathf.Clamp01(1.0f - distance / desiredDistance);
                steer += diff.normalized * strength;
                count++;
            }
        }
        
        if (count > 0)
            steer /= count;
        
        return steer;
    }

    private Vector2 Alignment(List<FishMovement> neighbors)
    {
        if (neighbors.Count == 0 || neighbors.Count > maxGroupSize) 
            return Vector2.zero;
        
        Vector2 avgDirection = Vector2.zero;
        foreach (var fish in neighbors)
        {
            avgDirection += (Vector2)fish.transform.right;
        }
        
        return avgDirection.normalized;
    }

    private Vector2 Cohesion(List<FishMovement> neighbors)
    {
        if (neighbors.Count == 0 || neighbors.Count > maxGroupSize) 
            return Vector2.zero;
        
        Vector2 centerOfMass = Vector2.zero;
        foreach (var fish in neighbors)
        {
            centerOfMass += (Vector2)fish.transform.position;
        }
        centerOfMass /= neighbors.Count;
        
        return (centerOfMass - (Vector2)transform.position).normalized;
    }
}