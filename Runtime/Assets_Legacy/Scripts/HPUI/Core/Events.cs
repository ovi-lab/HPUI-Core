using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace ubco.ovilab.HPUI.Legacy
{
    /// <summary>
    /// Event containing a ButtonController as a parameter 
    /// </summary>
    [Serializable]
    public class ButtonControllerEvent : UnityEvent<ButtonController>
    {}

    /// <summary>
    /// Event containing a List of ButtonControllers as a parameter 
    /// </summary>
    [Serializable]
    public class ButtonControllersEvent : UnityEvent<IEnumerable<ButtonController>>
    {}
}
