﻿//this source code was auto-generated by tolua#, do not modify it
using System;
using LuaInterface;

public class Framework_Singleton_Framework_LuaUtilityWrap
{
	public static void Register(LuaState L)
	{
		L.BeginClass(typeof(Framework.Singleton<Framework.LuaUtility>), typeof(Framework.SingletonBase), "Singleton_Framework_LuaUtility");
		L.RegFunction("__tostring", ToLua.op_ToString);
		L.RegVar("singleton", get_singleton, null);
		L.EndClass();
	}

	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static int get_singleton(IntPtr L)
	{
#if UNITY_EDITOR
        ToluaProfiler.AddCallRecord("Framework.Singleton<Framework.LuaUtility>.singleton");
#endif
		try
		{
			ToLua.PushObject(L, Framework.Singleton<Framework.LuaUtility>.singleton);
			return 1;
		}
		catch (Exception e)
		{
			return LuaDLL.toluaL_exception(L, e);
		}
	}
}

