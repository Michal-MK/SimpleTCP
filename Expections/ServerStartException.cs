using System;

namespace Igor.TCP {
	[Serializable]
	class ServerStartException : Exception {
		public ServerStartException(string message) : base(message) { }
	}
}
