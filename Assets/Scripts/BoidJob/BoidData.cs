using System;
using UnityEngine;

namespace BoidJob
{
    [Serializable]
    public class BoidData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
    }
}