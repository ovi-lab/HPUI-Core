using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using HPUI.Core.DeformableSurfaceDisplay;

namespace HPUI.Core
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
                    manager.deformableSurfaceDisplayManager.Setup();
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
