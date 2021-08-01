using System;
using System.Collections.Generic;

namespace SimpleTCP.Connections.Interfaces {
	internal interface IValueProvider {
		Dictionary<byte, Delegate> ProvidedValues { get; }
	}
}
