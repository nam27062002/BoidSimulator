using System;
using UnityEngine;

[CreateAssetMenu(fileName = "FishMovementData", menuName = "ScriptableObjects/FishMovementData", order = 1)]
public class FishMovementData : ScriptableObject
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float radius = 2f;
    public float visionAngle = 270f;
    public float turnSpeed = 10f;
    public float smoothTime = 0.1f;
    public float maxAcceleration = 10f;

    [Header("Group Behavior")]
    public float separationWeight = 0.8f;
    public float alignmentWeight = 0.3f;
    public float cohesionWeight = 0.2f;
    public float independence = 0.4f;
    public int maxGroupSize = 5;

    [Header("Perturbation")]
    public float perturbationIntensity = 0.5f;
    public float perturbationFrequency = 0.3f;
    public float perturbationDuration = 1f;

}