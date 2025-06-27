using System.Collections.Generic;
using UnityEngine;

namespace BoidJob
{
    [CreateAssetMenu(fileName = "ListBoidVariableJob", menuName = "ScriptableObjects/ListBoidVariableJob")]
    public class ListBoidVariable : ScriptableObject
    {
        public List<BoidData> boidDatas = new();
        public Matrix4x4[] matrices;
        public Vector3 prefabScale;
        public Quaternion prefabRotation;
        public Vector3 prefabUp;
    }
}