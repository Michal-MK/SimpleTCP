using Igor.TCP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTCP_Tests {

	[TestClass]
	public class PingerTests {

		[TestMethod]
		public void RangeTest() {

			IEnumerable<PingerHost> hosts = Pinger.PingAll("192.168.1.1-254");
			string activeAddress = SimpleTCPHelper.GetActiveIPv4Address().ToString();

			Assert.IsTrue(hosts.Where(s => s.IP == activeAddress).Count() == 1);
		}
	}
}
