﻿using System;
using SimpleTCP.Enums;
using SimpleTCP.Structures;

namespace SimpleTCP.Events {
	/// <summary>
	/// Client Disconnected event data
	/// </summary>
	public class ClientDisconnectedEventArgs: EventArgs {

		internal ClientDisconnectedEventArgs(TCPClientInfo clientInfo, DisconnectType disconnectType) {
			ClientInfo = clientInfo;
			DisconnectType = disconnectType;
		}

		/// <summary>
		/// ID of the disconnected client
		/// </summary>
		public TCPClientInfo ClientInfo { get; }

		/// <summary>
		/// The way client disconnected
		/// </summary>
		public DisconnectType DisconnectType { get; }
	}

}