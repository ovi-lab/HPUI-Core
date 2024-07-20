using ubco.ovilab.HPUI.Interaction;
using UnityEngine;

namespace ubco.ovilab.HPUI.CustomMeshUtils
{
    [RequireComponent(typeof(CustomMeshCollidersManager))]
    public class HPUICustomMesh : HPUIContinuousInteractable
    {
        [SerializeField] private SkinnedMeshRenderer customHPUIMesh;
        [SerializeField] private int meshXRes;
        
        private CustomMeshCollidersManager collidersManager;
        public int MeshXRes => meshXRes;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            collidersManager = GetComponent<CustomMeshCollidersManager>();
        }

        public void CreateCollidersMatrix()
        {
            collidersManager.SetupColliders(customHPUIMesh, this);
        }
    }
}