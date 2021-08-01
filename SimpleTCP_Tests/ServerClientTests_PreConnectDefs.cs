using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	[TestClass]
	public class ServerClientTests_PreConnectDefs : TestBase {
		private string testProp;
		public string TestProp { get => testProp;
			set {
				testProp = value;
				if(AllPassed()) evnt.Set();
			}
		}

		private string getValueString;
		private string clientObtainedValue;

		[TestMethod]
		public async Task PreConnectDefinition() {
			using TCPServer server = new(ServerConfiguration.Builder.Create().Build());
			await server.Start(0);
			server.OnClientConnected += async (_, e) => {
				server.SendToAll(50, "TestCustomPacket");

				getValueString = await server.GetValue<string>(51, e.ClientInfo.ID);
				
				server.UpdateProp(e.ClientInfo.ID,52,"TestPropSync");
			};

			using TCPClient client = new(server.IP!, server.Port, ClientConfiguration.Builder.Create().Build());


			client.DefineCustomPacket<string>(50, (_, e) => {
				clientObtainedValue = e;
				if (AllPassed()) evnt.Set();
			});
			client.ProvideValue(51, () => "TestGetValue");
			client.SyncProperty(this, nameof(TestProp), 52);

			await client.ConnectAsync(2000);

			await Task.Run(Wait);

			server.Stop();
		}

		private bool AllPassed() {
			return TestProp == "TestPropSync" && getValueString == "TestGetValue" && clientObtainedValue == "TestCustomPacket";
		}
	}
}