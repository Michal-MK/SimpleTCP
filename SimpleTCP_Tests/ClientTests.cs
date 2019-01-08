using System;
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
	}
}
