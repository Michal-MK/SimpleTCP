﻿using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Igor.TCP {

	[TestClass]
	public partial class ServerClientTests {

		private bool clientConnectedEventFired = false;

		[TestMethod]
		public async Task Connect() {
			TCPServer server = new TCPServer(new ServerConfiguration());
			server.OnClientConnected += Server_OnConnectionEstablished;
			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);
			bool res = await client.ConnectAsync(1000);

			if (!res) {
				Assert.Fail();
				return;
			}

			Assert.IsTrue(clientConnectedEventFired);
			Assert.IsTrue(server.ConnectedClients.Length == 1);
			Assert.IsNotNull(client.Connection);

			Assert.IsTrue(server.ConnectedClients[0].IsServer == false);

			Assert.IsTrue(server.ConnectedClients[0].Address.ToString() == SimpleTCPHelper.GetActiveIPv4Address().ToString());
			Assert.IsTrue(server.ConnectedClients[0].Name == Environment.UserName);
			Assert.IsTrue(server.ConnectedClients[0].ClientID == 1);

			Assert.IsTrue(client.Connection.ListeningForData);
			Assert.IsTrue(client.Connection.SendingData);
			Assert.IsTrue(client.ClientInfo.ClientID == 1);

			server.Stop();
		}

		private void Server_OnConnectionEstablished(object sender, ClientConnectedEventArgs e) {
			clientConnectedEventFired = true;
		}
	}
}
