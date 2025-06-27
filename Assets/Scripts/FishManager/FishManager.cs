using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class FishManager : MonoBehaviour
{
    public List<FishMovement> fishMovements = new();
    public FishMovementData fishMovementData;

    [Header("Grids")] [SerializeField] private float cellSize = 5f;
    [SerializeField] private float worldSizeX;
    [SerializeField] private float worldSizeY;
    [SerializeField] private int gridSizeX;
    [SerializeField] private int gridSizeY;

    private readonly Dictionary<Vector2Int, List<FishMovement>> _grid = new();

    private void Awake()
    {
        CalculateGridSize();
    }

    private void CalculateGridSize()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            worldSizeY = mainCam.orthographicSize * 2f;
            worldSizeX = worldSizeY * mainCam.aspect;
        }

        gridSizeX = Mathf.CeilToInt(worldSizeX / cellSize);
        gridSizeY = Mathf.CeilToInt(worldSizeY / cellSize);
    }

    public void RegisterFish(FishMovement fish)
    {
        Vector2Int gridPos = WorldToGridPosition(fish.transform.position);
        AddFishToGrid(fish, gridPos);
    }

    public void UnregisterFish(FishMovement fish)
    {
        Vector2Int gridPos = WorldToGridPosition(fish.transform.position);
        RemoveFishFromGrid(fish, gridPos);
    }

    public void UpdateFishPosition(FishMovement fish, Vector3 oldPosition)
    {
        Vector2Int oldGridPos = WorldToGridPosition(oldPosition);
        Vector2Int newGridPos = WorldToGridPosition(fish.transform.position);

        if (oldGridPos != newGridPos)
        {
            RemoveFishFromGrid(fish, oldGridPos);
            AddFishToGrid(fish, newGridPos);
        }
    }

    private void AddFishToGrid(FishMovement fish, Vector2Int gridPos)
    {
        if (!_grid.ContainsKey(gridPos))
        {
            _grid[gridPos] = new List<FishMovement>();
        }

        _grid[gridPos].Add(fish);
        fish.CurrentGridPosition = gridPos;
    }

    private void RemoveFishFromGrid(FishMovement fish, Vector2Int gridPos)
    {
        if (_grid.ContainsKey(gridPos))
        {
            _grid[gridPos].Remove(fish);
        }
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x + worldSizeX / 2f) / cellSize);
        int y = Mathf.FloorToInt((worldPosition.y + worldSizeY / 2f) / cellSize);
        return new Vector2Int(x, y);
    }

    public List<FishMovement> GetNearbyFishes(FishMovement fish, float radius, float visionAngle)
    {
        List<FishMovement> nearbyFishes = new List<FishMovement>();
        Vector2Int centerGrid = fish.CurrentGridPosition;
        float halfVisionRad = visionAngle * 0.5f * Mathf.Deg2Rad;
        float cosHalfVision = Mathf.Cos(halfVisionRad);
        Vector2 fishForward = fish.transform.right;

        int gridRadius = Mathf.CeilToInt(radius / cellSize);

        for (int x = centerGrid.x - gridRadius; x <= centerGrid.x + gridRadius; x++)
        {
            for (int y = centerGrid.y - gridRadius; y <= centerGrid.y + gridRadius; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                if (_grid.TryGetValue(gridPos, out var value))
                {
                    foreach (var otherFish in value)
                    {
                        if (otherFish == fish) continue;

                        Vector2 toOther = otherFish.transform.position - fish.transform.position;
                        float sqrDist = toOther.sqrMagnitude;
                        if (sqrDist > radius * radius) continue;
                        Vector2 dirToOther = toOther.normalized;
                        float dot = Vector2.Dot(fishForward, dirToOther);
                        if (dot < cosHalfVision) continue;

                        nearbyFishes.Add(otherFish);
                    }
                }
            }
        }

        return nearbyFishes;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        float drawWorldSizeX = worldSizeX;
        float drawWorldSizeY = worldSizeY;

        if (!Application.isPlaying)
        {
            Camera sceneCam = Camera.main;
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null) sceneCam = sceneView.camera;

            if (sceneCam != null && sceneCam.orthographic)
            {
                drawWorldSizeY = sceneCam.orthographicSize * 2f;
                drawWorldSizeX = drawWorldSizeY * sceneCam.aspect;
            }
        }
        if (drawWorldSizeX <= 0 || drawWorldSizeY <= 0) return;
        Gizmos.color = Color.cyan;
        int cellsX = Mathf.CeilToInt(drawWorldSizeX / cellSize);
        int cellsY = Mathf.CeilToInt(drawWorldSizeY / cellSize);
        for (int x = 0; x <= cellsX; x++)
        {
            float xPos = -drawWorldSizeX / 2 + x * cellSize;
            Vector3 start = new Vector3(xPos, -drawWorldSizeY / 2, 0);
            Vector3 end = new Vector3(xPos, drawWorldSizeY / 2, 0);
            Gizmos.DrawLine(start, end);
        }
        
        for (int y = 0; y <= cellsY; y++)
        {
            float yPos = -drawWorldSizeY / 2 + y * cellSize;
            Vector3 start = new Vector3(-drawWorldSizeX / 2, yPos, 0);
            Vector3 end = new Vector3(drawWorldSizeX / 2, yPos, 0);
            Gizmos.DrawLine(start, end);
        }
        
        Gizmos.color = Color.red;
        Vector3 bottomLeft = new Vector3(-drawWorldSizeX / 2, -drawWorldSizeY / 2, 0);
        Vector3 bottomRight = new Vector3(drawWorldSizeX / 2, -drawWorldSizeY / 2, 0);
        Vector3 topLeft = new Vector3(-drawWorldSizeX / 2, drawWorldSizeY / 2, 0);
        Vector3 topRight = new Vector3(drawWorldSizeX / 2, drawWorldSizeY / 2, 0);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
    }
#endif
}