// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Shared;

namespace Polytoria.Client.UI;

public partial class UIRenderingMethodField : MenuButton, ISettingField
{
	[Export] public string SettingName { get; set; } = "";
	private PopupMenu _popup = null!;

	private void Refresh()
	{
		int setto = ClientSettings.Singleton.GetSetting<RenderingDeviceSwitcher.RenderingDeviceEnum>(SettingName) switch
		{
			RenderingDeviceSwitcher.RenderingDeviceEnum.Forward => 0,
			RenderingDeviceSwitcher.RenderingDeviceEnum.Mobile => 1,
			RenderingDeviceSwitcher.RenderingDeviceEnum.GLCompatibility => 2,
			_ => 0
		};
		Text = _popup.GetItemText(_popup.GetItemIndex(setto));
	}

	public override void _Ready()
	{
		_popup = GetPopup();
		_popup.AddItem("High", 0);
		_popup.AddItem("Medium", 1);
		_popup.AddItem("Low (OpenGL)", 2);

		Refresh();

		_popup.IdPressed += id =>
		{
			RenderingDeviceSwitcher.RenderingDeviceEnum setto = id switch
			{
				0 => RenderingDeviceSwitcher.RenderingDeviceEnum.Forward,
				1 => RenderingDeviceSwitcher.RenderingDeviceEnum.Mobile,
				2 => RenderingDeviceSwitcher.RenderingDeviceEnum.GLCompatibility,
				_ => RenderingDeviceSwitcher.RenderingDeviceEnum.Mobile
			};
			ClientSettings.Singleton.SetSetting(SettingName, setto);
			Refresh();
		};
	}
}
