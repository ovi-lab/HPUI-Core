using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ubco.ovilab.HPUI.Interaction
{
    [RequireComponent(typeof(StaticMeshCollidersManager))]
    public class HPUIStaticContinuousInteractable : HPUIBaseInteractable
    {
        [SerializeField] private SkinnedMeshRenderer staticHPUIMesh;
        [SerializeField] bool flippedAndNormalisedCoords;
        [SerializeField] private int meshXResolution;
        
        private StaticMeshCollidersManager collidersManager;
        public int MeshXResolution => meshXResolution;
        public SkinnedMeshRenderer StaticHPUIMesh => staticHPUIMesh;
        
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
            flippedAndNormalisedCoords = true;
            return collidersManager.GetSurfacePointForCollider(distanceInfo.collider, flippedAndNormalisedCoords);
        }
    }
}