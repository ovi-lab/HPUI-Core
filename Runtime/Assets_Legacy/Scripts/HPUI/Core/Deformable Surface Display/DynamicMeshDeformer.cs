using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using IronPython.Hosting;

namespace ubco.ovilab.HPUI.Legacy.DeformableSurfaceDisplay
{
    public class DynamicMeshDeformer : MonoBehaviour
    {
	// public List<int> skipIds = new List<int>();
	public Vector3[] originalVertices {get; private set;}
	public Vector3[] modifiedVertices {get; private set;}

        public DeformationCoordinateManager deformationCoordinateManager;
        
	Vector3[] undeformedVerticesCoordinates;
        List<int> deformingVerticesIndices;

	dynamic py;

	private PlaneMeshGenerator plane;

	//d=1 for scalar fit
	int d = 1;

	double lambdav = 0.001;
	int nlayers = 2;
	alglib.spline2dbuilder builderx;
	alglib.spline2dinterpolant xx;
	alglib.spline2dfitreport repx;
	alglib.spline2dbuilder buildery;
	alglib.spline2dinterpolant yy;
	alglib.spline2dfitreport repy;
	alglib.spline2dbuilder builderz;
	alglib.spline2dinterpolant zz;
	alglib.spline2dfitreport repz;

	alglib.rbfmodel modelx;
	alglib.rbfreport rbfrepx;
	alglib.rbfmodel modely;
	alglib.rbfreport rbfrepy;
	alglib.rbfmodel modelz;
	alglib.rbfreport rbfrepz;

	alglib.rbfmodel modelxyz;
	alglib.rbfreport rbfrepxyz;

	//public static string method = "rbf";
	string method = "rbf2";
	//public static string method = "2dsplines";
	//public static string method = "test";
	bool triggered = false;

	GameObject[] undeformedSpheres;
	GameObject[] deformedSpheres;

        public void UpdateDeformingVerticesIndices(List<int> deformingVerticesIndices)
        {
            modifiedVertices = (Vector3[])originalVertices.Clone();
            UpdateModifiedVertices();

            if (deformingVerticesIndices == null)
            {
                this.deformingVerticesIndices = Enumerable.Range(0, undeformedVerticesCoordinates.Length).ToList();
            }
            else
            {
                this.deformingVerticesIndices = deformingVerticesIndices;
            }
        }

	public void MeshRegenerated(PlaneMeshGenerator plane)
	{
            this.plane = plane;
	    Debug.Log("Setting deformable mesh");
	    plane.mesh.MarkDynamic();
	    modifiedVertices = null;
	    originalVertices = null;
	    originalVertices = plane.mesh.vertices;
	    modifiedVertices = plane.mesh.vertices;
	    undeformedVerticesCoordinates = deformationCoordinateManager.undeformedVerticesCoordinates.ToArray();
	    modelxyz = null;

            if (deformingVerticesIndices == null)
            {
                deformingVerticesIndices = Enumerable.Range(0, undeformedVerticesCoordinates.Length).ToList();
            }
	
	    //working
	    //modifiedVertices[0] = originalVertices[0] + Vector3.down * 1000;
	    //plane.mesh.SetVertices(modifiedVertices);

	    //set up python
	    //var engine = Python.CreateEngine();
	    //ICollection<string> searchPaths = engine.GetSearchPaths();
	    //searchPaths.Add(@"D:\Michael\Documents\HCI\deformable display unity\deformable display attempt to add python\Assets\Scenes\Materials");
	    //searchPaths.Add(@"D:\Michael\Documents\HCI\deformable display unity\deformable display attempt to add python\Assets\Plugins\Lib");
	    //engine.SetSearchPaths(searchPaths);
	    //py = engine.ExecuteFile(@"D:\Michael\Documents\HCI\deformable display unity\deformable display attempt to add python\Assets\createsplines.py");



	    //CreateSplines();

	    if (method.Equals("2dsplines"))
	    {
		alglib.spline2dbuildercreate(d, out builderx);
		alglib.spline2dbuildersetgrid(builderx, plane.x_divisions, plane.y_divisions);
		alglib.spline2dbuildersetalgofastddm(builderx, nlayers, lambdav);

		alglib.spline2dbuildercreate(d, out buildery);
		alglib.spline2dbuildersetgrid(buildery, plane.x_divisions, plane.y_divisions);
		alglib.spline2dbuildersetalgofastddm(buildery, nlayers, lambdav);
		//alglib.spline2dfit(buildery, out yy, out repy);
		alglib.spline2dbuildercreate(d, out builderz);
		alglib.spline2dbuildersetgrid(builderz, plane.x_divisions, plane.y_divisions);
		alglib.spline2dbuildersetalgofastddm(builderz, nlayers, lambdav);
		//alglib.spline2dfit(builderz, out zz, out repz);
	    }
	}


