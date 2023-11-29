using System.Collections.Generic;
using UnityEngine;

namespace ubc.ok.ovilab.HPUI.CoreV1
{
    [DefaultExecutionOrder(-200)]
    public class HandsManager : MonoBehaviour
    {
        public static HandsManager instance;

        public List<HandCoordinateManager> handCoordinateManagers = new List<HandCoordinateManager> ();
        // Start is called before the first frame update
        void Start()
        {
            if (!instance)
                instance = this;
        }
    }
}
