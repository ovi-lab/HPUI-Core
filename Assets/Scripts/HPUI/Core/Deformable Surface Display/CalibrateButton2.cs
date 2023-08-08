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
                if (!manager.coordinateManager.isCalibrated())
                {
                    manager.coordinateManager.Calibrate();
                    if (manager.deformableSurfaceDisplayManager != null)
                    {
                        manager.deformableSurfaceDisplayManager.Setup();
                    }
                    else
                    {
                        manager.coordinateManager.GetComponent<DeformableSurfaceDisplayManager>().Setup();
                    }
                }
            }

            OnCalibrationCompleteEvent?.Invoke();
        }

        [Serializable]
        public class Managers
        {
            public CoordinateManager coordinateManager;
            public DeformableSurfaceDisplayManager deformableSurfaceDisplayManager;
        }
    }
}
