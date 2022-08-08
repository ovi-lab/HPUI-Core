using System;
using UnityEngine.Events;

namespace HPUI.Core
{
    /// <summary>
    /// Event containing a ButtonController as a parameter 
    /// </summary>
    [Serializable]
    public class ButtonControllerEvent : UnityEvent<ButtonController>
    {}   
}
