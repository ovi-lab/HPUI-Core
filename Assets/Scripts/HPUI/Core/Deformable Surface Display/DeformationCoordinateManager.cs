using System.Collections.Generic;
using UnityEngine;

namespace HPUI.Core.DeformableSurfaceDisplay
{
    /*
      This class modifies the coordnates of a mesh based on some criteria
     */
    [DefaultExecutionOrder(-120)]
    public class DeformationCoordinateManager : MonoBehaviour
    {
        //public bool useSendMessage = false;
	//public static GameObject hand;

        // Height and Width are here because we want to be able to dynamically edjust the sizes of them based on needs
        public float width = 1.0f;
	public float height = 5.0f;

	//order of keypoints is as follows:
	//index 1234 middle 1234 ring 1234 pinky 1234 bottom of palm
	public List<Vector3> keypoints {get; private set;}
	public List<Vector3> calibrationKeypoints {get; private set;}
	public List<Vector3> keypointDifferences {get; private set;}
	private List<GameObject> keypointObjects;

	public List<Vector3> undeformedVerticesCoordinates {get; private set;} = new List<Vector3>();

	//public List<Vector3> xDifferenceVectors = new List<Vector3>();
	//public List<Vector3> yDifferenceVectors = new List<Vector3>();
	//public List<Vector3> zDifferenceVectors = new List<Vector3>();
	public double[,] xDifferenceVectors {get; private set;}
	public double[,] yDifferenceVectors {get; private set;}
	public double[,] zDifferenceVectors {get; private set;}
	public double[,] xyzDifferenceVectors {get; private set;}

	//indices of POI for computing calibration width/height of display
	// int middleFingerTipIndex = 7;
	// int palmBaseIndex = 16;
	// int indexFingerTipIndex = 3;
	// int pinkyTipIndex = 15;
	// int middleFingerBottomIndex = 4;

	public bool _isCalibrated {get; private set;} = false;

	public bool startFinished {get; private set;} = false;

	float fingerThreshold;
	bool indexIsStraight;
	bool middleIsStraight;
	bool notificationWidthSatisfied;

	Vector3 index;
	Vector3 indexSmall;
	Vector3 middle;
	Vector3 middleSmall;

	public bool useStaticDisplaySize = true;

	public PlaneMeshGenerator planeMeshGenerator;
        public DynamicMeshDeformer dynamicMeshDeformer;
        public int handIndex = 0;
        private HandCoordinateManager handCoordinateManager;

        public List<string> keyPointsUsed = new List<string>();

	//public static string method = "rbf";
	string method = "rbf2";
	//public static string method = "2dsplines";
	//public static string method = "test";
	
	void Start()
	{
	    _isCalibrated = false;
	    keypoints = new List<Vector3>();
	    keypointObjects = new List<GameObject>();
	    calibrationKeypoints = new List<Vector3>();
	    keypointDifferences = new List<Vector3>();

	    //hand = GameObject.Find("b_r_wrist");

	    //find keypoint objects on the hand and add them to ordered list

            // Debug.Log(transform.Find("PlaneMeshTransformAnchors/offset"));
            // Debug.Log(handCoordinateManager.getManagedCoord("R2D1_anchor/left"));
            handCoordinateManager = HandsManager.instance.handCoordinateManagers[handIndex];

            foreach (var name in keyPointsUsed)
            {
                keypointObjects.Add(handCoordinateManager.GetManagedCoord(name).gameObject);
            }
            
	    startFinished = true;
	
	    //Calibrate();
	}

        public void AddKeypointObject(GameObject keypointObject)
        {
            keypointObjects.Add(keypointObject);
        }

