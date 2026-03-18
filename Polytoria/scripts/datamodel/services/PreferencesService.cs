// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Polytoria.Attributes;
using Polytoria.Client;
using Polytoria.Scripting;

namespace Polytoria.Datamodel.Services;

[Static("Preferences")]
[ExplorerExclude]
[SaveIgnore]
public sealed partial class PreferencesService : Instance
{
	[ScriptProperty] public PTSignal<string, object> SettingChanged { get; private set; } = new();
	[ScriptProperty] public static bool UsePhotoMode => ClientSettings.Singleton.Settings.PhotoMode;
	[ScriptProperty] public static bool UsePostProcessing => ClientSettings.Singleton.Settings.PostProcessing;


	public override void Init()
	{
		ClientSettings.Singleton.OnSettingChanged += OnSettingChanged;
		base.Init();
	}

	public override void PreDelete()
	{
		ClientSettings.Singleton.OnSettingChanged -= OnSettingChanged;
		base.PreDelete();
	}

	private void OnSettingChanged(string obj)
	{
		if (obj == "PhotoMode")
		{
			SettingChanged.Invoke("UsePhotoMode", ClientSettings.Singleton.Settings.PhotoMode);
		}
		else if (obj == "PostProcessing")
		{
			SettingChanged.Invoke("UsePostProcessing", ClientSettings.Singleton.Settings.PostProcessing);
		}
	}
}
