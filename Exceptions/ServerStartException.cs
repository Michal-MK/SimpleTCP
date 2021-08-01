using System;

namespace SimpleTCP.Exceptions {
	[Serializable]
	internal class ServerStartException : Exception {
		internal ServerStartException(string message) : base(message) { }
	}
}
