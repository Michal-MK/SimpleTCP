using System;

namespace Igor.TCP {
	[Serializable]
	class ServerStartException : Exception {
		internal ServerStartException(string message) : base(message) { }
	}
}
