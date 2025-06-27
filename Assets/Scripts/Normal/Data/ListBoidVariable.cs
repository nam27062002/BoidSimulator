using System.Collections.Generic;
using UnityEngine;

namespace Normal
{
    [CreateAssetMenu(fileName = "ListBoidVariable", menuName = "ScriptableObjects/ListBoidVariable")]
    public class ListBoidVariable : ScriptableObject
    {
        public List<BoidMovement> boidMovements = new();
    }
}