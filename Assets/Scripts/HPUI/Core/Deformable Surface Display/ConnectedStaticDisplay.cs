using System.Collections.Generic;
using UnityEngine;


namespace HPUI.Core.DeformableSurfaceDisplay
{

    public class ConnectedStaticDisplay : MonoBehaviour
    {
        [SerializeField]
        DeformationCoordinateManager deformationCoordinateManager;
        
        public float topSize = 0.01f;
        public float bottomSize = 0.01f;

        private Vector3 blCorner, brCorner, trCorner, tlCorner;
        private PlaneMeshGenerator planeMeshGenerator;

        private GameObject topMesh, bottomMesh;

        public Texture TopTexture { get => topMesh.GetComponent<MeshRenderer>().material.mainTexture; set => topMesh.GetComponent<MeshRenderer>().material.mainTexture = value; }
        public Texture MainTexture {
            get => deformationCoordinateManager.GetComponent<DeformableSurfaceDisplayManager>().MeshRenderer.material.mainTexture;
            set => deformationCoordinateManager.GetComponent<DeformableSurfaceDisplayManager>().MeshRenderer.material.mainTexture = value;
        }
        public Texture BottomTexture { get => bottomMesh.GetComponent<MeshRenderer>().material.mainTexture; set => bottomMesh.GetComponent<MeshRenderer>().material.mainTexture = value; }

        // Start is called before the first frame update
        void Start()
        {
            planeMeshGenerator = deformationCoordinateManager.planeMeshGenerator;
            planeMeshGenerator.MeshGeneratedEvent += OnMeshGenerated;
            topMesh = new GameObject("topmesh");
            topMesh.transform.parent = transform.parent;
            topMesh.transform.localPosition = Vector3.zero;
            topMesh.AddComponent<MeshFilter>();
            var renderer = topMesh.AddComponent<MeshRenderer>();
            renderer.material = new Material(deformationCoordinateManager.GetComponent<DeformableSurfaceDisplayManager>().MeshRenderer.material);

            bottomMesh = new GameObject("bottommesh");
            bottomMesh.transform.parent = transform.parent;
            bottomMesh.transform.localPosition = Vector3.zero;
            bottomMesh.AddComponent<MeshFilter>();
            renderer = bottomMesh.AddComponent<MeshRenderer>();
            renderer.material = new Material(deformationCoordinateManager.GetComponent<DeformableSurfaceDisplayManager>().MeshRenderer.material);
        }


        void OnMeshGenerated()
        {
            // // Testing locations
            // var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            // go.transform.localScale = Vector3.one * 0.01f;
            // go.transform.parent = planeMeshGenerator.transform;
            // // go.transform.localPosition = planeMeshGenerator.vertices[0]; // bottom left corner
            // // go.transform.localPosition = planeMeshGenerator.vertices[planeMeshGenerator.x_divisions - 1]; // bottom right corner
            // // go.transform.localPosition = planeMeshGenerator.vertices[planeMeshGenerator.y_divisions * planeMeshGenerator.x_divisions - 1]; // top right corner
            // // go.transform.localPosition = planeMeshGenerator.vertices[(planeMeshGenerator.y_divisions - 1) * planeMeshGenerator.x_divisions]; // top left corner

            blCorner = planeMeshGenerator.vertices[0]; // bottom left corner                                                                 
            brCorner = planeMeshGenerator.vertices[planeMeshGenerator.x_divisions - 1]; // bottom right corner                               
            trCorner = planeMeshGenerator.vertices[planeMeshGenerator.y_divisions * planeMeshGenerator.x_divisions - 1]; // top right corner 
            tlCorner = planeMeshGenerator.vertices[(planeMeshGenerator.y_divisions - 1) * planeMeshGenerator.x_divisions]; // top left corner

            int i;
            for (i = 0; i < planeMeshGenerator.y_divisions - 1; i += 2)
            {
                SetupKeyPointObject($"{i}l", planeMeshGenerator.x_divisions * i);
                SetupKeyPointObject($"{i}r", planeMeshGenerator.x_divisions * i + planeMeshGenerator.x_divisions - 1);
            }

            i = (planeMeshGenerator.y_divisions - 1);
            // making sure the top corners are added
            SetupKeyPointObject($"tl", planeMeshGenerator.x_divisions * i);
            SetupKeyPointObject($"tr", planeMeshGenerator.x_divisions * i + planeMeshGenerator.x_divisions - 1);

            GenerateSimpleMesh(topMesh.GetComponent<MeshFilter>(),
                               brCorner,
                               brCorner + new Vector3(planeMeshGenerator.x_size * topSize, 0, 0),
                               trCorner,
                               trCorner + new Vector3(planeMeshGenerator.x_size * topSize, 0, 0));

            GenerateSimpleMesh(bottomMesh.GetComponent<MeshFilter>(),
                               blCorner - new Vector3(planeMeshGenerator.x_size * bottomSize, 0, 0),
                               blCorner,
                               tlCorner - new Vector3(planeMeshGenerator.x_size * bottomSize, 0, 0),
                               tlCorner);
        }

        void SetupKeyPointObject(string name, int index)
        {
            var obj = new GameObject(name);// GameObject.CreatePrimitive(PrimitiveType.Sphere);//
            // obj.transform.localScale = Vector3.one * 0.01f;
            obj.transform.parent = planeMeshGenerator.transform;
            obj.transform.localPosition = planeMeshGenerator.vertices[index];
            deformationCoordinateManager.AddKeypointObject(obj);
        }

        void GenerateSimpleMesh(MeshFilter filter, params Vector3[] vertices)
        {
            Mesh mesh = new Mesh();
            // var vertices = new List<Vector3>();
	    var normals = new List<Vector3>();
	    var uvs = new List<Vector2>(){new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 1)};

            // vertices.Add(brCorner);
            // vertices.Add(brCorner + new Vector3(0.01f, 0, 0));
            // vertices.Add(trCorner);
            // vertices.Add(trCorner + new Vector3(0.01f, 0, 0));

            foreach (var v in vertices)
            {
                normals.Add(Vector3.up);
            }

            var triangles = new List<int>() {0, 2, 1, 1, 2, 3};
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);

            filter.mesh = mesh;
        }
    }
}