	public void DeformMesh()
	{
	    if (method.Equals("rbf"))
	    {
		//double lambd = 0.001;
		double lambd = 0.0;
		int nlayers = 15;
	    
            
		alglib.rbfcreate(2, 1, out modelx);
		alglib.rbfsetpoints(modelx, deformationCoordinateManager.xDifferenceVectors);     
		alglib.rbfsetalgohierarchical(modelx, 1.0, nlayers, lambd);
		alglib.rbfbuildmodel(modelx, out rbfrepx);


		alglib.rbfcreate(2, 1, out modely);
		alglib.rbfsetpoints(modely, deformationCoordinateManager.yDifferenceVectors);
		alglib.rbfsetalgohierarchical(modely, 1.0, nlayers, lambd);
		alglib.rbfbuildmodel(modely, out rbfrepy);

		alglib.rbfcreate(2, 1, out modelz);
		alglib.rbfsetpoints(modelz, deformationCoordinateManager.zDifferenceVectors);
		alglib.rbfsetalgohierarchical(modelz, 1.0, nlayers, lambd);
		alglib.rbfbuildmodel(modelz, out rbfrepz);
	    }

	    if (method.Equals("rbf2"))
	    {
		double lambd = 0.00001;
		//int nlayers = 20;
		int nlayers = 100;
	    
		alglib.rbfcreate(2, 3, out modelxyz);
		alglib.rbfsetpoints(modelxyz, deformationCoordinateManager.xyzDifferenceVectors);
		//RBASE CHANGES DEFORMATION RADIUS
		double rBase = 0.03;

		alglib.rbfsetalgohierarchical(modelxyz, rBase, nlayers, lambd);
		alglib.rbfbuildmodel(modelxyz, out rbfrepxyz);
	    }

	    if (method.Equals("2dsplines"))
	    {
		alglib.spline2dbuildersetpoints(builderx, deformationCoordinateManager.xDifferenceVectors, 17);
		alglib.spline2dbuildersetpoints(buildery, deformationCoordinateManager.yDifferenceVectors, 17);
		alglib.spline2dbuildersetpoints(builderz, deformationCoordinateManager.zDifferenceVectors, 17);

		alglib.spline2dfit(builderx, out xx, out repx);
		alglib.spline2dfit(buildery, out yy, out repy);
		alglib.spline2dfit(builderz, out zz, out repz);
	    }

	    Vector3 displacementInPalm;
	    Vector3 displacementInDisplay;
	    double[] xy = new double[] {0, 0};
	    double[] functionValues;
	    foreach (int i in deformingVerticesIndices)
	    {
		// if (skipIds.Contains(i))
		//     continue;

		if (method.Equals("rbf"))
		{
		    displacementInPalm = new Vector3((float)alglib.rbfcalc2(modelx, undeformedVerticesCoordinates[i].x, undeformedVerticesCoordinates[i].y), 
						     (float)alglib.rbfcalc2(modely, undeformedVerticesCoordinates[i].x, undeformedVerticesCoordinates[i].y), 
						     (float)alglib.rbfcalc2(modelz, undeformedVerticesCoordinates[i].x, undeformedVerticesCoordinates[i].y));
                
		    displacementInDisplay = plane.palmToDisplayCoords(displacementInPalm);


		    modifiedVertices[i] = new Vector3((originalVertices[i].x + displacementInDisplay.x),
						      (originalVertices[i].y + displacementInDisplay.y),
						      (originalVertices[i].z + displacementInDisplay.z));
		    //(originalVertices[i].z + displacementInDisplay.z - 0.028f));
		}

		if (method.Equals("rbf2"))
		{
		    // xy = new double[] { undeformedVerticesCoordinates[i].x, undeformedVerticesCoordinates[i].y };
		    xy[0] = undeformedVerticesCoordinates[i].x;
		    xy[1] = undeformedVerticesCoordinates[i].y;
		    alglib.rbfcalc(modelxyz, xy, out functionValues);

		    displacementInPalm = new Vector3((float)functionValues[0],(float)functionValues[1],(float)functionValues[2]);

		    displacementInDisplay = displacementInPalm;// plane.palmToDisplayCoords(displacementInPalm);


		    modifiedVertices[i] = originalVertices[i] + displacementInDisplay;
		    // new Vector3((originalVertices[i].x + displacementInDisplay.x),
		    // 		  (originalVertices[i].y + displacementInDisplay.y),
		    // 		  (originalVertices[i].z + displacementInDisplay.z));
		    //(originalVertices[i].z + displacementInDisplay.z - 0.005f));
		}

		if (method.Equals("2dsplines"))
		{
		    displacementInPalm = new Vector3((float)alglib.spline2dcalc(xx, undeformedVerticesCoordinates[i].x, undeformedVerticesCoordinates[i].y), 
						     (float)alglib.spline2dcalc(yy, undeformedVerticesCoordinates[i].x, undeformedVerticesCoordinates[i].y), 
						     (float)alglib.spline2dcalc(zz, undeformedVerticesCoordinates[i].x, undeformedVerticesCoordinates[i].y));
		    displacementInDisplay = plane.palmToDisplayCoords(displacementInPalm);


		    modifiedVertices[i] = new Vector3((originalVertices[i].x + displacementInDisplay.x),
						      (originalVertices[i].y + displacementInDisplay.y),
						      (originalVertices[i].z + displacementInDisplay.z - 0.028f));
		}


	    }

	    if (method.Equals("test"))
	    {
		//modifiedVertices[0] = new Vector3(originalVertices[0].x, originalVertices[0].y, -1 + originalVertices[0].z);

		//modifiedVertices[0] = plane.palmToDisplayCoords(new Vector3((float)(deformationCoordinateManager.xDifferenceVectors[0, 0] + deformationCoordinateManager.xDifferenceVectors[0, 2]),
		//(float)(deformationCoordinateManager.yDifferenceVectors[0, 1] + deformationCoordinateManager.yDifferenceVectors[0, 2]),
		//(float)(deformationCoordinateManager.calibrationKeypoints[0].z + deformationCoordinateManager.zDifferenceVectors[0, 2])));

	    }

	    //float floatx = deformationCoordinateManager.calibrationKeypoints[3].x + (float)alglib.rbfcalc2(modelx, deformationCoordinateManager.calibrationKeypoints[3].x, deformationCoordinateManager.calibrationKeypoints[3].y);
	    //float floaty = deformationCoordinateManager.calibrationKeypoints[3].y + (float)alglib.rbfcalc2(modely, deformationCoordinateManager.calibrationKeypoints[3].x, deformationCoordinateManager.calibrationKeypoints[3].y);
	    //float floatz = deformationCoordinateManager.calibrationKeypoints[3].z + (float)alglib.rbfcalc2(modelz, deformationCoordinateManager.calibrationKeypoints[3].x, deformationCoordinateManager.calibrationKeypoints[3].y);

	    //Debug.Log("xxxxx: " + deformationCoordinateManager.keypoints[3].x + " " + floatx);
	    //Debug.Log("yyyyy: " + deformationCoordinateManager.keypoints[3].y + " " + floaty);
	    //Debug.Log("zzzzz: " + deformationCoordinateManager.keypoints[3].z + " " + floatz);

            UpdateModifiedVertices();
        }

        private void UpdateModifiedVertices()
        {
            plane.mesh.SetVertices(modifiedVertices);
	    plane.mesh.RecalculateNormals();
        }


	private void Update()
	{
	    if (triggered == false &&   method.Equals("test"))
	    {
		undeformedSpheres = new GameObject[undeformedVerticesCoordinates.Length];
		deformedSpheres = new GameObject[undeformedVerticesCoordinates.Length];

		Debug.Log(undeformedVerticesCoordinates.Length);

		for (int i = 0; i < undeformedVerticesCoordinates.Length; i++)
		{
		    // undeformedSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		    // undeformedSpheres[i].transform.localScale += new Vector3(-0.999f, -.999f, -.999f);
		    // undeformedSpheres[i].transform.position = PalmBase.PalmToWorldCoords(new Vector3(undeformedVerticesCoordinates[i].x, undeformedVerticesCoordinates[i].y, undeformedVerticesCoordinates[i].z));
		}
		triggered = true;
	    }

	    // if (deformationCoordinateManager.isCalibrated == true)
	    // {
	    //     CreateSplines();
	    // }

	}
    }
}
