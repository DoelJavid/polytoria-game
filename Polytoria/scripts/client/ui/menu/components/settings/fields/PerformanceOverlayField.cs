// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;

namespace Polytoria.Client.UI;

public partial class PerformanceOverlayField : MenuButton, ISettingField
{
	[Export] public string SettingName { get; set; } = "";
	private PopupMenu _popup = null!;

	private void Refresh()
	{
		int setto = ClientSettings.Singleton.GetSetting<int>(SettingName);
		Text = _popup.GetItemText(_popup.GetItemIndex(setto));
	}

	public override void _Ready()
	{
		_popup = GetPopup();
		_popup.AddItem("None", 0);
		_popup.AddItem("Minimal", 1);
		_popup.AddItem("Full", 2);

		Refresh();

		_popup.IdPressed += id =>
		{
			ClientSettings.Singleton.SetSetting(SettingName, (ClientSettingsData.PerformanceOverlayModeEnum)id);
			Refresh();
		};
	}
}
