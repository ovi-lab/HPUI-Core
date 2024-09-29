namespace ubco.ovilab.HPUI.Interaction
{
    public interface IHPUIContinuousInteractable: IHPUIInteractable
    {
        /// <summary>
        /// The size along the abduction-adduction axis of the fingers (x-axis of joints) in unity units.
        /// </summary>
        public float X_size { get; }

        /// <summary>
        /// The size along the flexion-extension axis of the fingers (z-axis of joints) in unity units.
        /// </summary>
        public float Y_size { get; }
    }
}