	public void Calibrate()
	{
	    // Debug.Log("aaaaaaa");
	    if (true)//_isCalibrated == false)
	    {
		//0: height, 1: width
		float[] dimensions = new float[2];
                
		if (useStaticDisplaySize)
		{
		    //dimensions[0] = Vector3.Distance(calibrationKeypoints[middleFingerTipIndex], calibrationKeypoints[palmBaseIndex]);
		    dimensions[0] = height; //Vector3.Distance(calibrationKeypoints[middleFingerTipIndex], calibrationKeypoints[palmBaseIndex]);
		    dimensions[1] = width; //Vector3.Distance(calibrationKeypoints[indexFingerTipIndex], calibrationKeypoints[pinkyTipIndex]);
		}
		else
		{
                    Debug.LogError($"This is currently disabled");
		    // var planeMeshGenerator = FindObjectsOfType<PlaneMeshGenerator>();
		    // if (planeMeshGenerator.Length != 1)
		    //     Debug.LogError("There must be 1 GeneratePlaneMesh; but have " + planeMeshGenerator.Length + " in the scene.");
		    // var height = Vector3.Distance(handCoordinateManager.getManagedCoord("R2D2_anchor").transform.position, handCoordinateManager.getManagedCoord("R2D4_anchor").transform.position) * 1.2f;
		    // var width = (height / planeMeshGenerator[0].y_divisions) * planeMeshGenerator[0].y_divisions * 1.61f;
		    // dimensions = new float[] {height, width};
		    // // calibrationKeypoints[middleFingerTipIndex], calibrationKeypoints[palmBaseIndex]);
		    // // dimensions[1] = Vector3.Distance(index1.transform.position, ring1.transform.position) * 1.9f;
		    // // calibrationKeypoints[indexFingerTipIndex], calibrationKeypoints[pinkyTipIndex]);
		}

                planeMeshGenerator.CreateFlatMesh(dimensions, this);

		if (method != "rbf2")
		{
		    xDifferenceVectors = new double[keypointObjects.Count, 3];
		    yDifferenceVectors = new double[keypointObjects.Count, 3];
		    zDifferenceVectors = new double[keypointObjects.Count, 3];
		}
		else
		{
		    xyzDifferenceVectors = new double[keypointObjects.Count, 5];
		}

                for (int i = 0; i < keypointObjects.Count; i++)
                {
                    calibrationKeypoints.Add(handCoordinateManager.CoordinatesInPalmReferenceFrame(keypointObjects[i].transform.position));
                    keypoints.Add(handCoordinateManager.CoordinatesInPalmReferenceFrame(keypointObjects[i].transform.position));
                    keypointDifferences.Add(keypoints[i] - calibrationKeypoints[i]);

                    //xDifferenceVectors.Add(new Vector3(calibrationKeypoints[i].x, calibrationKeypoints[i].y, keypointDifferences[i].x));
                    //yDifferenceVectors.Add(new Vector3(calibrationKeypoints[i].x, calibrationKeypoints[i].y, keypointDifferences[i].y));
                    //zDifferenceVectors.Add(new Vector3(calibrationKeypoints[i].x, calibrationKeypoints[i].y, keypointDifferences[i].z));
                    if (method != "rbf2")
                    {
                        xDifferenceVectors[i, 0] = calibrationKeypoints[i].x;
                        xDifferenceVectors[i, 1] = calibrationKeypoints[i].y;
                        xDifferenceVectors[i, 2] = keypointDifferences[i].x;

                        yDifferenceVectors[i, 0] = calibrationKeypoints[i].x;
                        yDifferenceVectors[i, 1] = calibrationKeypoints[i].y;
                        yDifferenceVectors[i, 2] = keypointDifferences[i].y;

                        zDifferenceVectors[i, 0] = calibrationKeypoints[i].x;
                        zDifferenceVectors[i, 1] = calibrationKeypoints[i].y;
                        zDifferenceVectors[i, 2] = keypointDifferences[i].z;
                    }
                    else
                    {
                        xyzDifferenceVectors[i, 0] = calibrationKeypoints[i].x;
                        xyzDifferenceVectors[i, 1] = calibrationKeypoints[i].y;
                        xyzDifferenceVectors[i, 2] = keypointDifferences[i].x;
                        xyzDifferenceVectors[i, 3] = keypointDifferences[i].y;
                        xyzDifferenceVectors[i, 4] = keypointDifferences[i].z;
                    }

                }

                Debug.Log(dimensions[0] + " " + dimensions[1]);

		undeformedVerticesCoordinates.Clear();
		for (int i = 0; i < planeMeshGenerator.vertices.Count; i++)
		{
		    undeformedVerticesCoordinates.Add(handCoordinateManager.CoordinatesInPalmReferenceFrame(planeMeshGenerator.displayToWorldCoords(new Vector3(planeMeshGenerator.vertices[i].x, planeMeshGenerator.vertices[i].y, planeMeshGenerator.vertices[i].z))));
		}

		// GenerateNotificationBar.notBar.SendMessage("CreateMesh");

		Debug.Log("Vertices Count = " + undeformedVerticesCoordinates.Count);
	    
		// GeneratePlaneMesh.display.SendMessage("MeshRegenerated");
                dynamicMeshDeformer.MeshRegenerated(planeMeshGenerator);
		_isCalibrated = true;
	    }
        
	}

