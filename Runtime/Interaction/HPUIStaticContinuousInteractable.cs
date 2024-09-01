using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ubco.ovilab.HPUI.Interaction
{
    [RequireComponent(typeof(StaticMeshCollidersManager))]
    public class HPUIStaticContinuousInteractable : HPUIBaseInteractable, IHPUIContinuousInteractable
    {
        /// <inheritdoc />
        public float X_size { get => collidersManager.XWidth * collidersManager.MeshXResolution; }

        /// <inheritdoc />
        public float Y_size { get => collidersManager.YWidth * collidersManager.MeshYResolution; }

        [Tooltip("The associated SkinnedMeshRenderer used by this interactable")]
        [SerializeField] private SkinnedMeshRenderer staticHPUIMesh;

        /// <summary>
        /// The associated SkinnedMeshRenderer used by this interactable
        /// </summary>
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

        [Tooltip("The X resolution of the associated SkinnedMeshRenderer.")]
        [SerializeField] private int meshXResolution;

        /// <summary>
        /// The X resolution of the associated SkinnedMeshRenderer.
        /// </summary>
        public int MeshXResolution => meshXResolution;
        
        private StaticMeshCollidersManager collidersManager;

        protected override void Awake()
        {
            base.Awake();
            collidersManager = GetComponent<StaticMeshCollidersManager>();
            Debug.Assert(collidersManager!=null);
            colliders.AddRange(collidersManager.SetupColliders(StaticHPUIMesh, MeshXResolution));
        }

        /// <inheritdoc />
        protected override void ComputeSurfaceBounds()
        {
        }
        
        /// <inheritdoc />
        public override bool ComputeInteractorPosition(IHPUIInteractor interactor, out Vector2 position)
        {
            if (interactor.GetDistanceInfo(this, out DistanceInfo info))
            {
                Vector2 offsetOnCollider = ComputeTargetPointOnTransformXZPlane(info.point, info.collider.transform);
                position = collidersManager.GetSurfacePointForCollider(info.collider) + offsetOnCollider;
                return true;
            }
            position = Vector2.zero;
            return false;
        }
    }
}
