using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Igor.TCP {

	public partial class MultipleClientsPerServer {

		bool callbackSuccess = false;
		bool rerouteCallbackSuccess = false;

		[TestMethod]
		public async Task Rerouting() {
			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client1 = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client1.SetUpClientInfo("Client 1");
			TCPClient client2 = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client2.SetUpClientInfo("Client 2");

			await server.Start(55550);

			client1.Connect();
			client2.Connect();

			await Task.Delay(100);

			const byte PACKET = 4;

			server.DefineCustomPacket<string[]>(1, PACKET, OnServerReceivedStringArray);
			server.DefineRerouteID(1,2, PACKET);

			client1.DefineCustomPacket<string[]>(PACKET, OnMyStringArrayReceived);
			client2.DefineCustomPacket<string[]>(PACKET, OnClientStringsReceived);

			await Task.Delay(100);

			client1.getConnection.SendData(PACKET, new string[] { "Hello", "World" });
			server.GetConnection(1).SendData(PACKET, new string[] { "Hello", "World" });

			await Task.Delay(100);

			Assert.IsTrue(callbackSuccess);
			Assert.IsTrue(rerouteCallbackSuccess);

			await server.Stop();
		}

		private void OnServerReceivedStringArray(byte sender, string[] data) { /*Only definition needed*/ }

		private void OnClientStringsReceived(byte sender, string[] data) {
			if (data[0] == "Hello" && data[1] == "World" && sender == 1) {
				rerouteCallbackSuccess = true;
			}
		}

		private void OnMyStringArrayReceived(byte sender, string[] data) {
			if (data[0] == "Hello" && data[1] == "World" && sender == 0) {
				callbackSuccess = true;
			}
		}
	}
}
