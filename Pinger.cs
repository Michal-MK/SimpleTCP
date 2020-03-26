using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Igor.TCP {

	/// <summary>
	/// Slight modification to original work from here:
	/// <see cref="https://stackoverflow.com/questions/13911473/multithreading-c-sharp-gui-ping-example"/>
	/// <para>Example Usages:</para>
	/// <code>
	/// PingAll("1.2.3.4");
	/// </code>
	/// <code>
	/// PingAll("1.2.3.1-255");
	/// </code>
	/// <code>
	/// PingAll("1.2.3,7.1-255")
	/// </code>
	/// <code>
	/// PingAll("1.2.3-5.1-255")
	/// </code>
	/// Use ',' to select exact values and '-' to specify a range
	/// </summary>
	public static class Pinger {
		private const byte ICMP_ECHO = 8;
		private const byte ICMP_ECHOREPLY = 0;
		private const int OFFSET_ID = 4;
		private const int OFFSET_CHECKSUM = 2;
		private const int IP_HEADER_LEN = 20;
		private const int ICMP_HEADER_LEN = 8;

		public static IEnumerable<PingerHost> PingAll(string subNets, int timeOut = 1500) {
			ushort PACKET_ID = (ushort)new Random().Next(0, ushort.MaxValue);

			//Init
			using (Socket rawSock = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp)) {
				rawSock.Bind(new IPEndPoint(IPAddress.Any, 0));

				rawSock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, 255);
				rawSock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, int.MaxValue);
				rawSock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, int.MaxValue);
				rawSock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);

				HashSet<PingerHost> aliveIPs = new HashSet<PingerHost>();

				//** Receiver **
				Task receiver = Task.Factory.StartNew(() => {
					byte[] bytesRecv = new byte[64];
					EndPoint remoteAddr = new IPEndPoint(IPAddress.Any, 0);

					while (true) {
						try {
							rawSock.ReceiveFrom(bytesRecv, ref remoteAddr);
						}
						catch { return; };

						ushort replyId = BitConverter.ToUInt16(bytesRecv, IP_HEADER_LEN + OFFSET_ID);
						if (bytesRecv[IP_HEADER_LEN] == ICMP_ECHOREPLY && replyId == PACKET_ID) {
							long ticksInPong = BitConverter.ToInt64(bytesRecv, IP_HEADER_LEN + ICMP_HEADER_LEN);
							int duration = (int)((DateTime.Now.Ticks - ticksInPong) / TimeSpan.TicksPerMillisecond);
							var host = new PingerHost(((IPEndPoint)remoteAddr).Address.ToString(), duration);

							lock (aliveIPs) {
								aliveIPs.Add(host);
							}
						}
					}
				}, TaskCreationOptions.LongRunning);

				Task.Yield(); //Give a chance to listener task to start.

				//** Sender **
				foreach (var ip in GetIPAddresses(subNets)) {
					byte[] packet = CreatePacket(PACKET_ID, BitConverter.GetBytes(DateTime.Now.Ticks));
					IPEndPoint dest = new IPEndPoint(IPAddress.Parse(ip), 0);
					try {
						rawSock.SendTo(packet, dest);
					}
					catch (Exception ex) {
						Console.WriteLine(ex.Message);
						Console.WriteLine($"==>{ip}");
					}
				}

				Task.WaitAny(receiver, Task.Delay(timeOut));
				return aliveIPs;
			}
		}

		public static Task<IEnumerable<PingerHost>> PingAllAsync(string subNets, int TimeOut = 1500) {
			return Task.Run(() => PingAll(subNets, TimeOut));
		}

		static byte[] CreatePacket(ushort id, byte[] data) {
			byte[] packet = new byte[ICMP_HEADER_LEN + data.Length];
			packet[0] = ICMP_ECHO;

			Array.Copy(BitConverter.GetBytes(id), 0, packet, OFFSET_ID, 2);
			Array.Copy(data, 0, packet, ICMP_HEADER_LEN, data.Length);

			Array.Copy(BitConverter.GetBytes(GetChecksum(packet)), 0, packet, OFFSET_CHECKSUM, 2); //copy checksum

			return packet;
		}


		private static ushort GetChecksum(byte[] bytes) {
			ulong sum = 0;
			int i;

			for (i = 0; i < bytes.Length - 1; i += 2) {
				sum += BitConverter.ToUInt16(bytes, i);
			}
			if (i != bytes.Length)
				sum += bytes[i];

			sum = (sum >> 16) + (sum & 0xFFFF);
			sum += sum >> 16;
			return (ushort)(~sum);
		}

		private static IEnumerable<string> GetIPAddresses(string ip) {
			string[] parts = ip.Split('.');
			if (parts.Length != 4) throw new FormatException("Invalid format");
			return from p1 in GetRange(parts[0])
				   from p2 in GetRange(parts[1])
				   from p3 in GetRange(parts[2])
				   from p4 in GetRange(parts[3])
				   select string.Format("{0}.{1}.{2}.{3}", p1, p2, p3, p4);
		}

		private static IEnumerable<int> GetRange(string s) {
			foreach (var part in s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
				var range = part.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
				if (range.Length > 2) throw new FormatException($"Invalid Format");
				if (range.Length == 1) yield return int.Parse(range[0]);
				else {
					for (int i = int.Parse(range[0]); i <= int.Parse(range[1]); i++) {
						yield return i;
					}
				}
			}
		}
	}
}