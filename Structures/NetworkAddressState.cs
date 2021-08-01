using System.Net;

namespace Igor.TCP {
	public class NetworkAddressState {

		private NetworkAddressState(IPAddress address, bool success) {
			Address = address;
			Success = success;
		}
		
		/// <summary>
		/// The assigned IP address
		/// </summary>
		public IPAddress Address { get; }
		
		/// <summary>
		/// Was the operation successful, if false, the <see cref="Address"/> field contains valid value 
		/// </summary>
		public bool Success { get; }

		internal static NetworkAddressState Connected(IPAddress address) {
			return new(address, true);
		}

		internal static NetworkAddressState Fail() {
			return new(IPAddress.Loopback, false);
		}
	}
}