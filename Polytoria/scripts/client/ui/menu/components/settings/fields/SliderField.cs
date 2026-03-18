// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;

namespace Polytoria.Client.UI;

public partial class SliderField : HSlider, ISettingField
{
	[Export] public string SettingName { get; set; } = "";

	private void Refresh()
	{
		SetValueNoSignal(ClientSettings.Singleton.GetSetting<double>(SettingName));
	}

	public override void _Ready()
	{
		Refresh();
		ValueChanged += (double value) =>
		{
			ClientSettings.Singleton.SetSetting(SettingName, value);
		};
	}
}
