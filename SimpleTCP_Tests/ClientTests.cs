using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Igor.TCP {

	[TestClass]
	public class ClientTests {

		#region Client construction defaults

		[TestMethod]
		public void ClientStart() {
			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 55555);

			Assert.IsTrue(client.clientInfo.clientID == 255);
			Assert.IsTrue(client.clientInfo.computerName == Environment.UserName);
			Assert.IsNull(client.getConnection);
		}

		#endregion

		#region Definition of requests and packets
		private int matches;


		[TestMethod]
		public async Task Define() {
			TCPClient client = new TCPClient(SimpleTCPHelper.GetActiveIPv4Address(), 4245);

			TCPServer server = new TCPServer(new ServerConfiguration());

			await server.Start(4245);

			client.Connect(() => {
				client.DefineCustomPacket<TestStruct>(55, OnSt_C);
				client.DefineCustomPacket<MyClass>(56, OnNd_C);
				client.DefineCustomPacket<C2>(57, OnRd_C);
				client.DefineCustomPacket<Text<int>>(58, OnSTh_C);
				client.DefineCustomPacket<List<MyClass>>(59, OnSTh_C);
			});


			server.DefineCustomPacket<TestStruct>(1, 55, OnSt_C);
			server.DefineCustomPacket<MyClass>(1, 56, OnNd_C);
			server.DefineCustomPacket<C2>(1, 57, OnRd_C);
			server.DefineCustomPacket<Text<int>>(1, 58, OnSTh_C);
			server.DefineCustomPacket<List<MyClass>>(1, 59, OnSTh_C);

			await Task.Delay(200);

			server.GetConnection(1).SendData(55, new TestStruct { a = 50 });
			server.GetConnection(1).SendData(56, new MyClass());
			server.GetConnection(1).SendData(57, new C2());
			server.GetConnection(1).SendData(58, new Text<int>());
			server.GetConnection(1).SendData(59, new List<MyClass>());

			await Task.Delay(500);

			Assert.IsTrue(matches == 5);

		}

		private void OnSt_C(byte sender, TestStruct arg1) {
			matches++;
		}

		private void OnSTh_C(byte sender, List<MyClass> arg1) {
			matches++;
		}

		private void OnSTh_C(byte sender, Text<int> arg1) {
			matches++;
		}

		private void OnRd_C(byte sender, C2 arg1) {
			matches++;
		}

		private void OnNd_C(byte sender, MyClass arg1) {
			matches++;
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
		public abstract class C1 {

		}
		[Serializable]
		public class C2 : C1 {

		}
		[Serializable]
		public class Text<TText> {

		}

		#endregion
	}
}
