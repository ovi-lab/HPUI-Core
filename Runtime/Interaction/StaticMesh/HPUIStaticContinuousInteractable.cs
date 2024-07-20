using ubco.ovilab.HPUI.Interaction;
using UnityEngine;

namespace ubco.ovilab.HPUI.StaticMesh
{
    [RequireComponent(typeof(StaticMeshCollidersManager))]
    public class HPUIStaticContinuousInteractable : HPUIBaseInteractable
    {
        [SerializeField] private SkinnedMeshRenderer staticHPUIMesh;
        
        [SerializeField] private int meshXRes;
        
        private StaticMeshCollidersManager collidersManager;
        public int MeshXRes => meshXRes;
        public SkinnedMeshRenderer StaticHPUIMesh => staticHPUIMesh;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            collidersManager = GetComponent<StaticMeshCollidersManager>();
            collidersManager.SetupColliders(staticHPUIMesh, this);
        }

        protected override void ComputeSurfaceBounds()
        {
        }
    }
}