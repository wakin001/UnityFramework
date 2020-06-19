using ILRuntime.CLR.Method;
using System;

public class ProtobufRegister
{
	static bool InitedILRuntime = false;
	static IMethod s_HFInitialize;
	static IMethod s_HFUpdate;
	static ILRuntime.Runtime.Enviorment.AppDomain HFDomain;

	public void InitializeILRuntimeCLR()
	{
		ProtoBuf.PType.RegisterFunctionCreateInstance(PType_CreateInstance);
		ProtoBuf.PType.RegisterFunctionGetRealType(PType_GetRealType);
	}
	public void Initialize()
	{
		var hfMain = HFDomain.GetType("HotFix.Main");
		s_HFInitialize = hfMain.GetMethod("Initialize", 0);
		s_HFUpdate = hfMain.GetMethod("Update", 0);
		HFDomain.Invoke(s_HFInitialize, null, null);
	}
	public static void Update()
	{
		if (InitedILRuntime)
		{
			HFDomain.Invoke(s_HFUpdate, null, null);
		}
	}

	static object PType_CreateInstance(string typeName)
	{
		return HFDomain.Instantiate(typeName);
	}
	static Type PType_GetRealType(object o)
	{
		var type = o.GetType();
		if (type.FullName == "ILRuntime.Runtime.Intepreter.ILTypeInstance")
		{
			var ilo = o as ILRuntime.Runtime.Intepreter.ILTypeInstance;
			type = ProtoBuf.PType.FindType(ilo.Type.FullName);
		}
		return type;
	}
}
