// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Client;
using Polytoria.DocsGen;
using System.Collections.Generic;
using static Polytoria.Shared.Globals;

namespace Polytoria.Shared;

public partial class AppEntry : Node
{
	public async override void _Ready()
	{
		Dictionary<string, string> cmdargs = ReadCmdArgs();
		bool isApiRefGen = cmdargs.ContainsKey("genapi");
		bool isCreator = cmdargs.ContainsKey("creator");
		bool isLtChild = cmdargs.ContainsKey("ltchild");
		bool isSolo = cmdargs.ContainsKey("solo");

		if (cmdargs.TryGetValue("wait", out string? waitTime))
		{
			await Singleton.WaitAsync(float.Parse(waitTime));
		}

		if (isApiRefGen && IsInGDEditor)
		{
			PT.Print("Generating references...");
			APIReferenceGenerator.GenerateRefFile();
			PT.Print("Completed! Exiting...");
			Globals.Singleton.Quit();
			return;
		}

		AppEntryEnum entry = AppEntryEnum.Client;
		if (OS.HasFeature("client"))
		{
			entry = AppEntryEnum.Client;
		}
		if (OS.HasFeature("creator") || isCreator)
		{
			entry = AppEntryEnum.Creator;
		}
		if (OS.HasFeature("mobile-ui"))
		{
			entry = AppEntryEnum.MobileUI;
		}
		if (OS.HasFeature("renderer"))
		{
			entry = AppEntryEnum.Renderer;
		}

		if (isSolo)
		{
			entry = AppEntryEnum.Client;
		}

		if (isLtChild)
		{
			entry = AppEntryEnum.Client;
		}

		Callable.From(() =>
		{
			Node app = Globals.Singleton.SwitchEntry(entry);
			if (app is ClientEntry ce)
			{
				ce.Entry();
			}
			QueueFree();
		}).CallDeferred();
	}
}
