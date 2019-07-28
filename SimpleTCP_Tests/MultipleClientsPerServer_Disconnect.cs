using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Igor.TCP {

	public partial class MultipleClientsPerServer {

		private bool client1Disconnect;
		private bool client2Disconnect;

		[TestMethod]
		public async Task DisconnectingMultipleClients() {

			TCPServer server = new TCPServer(new ServerConfiguration());
			server.OnClientDisconnected += Server_OnClientDisconnected;

			TCPClient client1 = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client1.SetUpClientInfo("Client 1");
			TCPClient client2 = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client2.SetUpClientInfo("Client 2");

			await server.Start(55550);

			client1.Connect(null);
			client2.Connect(null);

			await Task.Delay(100);

			client1.Disconnect();
			await Task.Delay(400);
			client2.Disconnect();

			await Task.Delay(200);
			await server.Stop();

			if(!(client1Disconnect && client2Disconnect)) {
				throw new Exception("Disconnect invalid data returned");
			}
		}

		private void Server_OnClientDisconnected(object sender, ClientDisconnectedEventArgs e) {
			if(e.ClientInfo.ClientID == 1) {
				client1Disconnect = e.DisconnectType == Enums.DisconnectType.Success && e.ClientInfo.IsServer == false && e.ClientInfo.ClientID == 1 && e.ClientInfo.Name == "Client 1";
			}
			else {
				client2Disconnect = e.DisconnectType == Enums.DisconnectType.Success && e.ClientInfo.IsServer == false && e.ClientInfo.ClientID == 2 && e.ClientInfo.Name == "Client 2";
			}
		}
	}
}
