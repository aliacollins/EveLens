
namespace EVEMon.Common.EmailProvider
{
    public interface IEmailProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the server address.
        /// </summary>
        /// <value>The server address.</value>
        string ServerAddress { get; }

        /// <summary>
        /// Gets the server port.
        /// </summary>
        /// <value>The server port.</value>
        int ServerPort { get; }

        /// <summary>
        /// Gets a value indicating whether the server requires SSL.
        /// </summary>
        /// <value><c>true</c> if the server requires SSL; otherwise, <c>false</c>.</value>
        bool RequiresSsl { get; }

        /// <summary>
        /// Gets a value indicating whether the server requires authentication.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the server requires authentication ; otherwise, <c>false</c>.
        /// </value>
        bool RequiresAuthentication { get; }
    }
}
