using System;
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

			client.Connect();

			client.getConnection.dataIDs.DefineCustomDataTypeForID<TestStruct>(55, OnSt_C);
			client.getConnection.dataIDs.DefineCustomDataTypeForID<MyClass>(56, OnNd_C);
			client.getConnection.dataIDs.DefineCustomDataTypeForID<C2>(57, OnRd_C);
			client.getConnection.dataIDs.DefineCustomDataTypeForID<Text<int>>(58, OnSTh_C);
			client.getConnection.dataIDs.DefineCustomDataTypeForID<List<MyClass>>(59, OnSTh_C);



			server.GetConnection(1).dataIDs.DefineCustomDataTypeForID<TestStruct>(55, OnSt_C);
			server.GetConnection(1).dataIDs.DefineCustomDataTypeForID<MyClass>(56, OnNd_C);
			server.GetConnection(1).dataIDs.DefineCustomDataTypeForID<C2>(57, OnRd_C);
			server.GetConnection(1).dataIDs.DefineCustomDataTypeForID<Text<int>>(58, OnSTh_C);
			server.GetConnection(1).dataIDs.DefineCustomDataTypeForID<List<MyClass>>(59, OnSTh_C);
			await Task.Delay(200);

			server.GetConnection(1).SendData(55, SimpleTCPHelper.GetBytesFromObject(new TestStruct { a = 50 }));
			server.GetConnection(1).SendData(56, SimpleTCPHelper.GetBytesFromObject(new MyClass()));
			server.GetConnection(1).SendData(57, SimpleTCPHelper.GetBytesFromObject(new C2()));
			server.GetConnection(1).SendData(58, SimpleTCPHelper.GetBytesFromObject(new Text<int>()));
			server.GetConnection(1).SendData(59, SimpleTCPHelper.GetBytesFromObject(new List<MyClass>()));

			await Task.Delay(500);

			Assert.IsTrue(matches == 5);

		}

		private void OnSt_C(TestStruct arg1, byte arg2) {
			matches++;
		}

		private void OnSTh_C(List<MyClass> arg1, byte arg2) {
			matches++;
		}

		private void OnSTh_C(Text<int> arg1, byte arg2) {
			matches++;
		}

		private void OnRd_C(C2 arg1, byte arg2) {
			matches++;
		}

		private void OnNd_C(MyClass arg1, byte arg2) {
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
