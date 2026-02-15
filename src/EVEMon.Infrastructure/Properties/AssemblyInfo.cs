using System.Runtime.CompilerServices;

// Allow EVEMon.Common to access internal members of types that moved here.
// This enables incremental extraction without breaking callers.
[assembly: InternalsVisibleTo("EVEMon.Common")]
[assembly: InternalsVisibleTo("EVEMon")]
[assembly: InternalsVisibleTo("EVEMon.Tests")]
