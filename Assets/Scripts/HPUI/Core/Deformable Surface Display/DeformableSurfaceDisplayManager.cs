using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.Jobs;
using UnityEngine.Jobs;
using System;
using System.Linq;
using HPUI.Utils;

namespace HPUI.Core.DeformableSurfaceDisplay
{
    public class DeformableSurfaceDisplayManager : MonoBehaviour
    {
	public GameObject btnPrefab;
	public Transform planeMeshGeneratorRoot;
        
        // [RequireInterface(typeof(ICalibrationInterface))]
	// public UnityEngine.Object calibration;

        public DeformationCoordinateManager calibration;

        public float height {get {return calibration.height;} private set {}}
        public float width {get {return calibration.width;} private set {}}
        
	private PlaneMeshGenerator planeMeshGenerator;
	private DynamicMeshDeformer meshDeformer;
	private TransformAccessArray btns;
	public List<ButtonController> buttonControllers {get; private set;} = new List<ButtonController>();

	public bool generatedBtns {get; private set;} = false;
	
	[SerializeField]
	public Method method = Method.mulitifingerFOR_dynamic_deformed_spline;

	public enum Method
	{
	    mulitifingerFOR_planer,
	    mulitifingerFOR_dynamic_deformed_spline,
            fingerFOR_dynamic_deofrmed,
	    palmFOR
	}

        public UnityEvent SurfaceReadyAction = new UnityEvent();

	private bool processGenerateBtns = false;

	private NativeArray<Vector3> vertices;
	private NativeArray<Vector3> normals; 
	private Vector3 largestAngle, right, up, drawUp, drawRight, temppos;
	private int maxX, maxY;
	private float gridSize;
	private Vector3 scaleFactor, _scaleFactor;

	private List<ButtonController> btnControllers = new List<ButtonController>();

	public Coord currentCoord = new Coord();

	bool _inUse = false;
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
	    switch (method)
	    {
		case Method.mulitifingerFOR_dynamic_deformed_spline:
                    deformableSurfaceHandler();
		    break;
		case Method.mulitifingerFOR_planer:
                case Method.fingerFOR_dynamic_deofrmed:
                case Method.palmFOR:
                    throw new NotImplementedException();
		    // break;
	    }
	}

	void OnDestroy()
	{
	    btns.Dispose();
	}