	void Update()
	{
	    if (_isCalibrated == true)
	    {
		//Debug.Log(keypointObjects.Count + " " + keypoints.Count);

		//index2.transform.Rotate(new Vector3(index2.transform.rotation.x, index2.transform.rotation.y+1, index2.transform.rotation.z));

		//every frame update the coordinates of all keypoints based on transform.position of the corresponding gameobject
		for (int i = 0; i < keypointObjects.Count; i++)
		{
		    keypoints[i] = handCoordinateManager.CoordinatesInPalmReferenceFrame(keypointObjects[i].transform.position);

		    keypointDifferences[i] = keypoints[i] - calibrationKeypoints[i];
                

		    //list of vectors that will be used to create the 3 splines per frame
		    //contain the calibration x/y coordinates, and an x, y, or z displacement
		    if(method != "rbf2")
		    {
			xDifferenceVectors[i, 0] = calibrationKeypoints[i].x;
			xDifferenceVectors[i, 1] = calibrationKeypoints[i].y;
			xDifferenceVectors[i, 2] = keypointDifferences[i].x;

			yDifferenceVectors[i, 0] = calibrationKeypoints[i].x;
			yDifferenceVectors[i, 1] = calibrationKeypoints[i].y;
			yDifferenceVectors[i, 2] = keypointDifferences[i].y;

			zDifferenceVectors[i, 0] = calibrationKeypoints[i].x;
			zDifferenceVectors[i, 1] = calibrationKeypoints[i].y;
			zDifferenceVectors[i, 2] = keypointDifferences[i].z;
		    }
		    else
		    {
			xyzDifferenceVectors[i, 0] = calibrationKeypoints[i].x;
			xyzDifferenceVectors[i, 1] = calibrationKeypoints[i].y;
			xyzDifferenceVectors[i, 2] = keypointDifferences[i].x;
			xyzDifferenceVectors[i, 3] = keypointDifferences[i].y;
			xyzDifferenceVectors[i, 4] = keypointDifferences[i].z;
		    }
  
		}



		//check angles here
		//angle between index 1/2 and 1/4 must be small
		//angle between middle 1/2 and 1/4 must be small
		//angle between index 1/4 and middle 1/4 along certain axis must be > threshold
		// fingerThreshold = 30;
		// indexIsStraight = false;
		// middleIsStraight = false;
		// notificationWidthSatisfied = false;

		// index = index4.transform.position - index1.transform.position;
		// indexSmall = index2.transform.position - index1.transform.position;

		// if (System.Math.Abs(Vector3.Angle(index, indexSmall)) < fingerThreshold)
		// {
		//     indexIsStraight = true;
		// }

		// middle = middle4.transform.position - middle1.transform.position;
		// middleSmall = middle2.transform.position - middle1.transform.position;

		// if (System.Math.Abs(Vector3.Angle(middle, middleSmall)) < fingerThreshold)
		// {
		//     middleIsStraight = true;
		// }

		//float xBottomDistance = System.Math.Abs(PalmBase.CoordinatesInPalmReferenceFrame(index1.transform.position).x - PalmBase.CoordinatesInPalmReferenceFrame(middle1.transform.position).x);
		// float xTopDistance = System.Math.Abs(PalmBase.CoordinatesInPalmReferenceFrame(index4.transform.position).x - PalmBase.CoordinatesInPalmReferenceFrame(middle4.transform.position).x);
		// float xTopDistance2 = System.Math.Abs(PalmBase.CoordinatesInPalmReferenceFrame(ring4.transform.position).x - PalmBase.CoordinatesInPalmReferenceFrame(middle4.transform.position).x);
		// float xTopDistance3 = System.Math.Abs(PalmBase.CoordinatesInPalmReferenceFrame(ring4.transform.position).x - PalmBase.CoordinatesInPalmReferenceFrame(pinky4.transform.position).x);


		// if (xTopDistance > 3.5 * xTopDistance2 && xTopDistance > 3.5 * xTopDistance3)
		// {
		//     notificationWidthSatisfied = true;
		// }

		// if (indexIsStraight && middleIsStraight && notificationWidthSatisfied)
		// {
		//     GenerateNotificationBar.shouldRender = true;
		// }
		// else
		// {
		//     GenerateNotificationBar.shouldRender = false;
		// }

		// Debug.Log("ttttttttt "+indexIsStraight + " " + middleIsStraight + " " + notificationWidthSatisfied+" "+ xTopDistance+" "+ xTopDistance2);

	    }
	    //Debug.Log("kpd: " + keypointDifferences[3].x + " " + keypointDifferences[3].y + " " + keypointDifferences[3].z);

	}

	//public static Vector3 HandToWorldCoords(Vector3 handCoords)
	//{
	//    return hand.transform.TransformPoint(handCoords);
	//}

	public Vector3 MidpointCalculation(Vector3 point1, Vector3 point2)
	{
	    return ((point1 + point2) / 2);
	    //Vector3 returnVector;
	    //returnVector = point1 + point2;
	    //returnVector.x = returnVector.x / 2f;
	    //returnVector.y = returnVector.y / 2f;
	    //returnVector.z = returnVector.z / 2f;
	    //return returnVector;
	}

        public bool isCalibrated()
        {
            return _isCalibrated;
        }
    }
}
