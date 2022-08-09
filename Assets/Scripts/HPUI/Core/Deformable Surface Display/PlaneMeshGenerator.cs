using System;
using System.Collections.Generic;
using UnityEngine;

namespace HPUI.Core.DeformableSurfaceDisplay
{
    /*
      Provided the dimensions of a mesh, this class will gerneate the mesh
     */
    [RequireComponent(typeof(MeshFilter))]
    public class PlaneMeshGenerator : MonoBehaviour
    {
	private MeshFilter filter;
	private Texture tex;
	public Mesh mesh
	{
	    get { return filter.mesh; }
	}
    
	public bool meshGenerated { get; private set; } = false;

        public event Action MeshGeneratedEvent;

	public float x_size {get; private set;}
	public float y_size {get; private set;}

	public int x_divisions {get; private set;}
	public int y_divisions = 35;
	//public static int z_divisions = 50;
	public float step_size {get; private set;}

        public OrientationInformation orientationInformation;

        public int handIndex = 0;
        private HandCoordinateManager handCoordinateManager;

	public Transform transformAnchor;

	public GameObject display;

	public List<Vector3> vertices;

	//going further into the negatives makes the display start further above the hand
	//if this variable == 0 then the fingers poke through the display even in calibration pose
	public float zVerticesOffset;

	public bool flipUp = false;

        private void OnMeshGeneratedEvent()
        {
            if (MeshGeneratedEvent != null)
                MeshGeneratedEvent();
        }

	public void Start()
	{
	    if (display == null)
		display = GameObject.Find("DeformableDisplay");
            handCoordinateManager = HandsManager.instance.handCoordinateManagers[handIndex];
        }

	public void CreateFlatMesh(float[] dimensions, DeformationCoordinateManager deformationCoordinateManager)
	{
	    if (y_divisions == 0)
		return;
	    //zVerticesOffset = -0.7f;

	    //zVerticesOffset = -1.5f;
	    zVerticesOffset = -0.0005f;

	    //x/y scaling factors
	    float xsf = 1.5f;
	    float ysf = 1.5f;

	    xsf /= display.transform.lossyScale.x;
	    ysf /= display.transform.lossyScale.x;

	    //sizes based on calibration distances on hand model
	    y_size = ysf*(dimensions[0]);
	    x_size = xsf*(dimensions[1]);

	    filter = display.GetComponent<MeshFilter>();
	    tex = display.GetComponent<MeshRenderer>().material.mainTexture;

	    step_size = y_size / y_divisions;
	    x_divisions = (int)(x_size / step_size);

	    filter.mesh = GenerateMeshBottomMiddleOrigin(deformationCoordinateManager);
            // display.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Transparent/Diffuse"));
            // display.GetComponent<MeshRenderer>().material.shader = Shader.Find("Transparent/Diffuse");
            
	    Debug.Log("mesh generated");
	    meshGenerated = true;
            OnMeshGeneratedEvent();
        }

	Mesh GenerateMeshBottomMiddleOrigin(DeformationCoordinateManager deformationCoordinateManager)
	{
	    Mesh mesh = new Mesh();

	    vertices = new List<Vector3>();
	    var normals = new List<Vector3>();
	    var uvs = new List<Vector2>();

	    for (int k = 0; k < y_divisions; k++)
	    {

		//for (int i = -x_divisions/2; i < x_divisions/2; i++)
		for (int i = 0; i < x_divisions ; i++)
		{
		    //vertices.Add(new Vector3(x_size * (i / (float)x_divisions),  y_size * (k / (float)y_divisions), 0));
		    vertices.Add(new Vector3(x_size * ((i- ((float)x_divisions / 2.0f)) / (float)x_divisions), y_size * (k / (float)y_divisions), zVerticesOffset));
		    normals.Add(Vector3.up);

		    //uvs.Add(new Vector2((i+x_divisions/2+1) / (float)x_divisions, k / (float)y_divisions));
		    uvs.Add(new Vector2(1 - k / (float)(y_divisions-1), i / (float)(x_divisions-1)));

		}
	    }



	    var triangles = new List<int>();

	    //for (int i = 0; i < (y_divisions - 1) * (x_divisions) ; i++)
	    for (int i = 0; i < (y_divisions-1) * (x_divisions) - 1; i++)
	    {
		if ((i + 1) % (x_divisions) == 0)
		{
		    continue;
		}

		triangles.AddRange(new List<int>()
				   {
				       i,i+x_divisions,i+x_divisions+1,
					   i,i+x_divisions+1,i+1
					   });
	    }

	    mesh.SetVertices(vertices);
	    mesh.SetNormals(normals);
	    mesh.SetUVs(0, uvs);
	    mesh.SetTriangles(triangles, 0);

	    AlignDisplay(deformationCoordinateManager);
        
	    return mesh;
	}

	//Mesh GenerateMeshBottomLeftOrigin()
	//{
	//    Mesh mesh = new Mesh();

	//    var vertices = new List<Vector3>();
	//    var normals = new List<Vector3>();
	//    var uvs = new List<Vector2>();

