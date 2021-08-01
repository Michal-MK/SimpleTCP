using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Events;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	[TestClass]
	public class ServerClientTests_Primitives : TestBase {
		const string SERVER_STRING = "Hello from server";
		const string CLIENT_STRING = "Hello from client";

		const Int64 SERVER_LONG = 1000;
		const Int64 CLIENT_LONG = 10000;

		int eventsPassed;

		[TestMethod]
		public async Task SendPrimitives() {

			using TCPServer server = new(new ServerConfiguration());

			using TCPClient client = new(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);

			server.OnClientConnected += (_, e) => {
				server.GetConnection(e.ClientInfo.ID).OnStringReceived += ServerClientTests_OnStringReceived;
				server.GetConnection(e.ClientInfo.ID).OnInt64Received += ServerClientTests_OnInt64Received;

				server.GetConnection(e.ClientInfo.ID).SendData(SERVER_STRING);
				server.GetConnection(e.ClientInfo.ID).SendData(SERVER_LONG);
			};

			await client.ConnectAsync(1000);
			
			client.OnStringReceived += GetConnection_OnStringReceived;
			client.OnInt64Received += GetConnection_OnInt64Received;

			client.Connection!.SendData(CLIENT_STRING);
			client.Connection!.SendData(CLIENT_LONG);

			await Task.Run(Wait);
			Assert.IsTrue(eventsPassed == 4);

			server.Stop();
		}

		private void GetConnection_OnInt64Received(object sender, PacketReceivedEventArgs<long> e) {
			if (e.Data == SERVER_LONG && e.ClientID == TCPServer.SERVER_PACKET_ORIGIN_ID) {
				eventsPassed++;
				if (eventsPassed == 4) evnt.Set();
			}
		}

		private void GetConnection_OnStringReceived(object sender, PacketReceivedEventArgs<string> e) {
			if (e.Data == SERVER_STRING && e.ClientID == TCPServer.SERVER_PACKET_ORIGIN_ID) {
				eventsPassed++;
				if (eventsPassed == 4) evnt.Set();
			}
		}

		private void ServerClientTests_OnInt64Received(object sender, PacketReceivedEventArgs<long> e) {
			if (e.Data == CLIENT_LONG && e.ClientID == 1) {
				eventsPassed++;
				if (eventsPassed == 4) evnt.Set();
			}
		}

		private void ServerClientTests_OnStringReceived(object sender, PacketReceivedEventArgs<string> e) {
			if (e.Data == CLIENT_STRING && e.ClientID == 1) {
				eventsPassed++;
				if (eventsPassed == 4) evnt.Set();
			}
		}
	}
}