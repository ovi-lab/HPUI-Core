using System;
using System.Collections.Generic;
using UnityEngine;

using ubc.ok.ovilab.HPUI.Core.DeformableSurfaceDisplay;

namespace ubc.ok.ovilab.HPUI.Core
{
    public class CalibrateButton2 : MonoBehaviour
    {

        public event Action OnCalibrationCompleteEvent;

        public List<Managers> managers = new List<Managers>();
        
	private void OnTriggerEnter(Collider other)
	{
	    OnClick();
	}

	public void OnClick()
	{
            foreach (var manager in managers)
            {
                if (!manager.deformationCoordinateManager.isCalibrated())
                {
                    manager.deformationCoordinateManager.Calibrate();
                    if (manager.deformableSurfaceDisplayManager != null)
                    {
                        manager.deformableSurfaceDisplayManager.Setup();
                    }
                    else
                    {
                        manager.deformationCoordinateManager.GetComponent<DeformableSurfaceDisplayManager>().Setup();
                    }
                }
            }

            OnCalibrationCompleteEvent?.Invoke();
        }

        [Serializable]
        public class Managers
        {
            public DeformationCoordinateManager deformationCoordinateManager;
            public DeformableSurfaceDisplayManager deformableSurfaceDisplayManager;
        }
    }
}
