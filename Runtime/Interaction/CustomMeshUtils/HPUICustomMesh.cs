using ubco.ovilab.HPUI.Interaction;
using UnityEngine;

namespace ubco.ovilab.HPUI.CustomMeshUtils
{
    [RequireComponent(typeof(CustomMeshCollidersManager))]
    public class HPUICustomMesh : HPUIContinuousInteractable
    {
        [SerializeField] private SkinnedMeshRenderer customHPUIMesh;
        [SerializeField] private int meshXRes;
        [SerializeField] private int meshYRes;
        private CustomMeshCollidersManager collidersManager;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            collidersManager = GetComponent<CustomMeshCollidersManager>();
        }

        public void CreateCollidersMatrix()
        {
            collidersManager.SetupColliders(customHPUIMesh);
        }
    }
}