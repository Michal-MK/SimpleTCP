using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Igor.TCP;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SimpleTCP_Tests {

	[TestClass]
	public class ServerClientTests {

		#region Connecting to server

		private bool clientConnectedEventFired = false;

		[TestMethod]
		public async Task Connect() {
			TCPServer server = new TCPServer(new ServerConfiguration());
			server.OnConnectionEstablished += Server_OnConnectionEstablished;
			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);
			client.Connect();

			await Task.Run(() => { Thread.Sleep(50); });

			Assert.IsTrue(clientConnectedEventFired);
			Assert.IsTrue(server.getConnectedClients.Length == 1);
			Assert.IsNotNull(client.getConnection);

			Assert.IsTrue(server.getConnectedClients[0].isServer == false);

			Assert.IsTrue(Equals(server.getConnectedClients[0].clientAddress, SimpleTCPHelper.GetActiveIPv4Address()));
			Assert.IsTrue(server.getConnectedClients[0].computerName == Environment.UserName);
			Assert.IsTrue(server.getConnectedClients[0].clientID == 1);

			Assert.IsTrue(client.getConnection.listeningForData);
			Assert.IsTrue(client.getConnection.sendingData);
			Assert.IsTrue(client.clientInfo.clientID == 1);

			await server.Stop();
		}

		private void Server_OnConnectionEstablished(object sender, ClientConnectedEventArgs e) {
			clientConnectedEventFired = true;
		}

		#endregion


		#region Disconnect From server

		bool disconnectEvent = false;

		[TestMethod]
		public async Task Disconnect() {
			TCPServer server = new TCPServer(new ServerConfiguration());
			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			server.OnClientDisconnected += Server_OnClientDisconnected;

			await server.Start(55550);
			client.Connect();

			await Task.Run(() => { Thread.Sleep(50); });

			client.Disconnect();

			await Task.Run(() => { Thread.Sleep(100); });

			Assert.IsTrue(server.getConnectedClients.Length == 0);
			Assert.ThrowsException<NullReferenceException>(() => { server.GetConnection(1); });
			Assert.IsNull(client.getConnection);
			Assert.IsTrue(disconnectEvent);

			await server.Stop();
		}

		private void Server_OnClientDisconnected(object sender, ClientDisconnectedEventArgs e) {
			disconnectEvent = true;
		}

		#endregion


		#region Simple Requests and responses

		[TestMethod]
		public async Task RequestResponse() {
			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);
			client.Connect();

			await Task.Run(() => { Thread.Sleep(50); });

			const byte PACKET_ID = 4;

			server.DefineTwoWayComunication<string>(1, PACKET_ID, ServerString);
			client.DefineTwoWayComunication<string>(PACKET_ID, ClientString);

			TCPResponse resp = await server.RaiseRequestAsync(1, PACKET_ID);

			Assert.IsTrue(resp.dataType == typeof(string));
			Assert.IsTrue(resp.packetID == PACKET_ID);
			Assert.IsTrue(resp.getObject.ToString() == ClientString());

			TCPResponse resp2 = await client.RaiseRequestAsync(PACKET_ID);

			Assert.IsTrue(resp2.dataType == typeof(string));
			Assert.IsTrue(resp2.packetID == PACKET_ID);
			Assert.IsTrue(resp2.getObject.ToString() == ServerString());

			await server.Stop();
		}

		private string ClientString() {
			return "Clients string";
		}

		private string ServerString() {
			return "Servers string";
		}

		#endregion


		#region Sending primitive types from both sides

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
			client.Connect();

			Thread.Sleep(50);

			server.GetConnection(1).OnStringReceived += ServerClientTests_OnStringReceived;
			server.GetConnection(1).OnInt64Received += ServerClientTests_OnInt64Received;

			client.getConnection.OnStringReceived += GetConnection_OnStringReceived;
			client.getConnection.OnInt64Received += GetConnection_OnInt64Received;

			server.GetConnection(1).SendData(SERVER_STRING);
			server.GetConnection(1).SendData(SERVER_LONG);

			client.getConnection.SendData(CLIENT_STRING);
			client.getConnection.SendData(CLIENT_LONG);

			await Task.Run(() => { Thread.Sleep(50); });

			Assert.IsTrue(eventsPassed == 4);

			await server.Stop();
		}

		private void GetConnection_OnInt64Received(object sender, PacketReceivedEventArgs<long> e) {
			if (e.data == SERVER_LONG && e.clientID == TCPServer.ServerPacketOrigin) {
				eventsPassed++;
			}
		}

		private void GetConnection_OnStringReceived(object sender, PacketReceivedEventArgs<string> e) {
			if (e.data == SERVER_STRING && e.clientID == TCPServer.ServerPacketOrigin) {
				eventsPassed++;
			}
		}

		private void ServerClientTests_OnInt64Received(object sender, PacketReceivedEventArgs<long> e) {
			if (e.data == CLIENT_LONG && e.clientID == 1) {
				eventsPassed++;
			}
		}

		private void ServerClientTests_OnStringReceived(object sender, PacketReceivedEventArgs<string> e) {
			if (e.data == CLIENT_STRING && e.clientID == 1) {
				eventsPassed++;
			}
		}

		#endregion


		#region Reconnect to the server

		[TestMethod]
		public async Task ReconnectToServer() {
			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);
			client.Connect();

			await Task.Run(() => { Thread.Sleep(50); });

			client.Disconnect();

			await Task.Run(() => { Thread.Sleep(50); });

			Assert.IsTrue(client.getConnection == null);

			client.Connect();

			await Task.Run(() => { Thread.Sleep(50); });

			Assert.IsTrue(client.clientInfo.clientID == 1);
			Assert.IsTrue(client.getConnection != null);
			Assert.IsTrue(client.getConnection.listeningForData);
			Assert.IsTrue(client.getConnection.sendingData);
			Assert.IsTrue(server.getConnectedClients.Length == 1);
			Assert.IsTrue(server.getConnectedClients[0].clientID == 1);
			Assert.IsTrue(server.GetConnection(1).sendingData);
			Assert.IsTrue(server.GetConnection(1).listeningForData);

			await server.Stop();
		}

		#endregion

		#region Property synchronization

		public string[] serverProperty { get; set; } = new string[] { "Hello", "Server" };
		string[] clientProperty { get; set; } = null;
		public string[] clientPropertyPublic { get; set; } = null;

		[TestMethod]
		public async Task SynchronizeProp() {

			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55550);

			await server.Start(55550);
			client.Connect();

			Thread.Sleep(50);

			const byte PROP_ID = 4;

			server.SyncPropery(1, this, nameof(serverProperty), PROP_ID);

			Assert.ThrowsException<InvalidOperationException>(() => { client.SyncPropery(this, nameof(clientProperty), PROP_ID); });
			Assert.ThrowsException<NotImplementedException>(() => { client.SyncPropery(this, "a", PROP_ID); });

			client.SyncPropery(this, nameof(clientPropertyPublic), PROP_ID);

			server.UpdateProp(1, PROP_ID, serverProperty);

			await Task.Run(() => { Thread.Sleep(50); });

			Assert.IsNotNull(clientPropertyPublic);
			Assert.IsTrue(clientPropertyPublic[0] == "Hello" && clientPropertyPublic[1] == "Server");

			await server.Stop();
		}
		#endregion
	}
}
