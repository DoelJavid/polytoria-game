// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

#if CREATOR
using Godot;
using Polytoria.Creator.LSP;
#endif
using System.Runtime.InteropServices;

namespace Polytoria.Shared;

public static partial class NativeInit
{
	[LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
	private static partial int chmod(string pathname, int mode);

	public static void Init()
	{
#if CREATOR && GODOT_LINUXBSD
		InitLinuxCreator();
#elif CREATOR && GODOT_MACOS
		InitMacOSCreator();
#endif
	}

	private static void InitLinuxCreator()
	{
#if CREATOR
		string basePath;

		if (Globals.IsInGDEditor)
		{
			basePath = LuaCompletionService.LuaLSEditorExecutablePath.PathJoin("linux");
		}
		else
		{
			basePath = OS.GetExecutablePath().GetBaseDir();
		}

		string exeName = "luau-lsp";
		string fullPath = ProjectSettings.GlobalizePath(basePath.PathJoin(exeName));

		int ret = chmod(fullPath, 0x755);
		if (ret != 0)
		{
			throw new System.Exception("Linux permission set failure: Code " + ret);
		}
#endif
	}

	private static void InitMacOSCreator()
	{
#if CREATOR
		string basePath;
		if (Globals.IsInGDEditor)
		{
			basePath = LuaCompletionService.LuaLSEditorExecutablePath.PathJoin("macos");
		}
		else
		{
			basePath = OS.GetExecutablePath().GetBaseDir();
		}
		string exeName = "luau-lsp";
		string fullPath = ProjectSettings.GlobalizePath(basePath.PathJoin(exeName));
		int ret = chmod(fullPath, 0x755);
		if (ret != 0)
		{
			throw new System.Exception("macOS permission set failure: Code " + ret);
		}
#endif
	}
}
