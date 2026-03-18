// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;

namespace Polytoria.Client.UI;

public partial class UIScaleField : MenuButton, ISettingField
{
	[Export] public string SettingName { get; set; } = "";
	private PopupMenu _popup = null!;

	private void Refresh()
	{
		int setto = ((float)ClientSettings.Singleton.GetSetting(SettingName)!) switch
		{
			0.25f => 0,
			0.5f => 1,
			0.75f => 2,
			1f => 3,
			1.25f => 4,
			1.5f => 5,
			1.75f => 6,
			2f => 7,
			_ => 0
		};
		Text = _popup.GetItemText(_popup.GetItemIndex(setto));
	}

	public override void _Ready()
	{
		_popup = GetPopup();
		_popup.AddItem("x0.25", 0);
		_popup.AddItem("x0.5", 1);
		_popup.AddItem("x0.75", 2);
		_popup.AddItem("x1", 3);
		_popup.AddItem("x1.25", 4);
		_popup.AddItem("x1.5", 5);
		_popup.AddItem("x1.75", 6);
		_popup.AddItem("x2", 7);

		Refresh();

		_popup.IdPressed += id =>
		{
			float setto = id switch
			{
				0 => 0.25f,
				1 => 0.5f,
				2 => 0.75f,
				3 => 1f,
				4 => 1.25f,
				5 => 1.5f,
				6 => 1.75f,
				7 => 2f,
				_ => 1f
			};
			ClientSettings.Singleton.SetSetting(SettingName, setto);
			Refresh();
		};
	}
}
