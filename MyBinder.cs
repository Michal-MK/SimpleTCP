using System;
using System.Reflection;
using System.Runtime.Serialization;

sealed class MyBinder : SerializationBinder {
	public override Type BindToType(string assemblyName, string typeName) {
		Type ttd = null;
		try {
			bool isSystemType = false;
			if (assemblyName.Contains("PublicKeyToken=null")) {
				assemblyName = Assembly.GetExecutingAssembly().FullName;
			}
			else {
				isSystemType = true;
			}
			string originalAssemblyName = assemblyName.Split(',')[0];
			string wantedAssemblyName = Assembly.GetExecutingAssembly().FullName.Split(',')[0];

			foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies()) {
				if (ass.FullName.Split(',')[0] == (isSystemType ? originalAssemblyName : wantedAssemblyName)) {
					if (typeName.Contains("PublicKeyToken")) {
						string[] split = typeName.Split(',');
						split[1] = " " + wantedAssemblyName;
						typeName = string.Join(",", split);
					}
					ttd = ass.GetType(typeName);
					break;
				}
			}
		}
		catch (Exception e) {
			Console.WriteLine(e.Message);
		}
		return ttd;
	}
}
