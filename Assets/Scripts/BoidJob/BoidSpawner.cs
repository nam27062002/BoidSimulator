using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace BoidJob
{
    public class BoidSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int boidCount = 100;
        [SerializeField] private ListBoidVariable listBoidVariable;
        private Mesh _boidMesh;
        private Material _boidMaterial;
        private MaterialPropertyBlock _propertyBlock;
        private float _minX, _maxX, _minY, _maxY;

        private void Awake()
        {
            Application.targetFrameRate = 10000;
        }

        private void Start()
        {
            InitializeScreenBounds();
            InitializePrefabData();
            InitializeInstancingComponents();
            InitializeBoids();
        }

        private void Update()
        {
            RenderBoids();
        }

        private void InitializeScreenBounds()
        {
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
        }

        private void InitializePrefabData()
        {
            listBoidVariable.prefabScale = prefab.transform.localScale;
            listBoidVariable.prefabRotation = prefab.transform.rotation;
            listBoidVariable.prefabUp = prefab.transform.up;
        }

        private void InitializeInstancingComponents()
        {
            var meshFilter = prefab.GetComponentInChildren<MeshFilter>();
            var meshRenderer = prefab.GetComponentInChildren<MeshRenderer>();

            _boidMesh = meshFilter.sharedMesh;
            _boidMaterial = new Material(meshRenderer.sharedMaterial);
            _boidMaterial.enableInstancing = true;

            listBoidVariable.matrices = new Matrix4x4[boidCount];
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void InitializeBoids()
        {
            listBoidVariable.boidDatas.Clear();
            for (int i = 0; i < boidCount; i++)
            {
                listBoidVariable.boidDatas.Add(new BoidData());
                listBoidVariable.boidDatas[i].position = new Vector3(
                    Random.Range(_minX, _maxX),
                    Random.Range(_minY, _maxY),
                    0
                );

                float direction = Random.Range(0f, 360f);
                listBoidVariable.boidDatas[i].rotation = Quaternion.Euler(Vector3.forward * direction) * prefab.transform.localRotation;
                listBoidVariable.boidDatas[i].velocity = listBoidVariable.boidDatas[i].rotation * Vector3.forward;
                listBoidVariable.matrices[i] = Matrix4x4.TRS(
                    listBoidVariable.boidDatas[i].position,
                    listBoidVariable.boidDatas[i].rotation,
                    listBoidVariable.prefabScale
                );
            }
        }

        private void RenderBoids()
        {
            for (int i = 0; i < boidCount; i += 1023)
            {
                int batchCount = Mathf.Min(1023, boidCount - i);
                Graphics.DrawMeshInstanced(
                    _boidMesh,
                    0,
                    _boidMaterial,
                    new List<Matrix4x4>(listBoidVariable.matrices).GetRange(i, batchCount).ToArray(),
                    batchCount,
                    _propertyBlock,
                    UnityEngine.Rendering.ShadowCastingMode.On,
                    true
                );
            }
        }
    }
}