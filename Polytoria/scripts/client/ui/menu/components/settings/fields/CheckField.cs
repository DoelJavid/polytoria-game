// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;

namespace Polytoria.Client.UI;

public partial class CheckField : CheckButton, ISettingField
{
	[Export] public string SettingName { get; set; } = "";

	private void Refresh()
	{
		SetPressedNoSignal(ClientSettings.Singleton.GetSetting<bool>(SettingName));
	}

	public override void _EnterTree()
	{
		Refresh();
		ClientSettings.Singleton.OnSettingChanged += OnSettingChanged;

		Toggled += (bool value) =>
		{
			ClientSettings.Singleton.SetSetting(SettingName, value);
		};
	}

	private void OnSettingChanged(string what)
	{
		if (what == SettingName)
		{
			Refresh();
		}
	}

	public override void _ExitTree()
	{
		ClientSettings.Singleton.OnSettingChanged -= OnSettingChanged;
		base._ExitTree();
	}
}
