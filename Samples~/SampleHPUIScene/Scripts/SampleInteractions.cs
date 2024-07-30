using UnityEngine;
using TMPro;
using ubco.ovilab.HPUI.Interaction;

namespace ubco.ovilab.HPUI.Sample
{
    public class SampleInteractions: MonoBehaviour
    {
        public TextMeshPro text;

        public void OnTap(HPUITapEventArgs args)
        {
            text.text = $"Tap @ {args.Position}";
        }

        public void OnGesture(HPUIGestureEventArgs args)
        {
            text.text = $"Gesture @ {args.Position} in {args.CumulativeDirection}";
        }
    }
}
