
namespace EVEMon.Common.EmailProvider
{
    public sealed class YahooPlusProvider : IEmailProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name => "Yahoo Plus";

        /// <summary>
        /// Gets the server address.
        /// </summary>
        /// <value>The server address.</value>
        public string ServerAddress => "plus.smtp.mail.yahoo.com";

        /// <summary>
        /// Gets the server port.
        /// </summary>
        /// <value>The server port.</value>
        public int ServerPort => 465;

        /// <summary>
        /// Gets a value indicating whether the server requires SSL.
        /// </summary>
        /// <value><c>true</c> if the server requires SSL; otherwise, <c>false</c>.</value>
        public bool RequiresSsl => true;

        /// <summary>
        /// Gets a value indicating whether the server requires authentication.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the server requires authentication ; otherwise, <c>false</c>.
        /// </value>
        public bool RequiresAuthentication => true;
    }
}
