using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace ubco.ovilab.HPUI.Interaction
{
    [RequireComponent(typeof(MeshContinuousCollidersManager))]
    public class HPUIMeshContinuousInteractable : HPUIBaseInteractable, IHPUIContinuousInteractable
    {
        /// <inheritdoc />
        public float X_size { get => continuousCollidersManager.XWidth * continuousCollidersManager.MeshXResolution; }
        /// <inheritdoc />
        public float Y_size { get => continuousCollidersManager.YWidth * continuousCollidersManager.MeshYResolution; }
        /// <summary>
        /// X width of a single collider based on the mesh provided, in Unity units.
        /// </summary>
        public float SingleColliderXWidth { get => continuousCollidersManager.XWidth; }
        /// <summary>
        /// Y width of a single collider based on the mesh provided, in Unity units.
        /// </summary>
        public float SingleColliderYWidth { get => continuousCollidersManager.YWidth; }
        /// <summary>
        /// X Center of interactable (across the width of the finger).
        /// </summary>
        public float OffsetX { get => continuousCollidersManager.OffsetX; }
        /// <summary>
        /// Y Center of interactable (along the length of the finger).
        /// </summary>
        public float OffsetY { get => continuousCollidersManager.OffsetY; }
        /// <summary>
        /// The X resolution of the associated mesh.
        /// This is the number of vertices along the length of the finger(s)
        /// </summary>
        public int MeshXResolution { get => continuousCollidersManager.MeshXResolution;}
        /// <summary>
        /// The Y resolution of the associated mesh.
        /// This is the number of vertices along the width of the finger(s)
        /// </summary>
        public int MeshYResolution { get => continuousCollidersManager.MeshYResolution;}

        private MeshContinuousCollidersManager continuousCollidersManager;

        /// <summary>
        /// The associated Colliders Manager.
        /// </summary>
        public MeshContinuousCollidersManager ContinuousCollidersManager
        {
            get => continuousCollidersManager;
        }

        protected override void Awake()
        {
            base.Awake();
            continuousCollidersManager = GetComponent<MeshContinuousCollidersManager>();
            Debug.Assert(continuousCollidersManager!=null);
            colliders.AddRange(continuousCollidersManager.SetupColliders());
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
                position = continuousCollidersManager.GetSurfacePointForCollider(info.collider) + offsetOnCollider;
                return true;
            }
            position = Vector2.zero;
            return false;
        }
    }
}
