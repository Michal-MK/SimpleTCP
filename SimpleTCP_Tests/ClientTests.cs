using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleTCP.Structures;
using SimpleTCP.Tests.Base;

namespace SimpleTCP.Tests {
	[TestClass]
	public class ClientTests : TestBase {
		#region Client construction defaults

		[TestMethod]
		public void ClientStart() {
			TCPClient client = new(SimpleTCPHelper.GetActiveIPv4Address(), 55555);

			Assert.ThrowsException<InvalidOperationException>(() => client.Info.ID == 255);
			Assert.IsTrue(client.Info.Name == Environment.UserName);
			Assert.IsNull(client.Connection);
		}

		#endregion

		#region Definition of requests and packets

		private int matches;

		[TestMethod]
		public async Task Define() {
			using TCPClient client = new(SimpleTCPHelper.GetActiveIPv4Address(), 4245);
			using TCPServer server = new(new ServerConfiguration());
			using ManualResetEventSlim localEvnt = new();

			await server.Start(4245);

			server.OnClientConnected += (_, e) => {
				localEvnt.Wait();
				server.GetConnection(e.ClientInfo.ID).SendData(55, new TestStruct { a = 50 });
				server.GetConnection(e.ClientInfo.ID).SendData(56, new MyClass());
				server.GetConnection(e.ClientInfo.ID).SendData(57, new C2());
				server.GetConnection(e.ClientInfo.ID).SendData(58, new Text<int>());
				server.GetConnection(e.ClientInfo.ID).SendData(59, new List<MyClass>());
			};


			bool res = await client.ConnectAsync(1000);

			if (!res) {
				Assert.Fail();
			}
			
			client.DefineCustomPacket<TestStruct>(55, (_, _) => Match());
			client.DefineCustomPacket<MyClass>(56, (_, _) => Match());
			client.DefineCustomPacket<C2>(57, (_, _) => Match());
			client.DefineCustomPacket<Text<int>>(58, (_, _) => Match());
			client.DefineCustomPacket<List<MyClass>>(59, (_, _) => Match());
			
			localEvnt.Set();
			
			await Task.Run(Wait);

			Assert.IsTrue(evnt.IsSet);
			
			void Match() {
				matches++;
				if (matches == 5) evnt.Set();
			}
		}

		[Serializable]
		public struct TestStruct {
			public int a;
		}

		[Serializable]
		public class MyClass {
			public int a;
		}

		[Serializable]
		public abstract class C1 { }

		[Serializable]
		public class C2 : C1 { }

		[Serializable]
		public class Text<TText> {
			public TText field;
		}

		#endregion
	}
}