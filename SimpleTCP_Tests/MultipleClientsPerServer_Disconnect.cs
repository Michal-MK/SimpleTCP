using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Enums;
using SimpleTCP.Events;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	public class MultipleClientsPerServer_Disconnect : TestBase {
		private bool client1Disconnect;
		private bool client2Disconnect;

		[TestMethod]
		public async Task DisconnectingMultipleClients() {
			using TCPServer server = new(new ServerConfiguration());
			server.OnClientDisconnected += Server_OnClientDisconnected;

			using TCPClient client1 = new(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client1.SetUpClientInfo("Client 1");
			using TCPClient client2 = new(SimpleTCPHelper.GetActiveIPv4Address(), 55550);
			client2.SetUpClientInfo("Client 2");

			await server.Start(55550);

			await client1.ConnectAsync(1000);
			await client2.ConnectAsync(1000);

			client1.Disconnect();
			client2.Disconnect();

			await Task.Run(Wait);

			server.Stop();

			if (!(client1Disconnect && client2Disconnect)) {
				throw new Exception("Disconnect invalid data returned");
			}
		}

		private void Server_OnClientDisconnected(object sender, ClientDisconnectedEventArgs e) {
			if (e.ClientInfo.ID == 1) {
				client1Disconnect = e.DisconnectType == DisconnectType.Success && e.ClientInfo.IsServer == false && e.ClientInfo.ID == 1 && e.ClientInfo.Name == "Client 1";
				if (client1Disconnect && client2Disconnect) evnt.Set();
			}
			else {
				client2Disconnect = e.DisconnectType == DisconnectType.Success && e.ClientInfo.IsServer == false && e.ClientInfo.ID == 2 && e.ClientInfo.Name == "Client 2";
				if (client1Disconnect && client2Disconnect) evnt.Set();
			}
		}
	}
}