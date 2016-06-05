namespace Genesis.AsyncInitializationGuard
{
    /// <summary>
    /// Defines the possible states in which an <see cref="InitializationGuard"/> may exist.
    /// </summary>
    public enum InitializationGuardState
    {
        /// <summary>
        /// The guard is not yet initialized.
        /// </summary>
        Uninitialized,

        /// <summary>
        /// The guard is currently initializing.
        /// </summary>
        Initializing,

        /// <summary>
        /// The guard has been initialized.
        /// </summary>
        Initialized
    }
}