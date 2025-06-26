using System.Collections.Generic;
using UnityEngine;

public class FishMovement : MonoBehaviour
{
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
    public Vector2Int CurrentGridPosition { get; set; }
    private Vector3 _lastPosition;
    
    private FishMovementData _fishMovementData;
    public void Init(FishManager fishManager)
    {
        _fishMovementData = fishManager.fishMovementData;
        _fishManager = fishManager;
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Velocity = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * _fishMovementData.speed * 0.5f;
        _personalSpace = Random.Range(0.7f, 1.3f);
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
            _mainCamera = FindFirstObjectByType<Camera>();
        
        _fishManager.RegisterFish(this);
        _lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (transform.position != _lastPosition)
        {
            _fishManager.UpdateFishPosition(this, _lastPosition);
            _lastPosition = transform.position;
        }
        UpdatePerturbation();
        Vector2 targetVelocity = CalculateVelocity();
        Velocity = Vector2.SmoothDamp(
            Velocity, 
            targetVelocity, 
            ref _smoothDampVelocity, 
            _fishMovementData.smoothTime,
            _fishMovementData.maxAcceleration,
            Time.fixedDeltaTime
        );
        
        transform.position += Velocity * Time.fixedDeltaTime;
        CheckScreenBounds();
        LookRotation();
    }

    private void OnDestroy()
    {
        if (_fishManager != null)
            _fishManager.UnregisterFish(this);
    }

    private void UpdatePerturbation()
    {
        if (Time.time > _nextPerturbTime && Random.value < _fishMovementData.perturbationFrequency)
        {
            _currentPerturbation = Random.insideUnitCircle * _fishMovementData.perturbationIntensity;
            _perturbationEndTime = Time.time + _fishMovementData.perturbationDuration;
            _nextPerturbTime = Time.time + Random.Range(1f, 3f);
        }
        
        if (Time.time < _perturbationEndTime)
        {
            _currentPerturbation = Vector2.Lerp(_currentPerturbation, Vector2.zero, 
                (_fishMovementData.perturbationDuration - (_perturbationEndTime - Time.time)) / _fishMovementData.perturbationDuration);
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
                _fishMovementData.turnSpeed * Time.fixedDeltaTime
            );
        }
    }

    private Vector2 CalculateVelocity()
    {
        List<FishMovement> fishesInRange = FishesInRange();
        Vector2 separationForce = Separation(fishesInRange) * _fishMovementData.separationWeight;
        Vector2 alignmentForce = Alignment(fishesInRange) * _fishMovementData.alignmentWeight;
        Vector2 cohesionForce = Cohesion(fishesInRange) * _fishMovementData.cohesionWeight;
        Vector2 independentDirection = transform.right;
        if (fishesInRange.Count > _fishMovementData.maxGroupSize)
        {
            independentDirection = (independentDirection + separationForce * 2f).normalized;
        }
        
        Vector2 combinedForce = (
            independentDirection * (1f - _fishMovementData.independence) + 
            (separationForce + alignmentForce + cohesionForce) * _fishMovementData.independence +
            _currentPerturbation
        ).normalized;
        
        return combinedForce * _fishMovementData.speed;
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
        return _fishManager.GetNearbyFishes(this, _fishMovementData.radius, _fishMovementData.visionAngle);
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
        if (neighbors.Count == 0 || neighbors.Count > _fishMovementData.maxGroupSize) 
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
        if (neighbors.Count == 0 || neighbors.Count > _fishMovementData.maxGroupSize) 
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