using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ubco.ovilab.HPUI.Interaction
{
    [RequireComponent(typeof(StaticMeshCollidersManager))]
    public class HPUIStaticContinuousInteractable : HPUIBaseInteractable
    {
        [SerializeField] private SkinnedMeshRenderer staticHPUIMesh;
        [SerializeField] private int meshXResolution;
        
        private StaticMeshCollidersManager collidersManager;
        public int MeshXResolution => meshXResolution;
        public SkinnedMeshRenderer StaticHPUIMesh
        {
            get
            {
                if (staticHPUIMesh == null)
                {
                    staticHPUIMesh = GetComponent<SkinnedMeshRenderer>();
                }
                return staticHPUIMesh;
            }
            set => staticHPUIMesh = value;
        }


        public StaticMeshCollidersManager CollidersManager => collidersManager;
        
        protected override void Awake()
        {
            base.Awake();
            collidersManager = GetComponent<StaticMeshCollidersManager>();
            Debug.Assert(collidersManager!=null);
            colliders.AddRange(collidersManager.SetupColliders(StaticHPUIMesh, this));
        }

        /// <inheritdoc />
        protected override void ComputeSurfaceBounds()
        {
        }
        
        /// <inheritdoc />
        public override Vector2 ComputeInteractorPosition(IHPUIInteractor interactor)
        {
            DistanceInfo distanceInfo = GetDistanceOverride(this, interactor.GetCollisionPoint(this));
            return collidersManager.GetSurfacePointForCollider(distanceInfo.collider);
        }
    }
}