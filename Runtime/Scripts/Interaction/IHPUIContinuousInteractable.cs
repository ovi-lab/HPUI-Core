namespace ubco.ovilab.HPUI.Core.Interaction
{
    public interface IHPUIContinuousInteractable: IHPUIInteractable
    {
        /// <summary>
        /// The size along the abduction-adduction axis of the fingers (x-axis of joints) in unity units.
        /// </summary>
        public float X_size { get; }

        /// <summary>
        /// The signed size along the longitudinal axis of the fingers in Unity units.
        /// The reported surface position always increases in logical +y from proximal to distal;
        /// a negative value places the generated surface in the opposite physical z direction.
        /// </summary>
        public float Y_size { get; }
    }
}
