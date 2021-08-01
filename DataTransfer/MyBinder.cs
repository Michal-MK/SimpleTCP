using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace SimpleTCP.DataTransfer {
	sealed class MyBinder : SerializationBinder {
		public override Type BindToType(string assemblyName, string typeName) {
			Type ttd = null;
			string currentExecutingAssembly = Assembly.GetExecutingAssembly().FullName;
			bool isSystemType = !assemblyName.Contains("PublicKeyToken=null");

			string originalAssemblyName = assemblyName.Split(',')[0];
			string wantedAssemblyName = isSystemType ? originalAssemblyName : currentExecutingAssembly.Split(',')[0];

			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				if (assembly.FullName.Split(',')[0] != (isSystemType ? originalAssemblyName : wantedAssemblyName)) continue;
				
				if (typeName.Contains("PublicKeyToken")) {
					string[] split = typeName.Split(',');
					split[1] = " " + wantedAssemblyName;
					typeName = string.Join(",", split);
				}
				ttd = assembly.GetType(typeName);
				break;
			}
			return ttd;
		}
	}
}