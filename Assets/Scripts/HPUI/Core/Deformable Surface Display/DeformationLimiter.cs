using System;
using System.Collections.Generic;
using UnityEngine;

namespace HPUI.Core.DeformableSurfaceDisplay
{
    [RequireComponent(typeof(DeformationCoordinateManager))]
    public class DeformationLimiter : MonoBehaviour
    {
        [Range(0, 1)]
        public float topFixedPercentage;
        [Range(0, 1)]
        public float bottomFixedPercentage;

        private DeformationCoordinateManager deformationCoordinateManager;
        private PlaneMeshGenerator planeMeshGenerator;

        void Start()
        {
            deformationCoordinateManager = GetComponent<DeformationCoordinateManager>();
            planeMeshGenerator = deformationCoordinateManager.planeMeshGenerator;
            planeMeshGenerator.MeshGeneratedEvent += OnMeshGenerated;
        }

        void OnMeshGenerated()
        {

            List<int> xIndices = new List<int>();
            int i;
            for (i = Mathf.CeilToInt(planeMeshGenerator.x_divisions * bottomFixedPercentage); i >= 0; i -= 4)
            {
                xIndices.Add(i);
            }
            if (i != 0)
            {
                xIndices.Add(0);
            }
            for (i = planeMeshGenerator.x_divisions - Mathf.CeilToInt(planeMeshGenerator.x_divisions * topFixedPercentage) - 1; i < planeMeshGenerator.x_divisions; i += 4)
            {
                xIndices.Add(i);
            }
            if (i != planeMeshGenerator.x_divisions - 1)
            {
                xIndices.Add(planeMeshGenerator.x_divisions - 1);
            }

            foreach (int j in xIndices)
            {
                for (i = 0; i < planeMeshGenerator.y_divisions - 1; i += 4)
                {
                    SetupKeyPointObject($"{i}{j}", planeMeshGenerator.x_divisions * i + j);
                }

                i = (planeMeshGenerator.y_divisions - 1);
                // making sure the top corners are added
                SetupKeyPointObject($"t{j}", planeMeshGenerator.x_divisions * i + j);
            }
        }

        private void SetupKeyPointObject(string name, int index)
        {
            var obj = new GameObject(name);// GameObject.CreatePrimitive(PrimitiveType.Sphere);//
            // obj.transform.localScale = Vector3.one * 0.01f;
            obj.transform.parent = planeMeshGenerator.transform;
            obj.transform.localPosition = planeMeshGenerator.vertices[index];
            deformationCoordinateManager.AddKeypointObject(obj);
        }
    }
}
