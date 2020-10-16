﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Igor.TCP {
	public partial class ServerClientTests {
		#region Property synchronization

		public string[] serverProperty { get; set; } = new string[] { "Hello", "Server" };
		string[] clientProperty { get; set; } = null;
		public string[] clientPropertyPublic { get; set; } = null;

		[TestMethod]
		public async Task SynchronizeProp() {

			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55552);

			await server.Start(55552);
			await client.ConnectAsync(1000);

			const byte PROP_ID = 4;

			server.SyncProperty(1, this, nameof(serverProperty), PROP_ID);

			Assert.ThrowsException<InvalidOperationException>(() => { client.SyncProperty(this, nameof(clientProperty), PROP_ID); });
			Assert.ThrowsException<NotImplementedException>(() => { client.SyncProperty(this, "Nonexistent Property Name", PROP_ID); });

			client.SyncProperty(this, nameof(clientPropertyPublic), PROP_ID);

			server.UpdateProp(1, PROP_ID, serverProperty);

			await Task.Delay(100);;
			Assert.IsNotNull(clientPropertyPublic);
			Assert.IsTrue(clientPropertyPublic[0] == "Hello" && clientPropertyPublic[1] == "Server");

			server.Stop();
		}
		#endregion


		#region PropertySynch Event

		public string myProp { get; set; } = "Hello Property";
		public string myPropClient { get; set; } = "Hello";

		bool[] checks;

		const byte SYNC_ID = 88;

		[TestMethod]
		public async Task SynchronizationEventClient() {
			TCPServer server = new TCPServer(new ServerConfiguration());

			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55551);

			await server.Start(55551);
			await client.ConnectAsync(1000);

			client.OnPropertySynchronized += Client_OnPropertySynchronized;

			server.SyncProperty(1, this, nameof(myProp), SYNC_ID);

			client.SyncProperty(this, nameof(myPropClient), SYNC_ID);

			server.UpdateProp(1, SYNC_ID, myProp);

			await Task.Delay(100);

			for (int i = 0; i < checks.Length; i++) {
				Assert.IsTrue(checks[i]);
			}

			server.Stop();
		}

		private void Client_OnPropertySynchronized(object sender, OnPropertySynchronizationEventArgs e) {
			checks = new bool[3];

			checks[0] = e.PropertyName == nameof(myPropClient);
			checks[1] = e.SynchronizationPacketID == SYNC_ID;
			checks[2] = myProp == myPropClient;
		}
		#endregion

	}
}
