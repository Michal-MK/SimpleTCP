using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Structures;

namespace SimpleTCP.Tests {
	[TestClass]
	public class SimpleTCPHelperTests {
		[TestMethod]
		public async Task ObtainIPAddressAsync() {
			string ip = RefImplementation();
			
			NetworkAddressState state = await SimpleTCPHelper.GetActiveIPv4AddressAsync();
			Assert.AreEqual(ip, state.Address.ToString());
		}	
		
		[TestMethod]
		public void ObtainIPAddress() {
			string ip = RefImplementation();
			
			IPAddress address = SimpleTCPHelper.GetActiveIPv4Address();
			Assert.AreEqual(ip, address.ToString());
		}

		private static string RefImplementation() {
			using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, 0);
			socket.Connect("8.8.8.8", 65530);
			IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
			return endPoint.Address.ToString();
		}
	}
}