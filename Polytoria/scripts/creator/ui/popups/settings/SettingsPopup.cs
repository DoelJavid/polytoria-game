// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Creator.UI.Components;
using Polytoria.Shared;
using System.Collections.Generic;
using static Polytoria.Creator.CreatorSettings;

namespace Polytoria.Creator.UI.Popups;

public sealed partial class SettingsPopup : PopupWindowBase
{
	private const string SettingsPropertyPath = "res://scenes/creator/popups/settings/components/settings_property.tscn";
	[Export] private Tree _categoryTree = null!;
	[Export] private Control _layout = null!;

	private readonly Dictionary<TreeItem, SettingsCategory> _itemToCat = [];

	public override void _Ready()
	{
		TreeItem root = _categoryTree.CreateItem();
		bool isFirst = true;
		TreeItem? firstSelected = null;
		foreach (SettingsCategory cat in Singleton.Root.Categories)
		{
			TreeItem ch = root.CreateChild();
			ch.SetText(0, cat.DisplayName);
			_itemToCat[ch] = cat;
			if (isFirst)
			{
				firstSelected = ch;
				isFirst = false;
			}
		}

		_categoryTree.ItemSelected += OnItemSelected;

		firstSelected?.Select(0);
		base._Ready();
	}

	private void OnItemSelected()
	{
		ClearSettings();
		SettingsCategory cat = _itemToCat[_categoryTree.GetSelected()];

		foreach (SettingsProperty item in cat.Settings)
		{
			SettingsPropertyUI ui = Globals.CreateInstanceFromScene<SettingsPropertyUI>(SettingsPropertyPath);
			ui.Property = item;
			_layout.AddChild(ui);
		}
	}

	private void ClearSettings()
	{
		foreach (Node item in _layout.GetChildren())
		{
			item.QueueFree();
		}
	}
}
