using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.Jobs;
using UnityEngine.Jobs;
using System;
using System.Linq;
using ubc.ok.ovilab.HPUI.Utils;

namespace ubc.ok.ovilab.HPUI.CoreV1.DeformableSurfaceDisplay
{
    public class DeformableSurfaceDisplayManager : MonoBehaviour
    {
	public GameObject btnPrefab;
	public Transform planeMeshGeneratorRoot;
        
        public CoordinateManager calibration;

        public float height {get {return calibration.height;} private set {}}
        public float width {get {return calibration.width;} private set {}}
        
	private PlaneMeshGenerator planeMeshGenerator;
	private DynamicMeshDeformer meshDeformer;
	private TransformAccessArray btns;
	public List<ButtonController> buttonControllers {get; private set;} = new List<ButtonController>();

	public bool generatedBtns {get; private set;} = false;
        public Material material;

        [SerializeField]
	public Method method = Method.multifingerFOR_dynamic_deformed_spline;

	public enum Method
	{
	    multifingerFOR_dynamic_deformed_spline,
            multifingerFOR_dynamic_skinned_mesh
	}

        public bool noDynamic = false;
        public UnityEvent SurfaceReadyAction = new UnityEvent();

	private bool processGenerateBtns = false;

	private NativeArray<Vector3> vertices;
	private NativeArray<Vector3> normals; 
	private Vector3 largestAngle, right, up, drawUp, drawRight, temppos;
	private int maxX, maxY;
	private float gridSize;
	private Vector3 scaleFactor, _scaleFactor;
        private SkinnedMeshRenderer skinnedMeshRenderer;
        private Mesh tempMesh;
        private List<Vector3> _vertices;
        private List<Vector3> _normals;

        private List<ButtonController> btnControllers = new List<ButtonController>();

	public Coord currentCoord = new Coord();

        [SerializeField]
	private bool _inUse = false;
	public bool inUse
	{
	    get
	    {
		return _inUse;
	    }
	    set
	    {
		_inUse = value;
		if (generatedBtns)
		{
		    if (_inUse)
			planeMeshGenerator.gameObject.SetActive(true);
		    else
			planeMeshGenerator.gameObject.SetActive(false);
		}
	    }	
	}

        public MeshRenderer MeshRenderer {
            get {return planeMeshGenerator.GetComponent<MeshRenderer>();}
            private set {}}


	public void idToXY(int id, out int x, out int y)
	{
	    planeMeshGenerator.idToXY(id, out x, out y);
	}

	public int idToX(int id)
	{
	    return planeMeshGenerator.idToX(id);
	}

	public int idToY(int id)
	{
	    return planeMeshGenerator.idToY(id);
	}

	// Start is called before the first frame update
	void Awake()
	{
	    planeMeshGenerator = planeMeshGeneratorRoot.GetComponentInChildren<PlaneMeshGenerator>();
	    meshDeformer = planeMeshGenerator.GetComponent<DynamicMeshDeformer>();
	}

	// Update is called once per frame
	void Update()
	{
            if (!noDynamic)
            {
                deformableSurfaceHandler(method);
            }
	}

	void OnDestroy()
	{
            if (btns.isCreated)
            {
                btns.Dispose();
            }
            if (vertices.IsCreated)
            {
                vertices.Dispose();
            }
            if (normals.IsCreated)
            {
                normals.Dispose();
            }
        }

        public void SetButtonLocations(Method method)
        {
            switch(method)
            {
                case Method.multifingerFOR_dynamic_deformed_spline:
                    meshDeformer?.DeformMesh();
                    planeMeshGenerator.mesh.GetVertices(_vertices);
                    planeMeshGenerator.mesh.GetNormals(_normals);
                    break;
                case Method.multifingerFOR_dynamic_skinned_mesh:
                    // See https://forum.unity.com/threads/get-skinned-vertices-in-real-time.15685/
                    skinnedMeshRenderer.BakeMesh(tempMesh, true);
                    tempMesh.GetVertices(_vertices);
                    tempMesh.GetNormals(_normals);
                    break;
            }

            if (vertices.IsCreated)
                vertices.CopyFrom(_vertices.ToArray());
            else
                vertices = new NativeArray<Vector3>(_vertices.ToArray(), Allocator.Persistent);

            if (normals.IsCreated)
                normals.CopyFrom(_normals.ToArray());
            else
                normals = new NativeArray<Vector3>(_normals.ToArray(), Allocator.Persistent);

            maxY = vertices.Length - planeMeshGenerator.x_divisions;
            maxX = planeMeshGenerator.x_divisions;

            currentCoord.maxX = maxX;
            currentCoord.maxY = planeMeshGenerator.y_divisions;

            // Once the mesh has been deformed, update the locations of the buttons to match the mesh
            var job = new DeformedBtnLayoutJob()
            {
                scaleFactor = scaleFactor,
                gridSize = gridSize,
                maxX = maxX,
                maxY = maxY,
                normals = normals,
                vertices = vertices
            };

            var jobHandle = job.Schedule(btns);
            jobHandle.Complete();
        }

