using System;

namespace Igor.TCP {
	[Serializable]
	internal class ServerStartException : Exception {
		internal ServerStartException(string message) : base(message) { }
	}
}
