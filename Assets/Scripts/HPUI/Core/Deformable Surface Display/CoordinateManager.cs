using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ubc.ok.ovilab.HPUI.Core.DeformableSurfaceDisplay
{
    public abstract class CoordinateManager : MonoBehaviour
    {
        // Height and Width are here because we want to be able to dynamically edjust the sizes of them based on needs
        public float width = 1.0f;
	public float height = 5.0f;

	public bool startFinished {get; private set;} = false;

	public PlaneMeshGenerator planeMeshGenerator;
        public int handIndex = 0;
        public List<string> keyPointsUsed = new List<string>();
        protected HandCoordinateManager handCoordinateManager;
	protected List<GameObject> keypointObjects;

        //find keypoint objects on the hand and add them to ordered list
        protected void SetupKeypoints()
        {
	    keypointObjects = new List<GameObject>();
            handCoordinateManager = HandsManager.instance.handCoordinateManagers[handIndex];

            foreach (var name in keyPointsUsed)
            {
                keypointObjects.Add(handCoordinateManager.GetManagedCoord(name).gameObject);
            }
            
	    startFinished = true;
        }

        public void AddKeypointObject(GameObject keypointObject)
        {
            keypointObjects.Add(keypointObject);
        }

        public abstract void Calibrate();

        public abstract bool isCalibrated();
    }
}
