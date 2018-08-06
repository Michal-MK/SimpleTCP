using System;
using System.Net;
using System.Net.Sockets;

namespace Igor.TCP {
	public static class Helper {
		public static IPAddress GetActivePIv4Address() {
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
				socket.Connect("8.8.8.8", 65530);
				IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
				return endPoint.Address;
			}
		}
	}
}