        public void SetupButtons()
        {
            if (skinnedMeshRenderer == null)
            {
                skinnedMeshRenderer = planeMeshGenerator.GetComponent<SkinnedMeshRenderer>();
            }
            if (tempMesh == null)
            {
                tempMesh = new Mesh();
            }

            int vertexCount = planeMeshGenerator.mesh.vertexCount;
            _vertices = new List<Vector3>();
            _normals = new List<Vector3>();

            if (vertices.IsCreated)
            {
                vertices.Dispose();
            }
            if (normals.IsCreated)
            {
                normals.Dispose();
            }

            float yCenterOffset, xCenterOffset;
            generateBtns(planeMeshGenerator.mesh.vertices, planeMeshGenerator.mesh.normals, planeMeshGenerator.transform, out yCenterOffset, out xCenterOffset);
            inUse = inUse; // This will make the surface display either show or hide based onthe "inUse" status
            currentCoord.maxX = planeMeshGenerator.x_divisions;
            currentCoord.maxY = planeMeshGenerator.y_divisions;
            if (material != null)
            {
                planeMeshGenerator.GetComponent<Renderer>().material = material;
            }
            SurfaceReadyAction.Invoke();
            if (inUse)
                InteractionManger.instance.GetButtons();
        }

	public void deformableSurfaceHandler(Method method)
	{
	    if (generatedBtns)
	    {
		if (!inUse)
		    return;
                SetButtonLocations(method);
            }
	    else
	    {
		if (calibration.isCalibrated() && planeMeshGenerator.meshGenerated)
		{
		    if(processGenerateBtns)
		    {
			Debug.Log("Generating mesh");
                        SetupButtons();
                        generatedBtns = true;
                    }
		}
	    }
	}

	public void ContactAction(ButtonController btn)
	{
	    if (btnControllers.Contains(btn))
	    {
                int x, y;
		planeMeshGenerator.idToXY(btn.id, out x, out y);
                currentCoord.SetCoord(x, y);
            }
	}
    
	public void Setup()
	{
	    processGenerateBtns = true;
            generatedBtns = false;
            skinnedMeshRenderer = null;
            buttonControllers.Clear();
            if (btns.isCreated)
            {
                btns.Dispose();
            }
        }
    
        /// <summary>
        /// Update the button locations based on the mesh vertices
        /// </summary>
	void generateBtns(Vector3[] positions, Vector3[] _normals, Transform parent, out float yCenterOffset, out float xCenterOffset)
	{
	    var right = positions[1] - positions[0];
	    GameObject btn;
	    ButtonController btnCtrl;
	    scaleFactor = Vector3.zero;

            for (int i = 0; i < planeMeshGenerator.transform.childCount; i++)
            {
                Destroy(planeMeshGenerator.transform.GetChild(i).gameObject);
            }
            if (btns.isCreated)
            {
                btns.Dispose();
            }
            Transform[] _btns = new Transform[positions.Length];

	    btnControllers.Clear();
	
	    for(var i = 0; i < positions.Length; i ++)
	    {
		btn = Instantiate(btnPrefab);
		btn.transform.name = "X" + (int) i % planeMeshGenerator.x_divisions + "-Y" + (int) i / planeMeshGenerator.x_divisions;
		// Getting the scale values to set the size of the buttons based on the size of a single square in the generated mesh
		if (scaleFactor == Vector3.zero)
		{
		    Vector3 buttonSize = btn.GetComponentInChildren<MeshRenderer>().bounds.size;
		    gridSize = parent.InverseTransformVector(positions[0] - positions[1]).magnitude;
		
		    scaleFactor = btn.transform.localScale;
		    // making them slightly larger to remove the spaces between the pixels
		    scaleFactor.x = (gridSize / buttonSize.x) * 1.05f * parent.lossyScale.x;
		    scaleFactor.y = (gridSize / buttonSize.y) * 1.05f * parent.lossyScale.y;
		    scaleFactor.z = 1/parent.lossyScale.z;
		    gridSize = (positions[0] - positions[1]).magnitude;
		}
		btn.transform.parent = parent;
		btn.transform.localPosition = positions[i];
		btn.transform.localRotation = Quaternion.identity;
		btn.transform.localScale = scaleFactor;
		btnCtrl = btn.GetComponentInChildren<ButtonController>();
		buttonControllers.Add(btnCtrl);
		btnCtrl.id = i;
		_btns[i] = btnCtrl.transform.parent;
	    
		btnControllers.Add(btnCtrl);
		btnCtrl.contactAction.AddListener(ContactAction);
	    }
	    btns = new TransformAccessArray(_btns);
	    var yPos = (from pos in positions select pos[1]);
	    var xPos = (from pos in positions select pos[0]);
	    yCenterOffset = (yPos.Max() - yPos.Min()) / 2;
	    xCenterOffset = (xPos.Max() - xPos.Min()) / 2;
	}
   
	struct DeformedBtnLayoutJob: IJobParallelForTransform
	{
	    private Vector3 right, up, temppos, _scaleFactor;
	    public Vector3 scaleFactor;
	    public float gridSize; 
	    public int maxX, maxY;

            [ReadOnly]
            public NativeArray<Vector3> vertices;
            [ReadOnly]
            public NativeArray<Vector3> normals;
	
	    public void Execute(int i, TransformAccess btn)
	    {
		temppos = vertices[i];
		temppos.z += -0.0002f;
		btn.localPosition = temppos;

                if (i > maxX)
                    up = vertices[i] - vertices[i - maxX];
                else
                    up = vertices[i + maxX] - vertices[i];
		    
                if (i % maxX == 0)
                    right = vertices[i + 1] - vertices[i];
                else
                    right = vertices[i] - vertices[i - 1];

                btn.localRotation = Quaternion.LookRotation(normals[i], up);
                _scaleFactor.x = (right.magnitude / gridSize) * scaleFactor.x;
                _scaleFactor.y = (up.magnitude / gridSize) * scaleFactor.y;
                _scaleFactor.z = scaleFactor.z;
                btn.localScale = _scaleFactor;
	    }
	}
    }
}
