using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Igor.TCP {
	[TestClass]
	public class ServerClientTests_PropSync : TestBase {
		#region Property synchronization

		public string[] ServerProperty { get; set; } = { "Hello", "Server" };
		public string[] ClientProperty { get; set; } = null;
		
		private string[] clientPropertyPublic;

		public string[] ClientPropertyPublic {
			get => clientPropertyPublic;
			set {
				clientPropertyPublic = value;
				evnt.Set();
			}
		}

		[TestMethod]
		public async Task SynchronizeProp() {

			using TCPServer server = new(new ServerConfiguration());

			using TCPClient client = new(SimpleTCPHelper.GetActiveIPv4Address(), 55552);

			using ManualResetEventSlim localEvnt = new();

			const byte PROP_ID = 4;
			
			await server.Start(55552);

			server.OnClientConnected += (_, e) => {
				server.SyncProperty(1, this, nameof(ServerProperty), PROP_ID);
				localEvnt.Set();
			};

			await client.ConnectAsync(1000);

			await Task.Run(localEvnt.Wait);

			Assert.ThrowsException<NotImplementedException>(() => { client.SyncProperty(this, "Nonexistent Property Name", PROP_ID); });

			client.SyncProperty(this, nameof(ClientPropertyPublic), PROP_ID);
			Assert.ThrowsException<ArgumentException>(() => { client.SyncProperty(this, nameof(ClientProperty), PROP_ID); });

			server.UpdateProp(1, PROP_ID, ServerProperty);

			await Task.Run(Wait);

			Assert.IsNotNull(ClientPropertyPublic);
			Assert.IsTrue(ClientPropertyPublic[0] == "Hello" && ClientPropertyPublic[1] == "Server");

			server.Stop();
		}

		#endregion

		#region PropertySynch Event

		public string MyProp { get; set; } = "Hello Property";
		public string MyPropClient { get; set; } = "Hello";

		private bool[] checks;

		private const byte SYNC_ID = 88;

		[TestMethod]
		public async Task SynchronizationEventClient() {
			using TCPServer server = new(new ServerConfiguration());

			using TCPClient client = new(SimpleTCPHelper.GetActiveIPv4Address(), 55551);

			using ManualResetEventSlim localEvnt = new();

			await server.Start(55551);

			server.OnClientConnected += (_, e) => {
				server.SyncProperty(e.ClientInfo.ID, this, nameof(MyProp), SYNC_ID);
				localEvnt.Set();
			};

			await client.ConnectAsync(1000);
			client.SyncProperty(this, nameof(MyPropClient), SYNC_ID);

			client.OnPropertySynchronized += Client_OnPropertySynchronized;

			await Task.Run(localEvnt.Wait);

			server.UpdateProp(1, SYNC_ID, MyProp);

			await Task.Run(Wait);

			for (int i = 0; i < checks.Length; i++) {
				Assert.IsTrue(checks[i]);
			}

			server.Stop();
		}

		private void Client_OnPropertySynchronized(object sender, OnPropertySynchronizationEventArgs e) {
			checks = new bool[3];

			checks[0] = e.PropertyName == nameof(MyPropClient);
			checks[1] = e.SynchronizationPacketID == SYNC_ID;
			checks[2] = MyProp == MyPropClient;
			evnt.Set();
		}

		#endregion
	}
}