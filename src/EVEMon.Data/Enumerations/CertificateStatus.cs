namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Represents a certificate's status from a character's point of view.
    /// </summary>
    public enum CertificateStatus
    {
        /// <summary>
        /// The certificate is trained by the char, all prerequisites are met.
        /// </summary>
        Trained,

        /// <summary>
        /// The certificate is not trained yet but at least one prerequisite is satisfied
        /// </summary>
        PartiallyTrained,

        /// <summary>
        /// The certificate is not trained and none of its prerequisites are satisfied
        /// </summary>
        Untrained
    }
}
