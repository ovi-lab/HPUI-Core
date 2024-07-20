using UnityEngine;

namespace ubco.ovilab.HPUI.StaticMesh
{
    [CreateAssetMenu(fileName = "VertexRemapData", menuName = "CustomMesh/VertexData")]
    public class VertexRemapData : ScriptableObject
    {
        public int[] RemappedVertices;
    }
}