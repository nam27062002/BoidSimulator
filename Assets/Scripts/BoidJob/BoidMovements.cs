using System.Linq;
using UnityEngine;
using BoidJob.Data;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

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
        private JobHandle boidJobHandle;
        private NativeArray<Vector3> positionsRead, velocitiesRead;
        private NativeArray<Quaternion> rotationsRead;
        private NativeArray<Vector3> positionsWrite, velocitiesWrite;
        private NativeArray<Quaternion> rotationsWrite;
        private bool jobArraysInitialized = false;
        private NativeArray<int> gridCellStart;
        private NativeArray<int> gridCellEnd;
        private NativeArray<int> gridIndices;
        private int gridDimX, gridDimY;
        private float gridMinX, gridMinY;
        private float gridCellSize;
        private int gridCellCount;
        [SerializeField, Range(0, 3)] public float separationWeight = 0.8f;
        [SerializeField, Range(0, 3)] public float alignmentWeight = 1.2f;
        [SerializeField, Range(0, 3)] public float cohesionWeight = 0.8f;

        private void FixedUpdate()
        {
            int count = listBoidVariable.boidDatas.Count;
            EnsureJobArrays(count);
            // Copy dữ liệu từ ListBoidVariable sang NativeArray (ReadOnly)
            for (int i = 0; i < count; i++)
            {
                positionsRead[i] = listBoidVariable.boidDatas[i].position;
                velocitiesRead[i] = listBoidVariable.boidDatas[i].velocity;
                rotationsRead[i] = listBoidVariable.boidDatas[i].rotation;
            }

            // --- Xây dựng spatial grid ---
            BuildSpatialGrid(count);

            // Lấy giá trị Y/Z gốc của prefab
            float prefabY = listBoidVariable.prefabRotation.eulerAngles.y;
            float prefabZ = listBoidVariable.prefabRotation.eulerAngles.z;
            Quaternion prefabRotation = listBoidVariable.prefabRotation;

            // Tạo và chạy job song song
            var job = new BoidUpdateJob
            {
                positionsRead = positionsRead,
                velocitiesRead = velocitiesRead,
                rotationsRead = rotationsRead,
                positionsWrite = positionsWrite,
                velocitiesWrite = velocitiesWrite,
                rotationsWrite = rotationsWrite,
                speed = speed,
                radius = radius,
                visionAngle = visionAngle,
                turnSpeed = turnSpeed,
                deltaTime = Time.fixedDeltaTime,
                count = count,
                prefabY = prefabY,
                prefabZ = prefabZ,
                prefabRotation = prefabRotation,
                gridCellStart = gridCellStart,
                gridCellEnd = gridCellEnd,
                gridIndices = gridIndices,
                gridDimX = gridDimX,
                gridDimY = gridDimY,
                gridMinX = gridMinX,
                gridMinY = gridMinY,
                gridCellSize = gridCellSize,
                separationWeight = separationWeight,
                alignmentWeight = alignmentWeight,
                cohesionWeight = cohesionWeight
            };
            var handle = job.Schedule(count, 32);
            handle.Complete();

            // Copy kết quả về lại ListBoidVariable
            for (int i = 0; i < count; i++)
            {
                listBoidVariable.boidDatas[i].position = positionsWrite[i];
                listBoidVariable.boidDatas[i].velocity = velocitiesWrite[i];
                listBoidVariable.boidDatas[i].rotation = rotationsWrite[i];
                listBoidVariable.matrices[i] = Matrix4x4.TRS(
                    positionsWrite[i], rotationsWrite[i], listBoidVariable.prefabScale);
                CheckScreenBounds(ref listBoidVariable.boidDatas[i].position);
            }
        }

        private void BuildSpatialGrid(int count)
        {
            // Xác định bounds
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                var pos = positionsRead[i];
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.y > maxY) maxY = pos.y;
            }
            gridCellSize = radius * 1.2f;
            gridMinX = minX;
            gridMinY = minY;
            gridDimX = Mathf.CeilToInt((maxX - minX) / gridCellSize) + 1;
            gridDimY = Mathf.CeilToInt((maxY - minY) / gridCellSize) + 1;
            gridCellCount = gridDimX * gridDimY;

            // Khởi tạo mảng
            if (gridCellStart.IsCreated) gridCellStart.Dispose();
            if (gridCellEnd.IsCreated) gridCellEnd.Dispose();
            if (gridIndices.IsCreated) gridIndices.Dispose();
            gridCellStart = new NativeArray<int>(gridCellCount, Allocator.TempJob);
            gridCellEnd = new NativeArray<int>(gridCellCount, Allocator.TempJob);
            gridIndices = new NativeArray<int>(count, Allocator.TempJob);

            // Đếm số lượng boid trong mỗi cell
            var cellCounts = new int[gridCellCount];
            for (int i = 0; i < count; i++)
            {
                int cell = GetCellIndex(positionsRead[i]);
                cellCounts[cell]++;
            }
            // Tính toán start/end index cho mỗi cell
            int idx = 0;
            for (int i = 0; i < gridCellCount; i++)
            {
                gridCellStart[i] = idx;
                idx += cellCounts[i];
                gridCellEnd[i] = idx;
            }
            // Đặt lại cellCounts để dùng như con trỏ ghi
            for (int i = 0; i < gridCellCount; i++) cellCounts[i] = gridCellStart[i];
            // Gán index boid vào gridIndices
            for (int i = 0; i < count; i++)
            {
                int cell = GetCellIndex(positionsRead[i]);
                gridIndices[cellCounts[cell]++] = i;
            }
        }

        private int GetCellIndex(Vector3 pos)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt((pos.x - gridMinX) / gridCellSize), 0, gridDimX - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt((pos.y - gridMinY) / gridCellSize), 0, gridDimY - 1);
            return y * gridDimX + x;
        }

        private void OnDestroy()
        {
            if (jobArraysInitialized)
            {
                positionsRead.Dispose();
                velocitiesRead.Dispose();
                rotationsRead.Dispose();
                positionsWrite.Dispose();
                velocitiesWrite.Dispose();
                rotationsWrite.Dispose();
            }
            if (gridCellStart.IsCreated) gridCellStart.Dispose();
            if (gridCellEnd.IsCreated) gridCellEnd.Dispose();
            if (gridIndices.IsCreated) gridIndices.Dispose();
        }

        private void EnsureJobArrays(int count)
        {
            if (jobArraysInitialized && positionsRead.Length == count) return;
            if (jobArraysInitialized)
            {
                positionsRead.Dispose();
                velocitiesRead.Dispose();
                rotationsRead.Dispose();
                positionsWrite.Dispose();
                velocitiesWrite.Dispose();
                rotationsWrite.Dispose();
            }
            positionsRead = new NativeArray<Vector3>(count, Allocator.Persistent);
            velocitiesRead = new NativeArray<Vector3>(count, Allocator.Persistent);
            rotationsRead = new NativeArray<Quaternion>(count, Allocator.Persistent);
            positionsWrite = new NativeArray<Vector3>(count, Allocator.Persistent);
            velocitiesWrite = new NativeArray<Vector3>(count, Allocator.Persistent);
            rotationsWrite = new NativeArray<Quaternion>(count, Allocator.Persistent);
            jobArraysInitialized = true;
        }

        [BurstCompile]
        private struct BoidUpdateJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector3> positionsRead;
            [ReadOnly] public NativeArray<Vector3> velocitiesRead;
            [ReadOnly] public NativeArray<Quaternion> rotationsRead;
            public NativeArray<Vector3> positionsWrite;
            public NativeArray<Vector3> velocitiesWrite;
            public NativeArray<Quaternion> rotationsWrite;
            public float speed;
            public float radius;
            public float visionAngle;
            public float turnSpeed;
            public float deltaTime;
            public int count;
            public float prefabY;
            public float prefabZ;
            public Quaternion prefabRotation;
            [ReadOnly] public NativeArray<int> gridCellStart;
            [ReadOnly] public NativeArray<int> gridCellEnd;
            [ReadOnly] public NativeArray<int> gridIndices;
            public int gridDimX;
            public int gridDimY;
            public float gridMinX;
            public float gridMinY;
            public float gridCellSize;
            public float separationWeight;
            public float alignmentWeight;
            public float cohesionWeight;

            public void Execute(int index)
            {
                var selfPos = positionsRead[index];
                var selfRot = rotationsRead[index];
                var selfVel = velocitiesRead[index];
                var neighbors = new NativeList<int>(Allocator.Temp);
                var forward = selfRot * Vector3.forward;
                float cosHalfVision = Mathf.Cos((visionAngle * 0.5f) * Mathf.Deg2Rad);
                float radiusSqr = radius * radius;
                // Xác định cell của mình
                int cellX = Mathf.Clamp((int)((selfPos.x - gridMinX) / gridCellSize), 0, gridDimX - 1);
                int cellY = Mathf.Clamp((int)((selfPos.y - gridMinY) / gridCellSize), 0, gridDimY - 1);
                // Duyệt các cell lân cận (3x3)
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = cellX + dx;
                        int ny = cellY + dy;
                        if (nx < 0 || nx >= gridDimX || ny < 0 || ny >= gridDimY) continue;
                        int ncell = ny * gridDimX + nx;
                        int start = gridCellStart[ncell];
                        int end = gridCellEnd[ncell];
                        for (int i = start; i < end; i++)
                        {
                            int boidIdx = gridIndices[i];
                            if (boidIdx == index) continue;
                            var otherPos = positionsRead[boidIdx];
                            var dir = (otherPos - selfPos).normalized;
                            float distSqr = math.lengthsq(selfPos - otherPos);
                            float dot = Vector3.Dot(forward, dir);
                            if (distSqr <= radiusSqr && dot >= cosHalfVision)
                                neighbors.Add(boidIdx);
                        }
                    }
                // Tính toán các lực
                Vector3 sep = Vector3.zero;
                Vector3 ali = Vector3.zero;
                Vector3 coh = Vector3.zero;
                for (int n = 0; n < neighbors.Length; n++)
                {
                    int i = neighbors[n];
                    float ratio = math.saturate(math.sqrt(math.lengthsq(positionsRead[i] - selfPos)) / radius);
                    sep -= ratio * (positionsRead[i] - selfPos);
                    ali += velocitiesRead[i];
                    coh += positionsRead[i];
                }
                if (neighbors.Length > 0)
                {
                    ali /= neighbors.Length;
                    coh /= neighbors.Length;
                }
                else
                {
                    coh = selfPos;
                }
                sep = sep.normalized;
                ali = ali.normalized;
                coh = (coh - selfPos).normalized;
                // Tính velocity mới
                Vector3 targetVel = (forward
                    + separationWeight * sep
                    + alignmentWeight * ali
                    + cohesionWeight * coh
                ).normalized * speed;
                Vector3 newVel = Vector3.Lerp(selfVel, targetVel, turnSpeed / 2 * deltaTime);
                Vector3 newPos = selfPos + newVel * deltaTime;
                Quaternion newRot = rotationsRead[index];
                if (newVel.sqrMagnitude > 0.0001f)
                {
                    float angle = math.degrees(math.atan2(newVel.y, newVel.x));
                    Quaternion targetRot = Quaternion.Euler(0, 0, angle) * prefabRotation;
                    newRot = Quaternion.Slerp(newRot, targetRot, turnSpeed * deltaTime);
                }
                positionsWrite[index] = newPos;
                velocitiesWrite[index] = newVel;
                rotationsWrite[index] = newRot;
                neighbors.Dispose();
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