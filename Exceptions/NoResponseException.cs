using System;
using System.Text;

namespace Igor.TCP {
	[Serializable]
	public class NoResponseException : Exception {
		public NoResponseException(TCPResponse response) : base(Encoding.ASCII.GetString(response.RawData)) { }
	}
}