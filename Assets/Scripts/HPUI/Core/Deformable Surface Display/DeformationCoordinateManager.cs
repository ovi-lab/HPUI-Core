using System.Collections.Generic;
using UnityEngine;

namespace ubc.ok.ovilab.HPUI.Core.DeformableSurfaceDisplay
{
    /*
      This class modifies the coordnates of a mesh based on some criteria
     */
    [DefaultExecutionOrder(-120)]
    public class DeformationCoordinateManager : CoordinateManager
    {
	//order of keypoints is as follows:
	//index 1234 middle 1234 ring 1234 pinky 1234 bottom of palm
	public List<Vector3> keypoints {get; private set;}
	public List<Vector3> calibrationKeypoints {get; private set;}
	public List<Vector3> keypointDifferences {get; private set;}

	public List<Vector3> undeformedVerticesCoordinates {get; private set;} = new List<Vector3>();

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

        public DynamicMeshDeformer dynamicMeshDeformer;


	//public static string method = "rbf";
	string method = "rbf2";
        //public static string method = "2dsplines";
        //public static string method = "test";

        void Start()
	{
	    _isCalibrated = false;
	    keypoints = new List<Vector3>();
	    calibrationKeypoints = new List<Vector3>();
	    keypointDifferences = new List<Vector3>();

            SetupKeypoints();
        }

	public override void Calibrate()
	{
	    if (true)//_isCalibrated == false)
	    {
		//0: height, 1: width
		float[] dimensions = new float[2];
                
                dimensions[0] = height;
                dimensions[1] = width;
                planeMeshGenerator.CreateFlatMesh(dimensions);

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

		Debug.Log("Vertices Count = " + undeformedVerticesCoordinates.Count);
	    
                dynamicMeshDeformer.MeshRegenerated(planeMeshGenerator);
		_isCalibrated = true;
	    }
        
	}

	void Update()
	{
	    if (_isCalibrated == true)
	    {
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
	    }
	}

	public Vector3 MidpointCalculation(Vector3 point1, Vector3 point2)
	{
	    return ((point1 + point2) / 2);
	}

        public override bool isCalibrated()
        {
            return _isCalibrated;
        }
    }
}
