using System.Linq;
using UnityEngine;

namespace Normal
{
    public class BoidMovement : MonoBehaviour
    {
        [SerializeField] private ListBoidVariable listBoidVariable;
        public float speed = 3f;
        public float radius = 2f;
        public float visionAngle = 270f;
        public float turnSpeed = 10f;
        private Vector3 Velocity { get; set; }
        
        private void FixedUpdate()
        {
            Velocity = Vector2.Lerp(Velocity, CalculateVelocity(),turnSpeed / 2 * Time.fixedDeltaTime);
            transform.position += Velocity * Time.fixedDeltaTime;
            LookRotation();
        }

        private Vector3 CalculateVelocity()
        {
            var boidsInRange = GetBoidsInRange();
            var velocity = ((Vector2)transform.forward 
                            + 1.7f * Separation(boidsInRange)
                            + 0.1f * Alignment(boidsInRange)
                            + Cohesion(boidsInRange)).normalized * speed;
            return velocity;
        }

        private void LookRotation()
        {
            transform.rotation = Quaternion.Slerp(transform.localRotation, Quaternion.LookRotation(Velocity), turnSpeed * Time.fixedDeltaTime);
        }

        private BoidMovement[] GetBoidsInRange()
        {
            return listBoidVariable.boidMovements.Where(boid =>
                Vector3.Distance(transform.position, boid.transform.position) <= radius && IsInVisionCone(boid.transform.position)).ToArray();
        }

        private bool IsInVisionCone(Vector3 position)
        {
            var cosHalfVision = Mathf.Cos((visionAngle * 0.5f) * Mathf.Deg2Rad);
            var directionToTarget = (position - transform.position).normalized;
            var dot = Vector3.Dot(transform.forward, directionToTarget);
            return dot >= cosHalfVision;
        }

        private Vector2 Separation(BoidMovement[] neighbors)
        {
            Vector2 direction = Vector2.zero;
            foreach (var boid in neighbors)
            {
                float ratio = Mathf.Clamp01(Vector3.Distance(boid.transform.position, transform.position) / radius);
                direction -= ratio * (Vector2)(boid.transform.position - transform.position);
            }
            return direction.normalized;
        }

        private static Vector2 Alignment(BoidMovement[] neighbors)
        {
            var direction = Vector2.zero;
            var boidMovements = neighbors.ToList();
            direction = boidMovements.Aggregate(direction, (current, boid) => current + (Vector2)boid.Velocity);
            if (boidMovements.Count() != 0) direction /= boidMovements.Count();
            return direction.normalized;
        }

        private Vector2 Cohesion(BoidMovement[] neighbors)
        {
            var center = neighbors.Aggregate(Vector2.zero, (current, boid) => current + (Vector2)boid.transform.position);
            if (neighbors.Count() != 0) center /= neighbors.Count();
            else center = transform.position;
            var direction = center - (Vector2)transform.position;
            return direction.normalized;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + Velocity);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.color = Color.yellow;
            var boidsInRange = GetBoidsInRange();
            foreach (var boid in boidsInRange)
            {
                Gizmos.DrawLine(transform.position, boid.transform.position);
            }
        }
    }
}