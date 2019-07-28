﻿using System;
using System.Net;

namespace Igor.TCP {
	/// <summary>
	/// Basic information about this client, can be sent to sever
	/// </summary>
	[Serializable]
	public class TCPClientInfo {
		/// <summary>
		/// Is current client playing the role of the server
		/// </summary>
		public bool IsServer { get; }

		/// <summary>
		/// Is current client playing the role of the client
		/// </summary>
		public bool IsClient => !IsServer;

		/// <summary>
		/// Address of current instance
		/// </summary>
		public IPAddress Address { get; }

		/// <summary>
		/// The name of connected client, if not set up users computer name is used.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The ID assigned from server
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		public byte ClientID {
			get {
				if (IsValid) {
					return clientID;
				}
				throw new InvalidOperationException("Client has not been given an ID yet");
			}
			internal set {
				clientID = value;
			}
		}
		private byte clientID = 255;

		internal bool IsValid => clientID != 255;
		/// <summary>
		/// Initialize new <see cref="TCPClientInfo"/>
		/// </summary>
		public TCPClientInfo(string computerName, bool isServer, IPAddress clientAddress) {
			IsServer = isServer;
			Address = clientAddress;
			Name = computerName;
		}
	}
}