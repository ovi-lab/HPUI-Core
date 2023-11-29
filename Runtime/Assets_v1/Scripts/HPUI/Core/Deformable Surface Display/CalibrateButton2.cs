using System;
using System.Collections.Generic;
using UnityEngine;

using ubc.ok.ovilab.HPUI.CoreV1.DeformableSurfaceDisplay;

namespace ubc.ok.ovilab.HPUI.CoreV1
{
    public class CalibrateButton2 : MonoBehaviour
    {

        public event Action OnCalibrationCompleteEvent;

        public List<Managers> managers = new List<Managers>();
        public bool allowForceCalibrate = false;

        private void OnTriggerEnter(Collider other)
	{
	    OnClick();
	}

	public void OnClick()
	{
            foreach (var manager in managers)
            {
                if (allowForceCalibrate || !manager.coordinateManager.isCalibrated())
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
