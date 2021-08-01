using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	[TestClass]
	public class MultipleClientsPerServer_Reroute : TestBase {
		private bool callbackSuccess;
		private bool rerouteCallbackSuccess;

		[TestMethod]
		public async Task Rerouting() {
			using TCPServer server = new(new ServerConfiguration());

			using TCPClient client1 = new(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client1.SetUpClientInfo("Client 1");
			using TCPClient client2 = new(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client2.SetUpClientInfo("Client 2");

			await server.Start(55550);
			const byte PACKET = 4;

			await client1.ConnectAsync(1000);
			client1.DefineCustomPacket<string[]>(PACKET, OnMyStringArrayReceived);

			await client2.ConnectAsync(1000);
			client2.DefineCustomPacket<string[]>(PACKET, OnClientStringsReceived);

			server.DefineCustomPacket<string[]>(1, PACKET, OnServerReceivedStringArray);
			server.DefineRerouteID(1, 2, PACKET);

			client1.SendData(PACKET, new[] { "Hello", "World" });
			server.GetConnection(1).SendData(PACKET, new[] { "Hello", "World" });

			await Task.Run(Wait);

			Assert.IsTrue(callbackSuccess);
			Assert.IsTrue(rerouteCallbackSuccess);

			server.Stop();
		}

		private void OnServerReceivedStringArray(byte sender, string[] data) {
			/*Only definition needed*/
		}

		private void OnClientStringsReceived(byte sender, string[] data) {
			if (data[0] == "Hello" && data[1] == "World" && sender == 1) {
				rerouteCallbackSuccess = true;
				if (rerouteCallbackSuccess && callbackSuccess) evnt.Set();
			}
		}

		private void OnMyStringArrayReceived(byte sender, string[] data) {
			if (data[0] == "Hello" && data[1] == "World" && sender == 0) {
				callbackSuccess = true;
				if (rerouteCallbackSuccess && callbackSuccess) evnt.Set();
			}
		}
	}
}