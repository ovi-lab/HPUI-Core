using UnityEngine;

namespace ubc.ok.ovilab.HPUI.Core
{
    public class InteractableButtonsRoot : MonoBehaviour
    {
#if UNITY_EDITOR
        private static GameObject dummyObject;

        public void TriggerTargetButton(ButtonController targetButton)
        {
            if (dummyObject == null)
            {
                dummyObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dummyObject.transform.localScale = Vector3.one * 0.02f;
                dummyObject.GetComponent<MeshRenderer>().enabled = false;
                dummyObject.AddComponent<ButtonTriggerCollider>();
            }
            if (targetButton == null)
            {
                return;
            }

            dummyObject.transform.position = targetButton.transform.position;
            targetButton.contactZone.TriggerBehaviour(dummyObject.GetComponent<Collider>());
            targetButton.contactAction.AddListener((btn) =>
            {
                dummyObject.transform.position = btn.transform.position - btn.transform.forward.normalized * 0.01f;
                btn.contactZone.state = ButtonZone.State.outside;
                btn.proximalZone.state = ButtonZone.State.outside;
            });
        }
#endif
    }
}