	void deformableSurfaceHandler()
	{
	    if (generatedBtns)
	    {
		if (!inUse)
		    return;
		// var a = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		meshDeformer.DeformMesh();
		// var b = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                if (vertices.IsCreated)
                    vertices.CopyFrom(planeMeshGenerator.mesh.vertices);
                else
                    vertices = new NativeArray<Vector3>(planeMeshGenerator.mesh.vertices, Allocator.Persistent);

                if (normals.IsCreated)
                    normals.CopyFrom(planeMeshGenerator.mesh.normals);
                else
                    normals = new NativeArray<Vector3>(planeMeshGenerator.mesh.normals, Allocator.Persistent);

		right = vertices[10] - vertices[1];
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
		// var c = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		// Debug.Log(jobHandle.IsCompleted + "------- b-a:  " + (b-a) + "      ------- c-b  " + (c-b) +  "  " + (c-a) + " ---- :"  + ((double)((b-a)/(c-a))).ToString("F6"));
	    }
	    else
	    {
		if (calibration.isCalibrated() && planeMeshGenerator.meshGenerated)
		{
		    if(processGenerateBtns)
		    {
			Debug.Log("Generating mesh");
			float yCenterOffset, xCenterOffset;
			generateBtns(planeMeshGenerator.mesh.vertices, planeMeshGenerator.mesh.normals, planeMeshGenerator.transform, out yCenterOffset, out xCenterOffset);
			generatedBtns = true;
			inUse = inUse; // This will make the surface display either show or hide based onthe "inUse" status
                        currentCoord.maxX = planeMeshGenerator.x_divisions;
                        currentCoord.maxY = planeMeshGenerator.y_divisions;
                        SurfaceReadyAction.Invoke();
			if (inUse)
			    InteractionManger.instance.getButtons();
		    }
		    else
		    {
			// meshDeformer.DeformMesh();
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
                currentCoord.x = x;
                currentCoord.y = y;
	    }
	}
    
	public void Setup()
	{
	    processGenerateBtns = true;
	}
    
	void generateBtns(Vector3[] positions, Vector3[] _normals, Transform parent, out float yCenterOffset, out float xCenterOffset)
	{
	    var right = positions[1] - positions[0];
	    GameObject btn;
	    ButtonController btnCtrl;
	    scaleFactor = Vector3.zero;

	    Transform[] _btns = new Transform[positions.Length];

	    btnControllers.Clear();
	
	    for(var i = 0; i < positions.Length; i ++)
	    {
		btn = Instantiate(btnPrefab);
		btn.transform.name = "X" + (int) i % planeMeshGenerator.x_divisions + "-Y" + (int) i / planeMeshGenerator.x_divisions;
		// Getting the scale values to set the size of the buttons based on the size of a single square in the generated mesh
		if (scaleFactor == Vector3.zero)
		{
		    Vector3 buttonSize = btn.GetComponentInChildren<MeshRenderer>().bounds.size;//.Where(x => x.Zone == ButtonZone.Type.contact).ToList()[0].GetComponent<Collider>();
		    // float gridSize = parent.InverseTransformVector(Vector3.up * planeMeshGenerator.step_size).magnitude;
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
		btn.transform.localScale = scaleFactor;// Vector3.one * 0.06f;
		btnCtrl = btn.GetComponentInChildren<ButtonController>();
		buttonControllers.Add(btnCtrl);
		btnCtrl.id = i;
		// Debug.Log(btnCtrl + "" +  btns + "  " + btnCtrl.transform.parent);
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

	void OnDrawGizmos()
	{
	    // if (planeMeshGenerator.meshGenerated)
	    // {
	    //     planeMeshGeneratorRoot.transform.position = basePosition.position;
	    //     GameObject ob;
	    //     foreach (var v in planeMeshGenerator.mesh.vertices)
	    //     {
	    // 	Gizmos.DrawSphere(v, 0.01f);
	    //     }
	    //     Debug.Log(planeMeshGenerator.mesh.vertices.Length);
	    //     Debug.Break();
	    // }
	    if (generatedBtns)
	    {
		// Gizmos.DrawRay(btns[922].transform.position, drawUp * 200, Color.green);
		// Gizmos.DrawRay(btns[922].transform.position, drawRight * 200, Color.red);
	    }
	}
    
	[Serializable]
	public class RotatingPair{
	    public Transform p1;
	    public Transform p2;
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
		if (true)//btn.isSelectionBtn)
		{
		    if (i > maxX)
			up = vertices[i] - vertices[i - maxX];//up = btn.transform.position - btns[i - maxX].transform.position;
		    else
			up = vertices[i + maxX] - vertices[i];
		    
		    if ( i % maxX == 0)
			right = vertices[i + 1] - vertices[i];
		    else
			right = vertices[i] - vertices[i-1];//right = btn.transform.position - btns[i-1].transform.position;

		    // if (false)//i == 922)
		    // {
		    //     drawUp = up;
		    //     drawRight = right;
		    //     Debug.DrawRay(btn.transform.position, drawUp * 200, Color.green); 
		    //     Debug.DrawRay(btn.transform.position, drawRight * 200, Color.red);
		    //     Debug.Log("------ " + i + "   " + (i + 1));
		    // }
		    
		    btn.localRotation = Quaternion.LookRotation(normals[i], up);//Vector3.Cross(right, up), up);//-btn.transform.forward, up);
		    _scaleFactor.x = (right.magnitude / gridSize) * scaleFactor.x;
		    _scaleFactor.y = (up.magnitude / gridSize) * scaleFactor.y;
		    _scaleFactor.z = scaleFactor.z;
		    btn.localScale = _scaleFactor;
		    // btn.transform.parent.forward = -btn.transform.parent.forward;
		    // btn.transform.parent.forward = -normals[i];
		}
		// else
		// {
		//     btn.localRotation = Quaternion.LookRotation(normals[i]);
		// }
	    }
	}
    }
}
