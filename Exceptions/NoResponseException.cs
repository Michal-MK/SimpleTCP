using System;
using System.Text;
using SimpleTCP.Structures;

namespace SimpleTCP.Exceptions {
	[Serializable]
	public class NoResponseException : Exception {
		public NoResponseException(TCPResponse response) : base(Encoding.ASCII.GetString(response.RawData)) { }
	}
}