	//    for (int k = 0; k < y_divisions; k++)
	//    {
	//        for (int i = 0; i < x_divisions; i++)
	//        {
	//            vertices.Add(new Vector3(x_size * (i / (float)x_divisions), y_size * (k / (float)y_divisions), 0));
	//            normals.Add(Vector3.up);
	//            uvs.Add(new Vector2(i/(float)x_divisions,k/(float)y_divisions));
	//        }
	//    }

	//    var triangles = new List<int>();

	//    for (int i = 0; i < (y_divisions - 1) * (x_divisions); i++)
	//    {

	//        if ((i + 1) % (x_divisions) == 0)
	//        {
	//            continue;
	//        }

	//        triangles.AddRange(new List<int>()
	//        {
	//            i,i+x_divisions,i+x_divisions+1,
	//            i,i+x_divisions+1,i+1
	//        });

	//    }

	//    mesh.SetVertices(vertices);
	//    mesh.SetNormals(normals);
	//    mesh.SetUVs(0, uvs);
	//    mesh.SetTriangles(triangles, 0);

	//    return mesh;
	//}

	void AlignDisplay(DeformationCoordinateManager deformationCoordinateManager, bool calcRotation=true)
	{
	    //display.transform.position = HandCoordinateGetter.palmBottom.transform.position;
	    //display.transform.localPosition = new Vector3(0, -x_size / 20, 0);

	    //display.transform.localPosition = new Vector3(0, 0, -0.015f);

	    if (calcRotation)
	    {
		Vector3 forwardDirectionVector = handCoordinateManager.GetManagedCoord(orientationInformation.forwardVectorP2).position - handCoordinateManager.GetManagedCoord(orientationInformation.forwardVectorP1).position;
		Vector3 sidewaysDirectionVector = handCoordinateManager.GetManagedCoord(orientationInformation.sideVectorP2).position - handCoordinateManager.GetManagedCoord(orientationInformation.sideVectorP1).position;
		Vector3 upwardDirectionVector = Vector3.Cross(sidewaysDirectionVector, forwardDirectionVector);

		//Debug.DrawLine(HandCoordinateGetter.middle4.transform.position, HandCoordinateGetter.palmBottom.transform.position, Color.white, 200f);

		//forward and upwards direction
		//forward would be palm base -> middle finger base
		//upward would be the normal of forward and sideways (index1/pinky1) vectors
		// if (flipUp)
		// 	upwardDirectionVector = -upwardDirectionVector;
		display.transform.rotation = Quaternion.LookRotation(upwardDirectionVector, forwardDirectionVector);
	    }
	    else
	    {
                // There is a descrepency with the local rotation to the rotation of the mesh, hence removing this for now
		// display.transform.localRotation = transformAnchor.localRotation;
	    }
	    display.transform.position = transformAnchor.position;
	}

	public Vector3 displayToWorldCoords(Vector3 displayCoords)
	{
	    return filter.transform.TransformPoint(displayCoords);
	}

	public Vector3 palmToDisplayCoords(Vector3 palmCoords)
	{
	    Vector3 worldCoords = handCoordinateManager.PalmToWorldCoords(palmCoords);
	    return filter.transform.InverseTransformPoint(worldCoords);
	}

	private void OnDrawGizmos()
	{
	    //for (int i = 0; i < x_divisions; i++)
	    //{
	    //    for (int k = 0; k < z_divisions; k++)
	    //    {
	    //        Gizmos.DrawSphere(new Vector3(x_size * (i / (float)x_divisions), 0, z_size * (k / (float)z_divisions)), 100f); 
	    //    }
	    //}
	    //Gizmos.DrawSphere(new Vector3(0, 0, 0), 1f);
	    //Gizmos.DrawSphere(new Vector3(x_size, 0, 0), 1f);
	    //Gizmos.DrawSphere(new Vector3(x_size, 0, z_size), 1f);
	    //Gizmos.DrawSphere(new Vector3(0, 0, z_size), 1f);

	    // Gizmos.DrawLine(HandCoordinateGetter.middle4.transform.position, HandCoordinateGetter.palmBottom.transform.position);
	    // Gizmos.DrawLine(HandCoordinateGetter.index1.transform.position, HandCoordinateGetter.pinky1.transform.position);

	    // Gizmos.DrawRay(HandCoordinateGetter.middle1.transform.position, (HandCoordinateGetter.middle4.transform.position - HandCoordinateGetter.middle1.transform.position) * 2);
	    // Gizmos.DrawRay(HandCoordinateGetter.pinky1.transform.position, (HandCoordinateGetter.index1.transform.position - HandCoordinateGetter.pinky1.transform.position) * 2);
	}

	public void idToXY(int id, out int x, out int y)
	{
	    y = idToY(id);
	    x = idToX(id);
	}

	public int idToX(int id)
	{
	    return (int) (id % x_divisions);
	}

	public int idToY(int id)
	{
	    return (int) (id / x_divisions);
	}

        [Serializable]
        public class OrientationInformation
        {
            public string sideVectorP1;
            public string sideVectorP2;
            public string forwardVectorP1;
            public string forwardVectorP2;
        }
    }
}
