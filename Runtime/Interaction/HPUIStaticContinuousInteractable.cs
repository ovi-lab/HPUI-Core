using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ubco.ovilab.HPUI.Interaction
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
            Debug.Assert(collidersManager!=null);
            collidersManager.SetupColliders(StaticHPUIMesh, this);
        }

        /// <inheritdoc />
        protected override void ComputeSurfaceBounds()
        {
        }
        
        public override Vector2 ComputeInteractorPosition(IHPUIInteractor interactor)
        {
            DistanceInfo distanceInfo = GetDistanceOverride(this, interactor.GetCollisionPoint(this));
            return collidersManager.GetSurfacePointForCollider(distanceInfo.collider);
        }
    }
}