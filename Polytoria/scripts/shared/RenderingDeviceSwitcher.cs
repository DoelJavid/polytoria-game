using Godot;
using System;
using System.Collections.Generic;

namespace Polytoria.Shared;

public static class RenderingDeviceSwitcher
{
	public static void Switch(RenderingDeviceEnum to)
	{
		string renderingName = GetRenderingName(to);
		string currentMethod = RenderingServer.GetCurrentRenderingMethod();
		if (currentMethod == renderingName)
		{
			// already using this rendering, nothing to do
			return;
		}

		string exePath = OS.GetExecutablePath();
		OS.CreateProcess(exePath, [.. OS.GetCmdlineArgs(), "--rendering-method", renderingName]);
		Globals.Singleton.Quit(force: true);
		throw new SwitchingRenderingDeviceException();
	}

	public static string GetCurrentDriverName()
	{
		return RenderingServer.GetCurrentRenderingMethod();
	}

	public static string GetRenderingName(RenderingDeviceEnum e)
	{
		return e switch
		{
			RenderingDeviceEnum.Forward => "forward_plus",
			RenderingDeviceEnum.Mobile => "mobile",
			RenderingDeviceEnum.GLCompatibility => "gl_compatibility",
			_ => throw new IndexOutOfRangeException()
		};
	}

	public class SwitchingRenderingDeviceException : Exception { }

	public enum RenderingDeviceEnum
	{
		Forward,
		Mobile,
		GLCompatibility
	}
}
