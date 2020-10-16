﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Igor.TCP {
	public partial class ServerClientTests {

		const string SERVER_STRING = "Hello from server";
		const string CLIENT_STRING = "Hello from client";

		const Int64 SERVER_LONG = 1000;
		const Int64 CLIENT_LONG = 10000;

		int eventsPassed = 0;

		[TestMethod]
		public async Task SendPrimitives() {

			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);

			await client.ConnectAsync(1000);
			client.Connection.OnStringReceived += GetConnection_OnStringReceived;
			client.Connection.OnInt64Received += GetConnection_OnInt64Received;

			server.GetConnection(1).OnStringReceived += ServerClientTests_OnStringReceived;
			server.GetConnection(1).OnInt64Received += ServerClientTests_OnInt64Received;

			server.GetConnection(1).SendData(SERVER_STRING);
			server.GetConnection(1).SendData(SERVER_LONG);

			client.Connection.SendData(CLIENT_STRING);
			client.Connection.SendData(CLIENT_LONG);

			await Task.Delay(100);
			Assert.IsTrue(eventsPassed == 4);

			server.Stop();
		}

		private void GetConnection_OnInt64Received(object sender, PacketReceivedEventArgs<long> e) {
			if (e.Data == SERVER_LONG && e.ClientID == TCPServer.SERVER_PACKET_ORIGIN_ID) {
				eventsPassed++;
			}
		}

		private void GetConnection_OnStringReceived(object sender, PacketReceivedEventArgs<string> e) {
			if (e.Data == SERVER_STRING && e.ClientID == TCPServer.SERVER_PACKET_ORIGIN_ID) {
				eventsPassed++;
			}
		}

		private void ServerClientTests_OnInt64Received(object sender, PacketReceivedEventArgs<long> e) {
			if (e.Data == CLIENT_LONG && e.ClientID == 1) {
				eventsPassed++;
			}
		}

		private void ServerClientTests_OnStringReceived(object sender, PacketReceivedEventArgs<string> e) {
			if (e.Data == CLIENT_STRING && e.ClientID == 1) {
				eventsPassed++;
			}
		}
	}
}
