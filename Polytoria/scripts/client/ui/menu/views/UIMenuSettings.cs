// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using System;
using System.Collections.Generic;

namespace Polytoria.Client.UI;

public sealed partial class UIMenuSettings : UIMenuViewBase
{
	private UISettingsView? _currentView = null;
	public event Action<SettingsViewEnum>? ViewChanged;
	private readonly List<UISettingsCategoryButton> _categoryButtons = [];

	[Export] private Control _viewContainer = null!;

	public static UIMenuSettings Singleton { get; private set; } = null!;

	public UIMenuSettings()
	{
		Singleton = this;
	}

	public void RegisterCategoryButton(UISettingsCategoryButton btn)
	{
		_categoryButtons.Add(btn);

		if (_currentView != null && _currentView.FirstFocus != null)
		{
			btn.FocusNeighborRight = btn.GetPathTo(_currentView.FirstFocus);
		}
	}

	public override void ShowView()
	{
		SwitchView(SettingsViewEnum.General);
		base.ShowView();
	}

	public override void HideView()
	{
		ClientSettings.Singleton.SaveSettings();
		base.HideView();
	}

	public void SwitchView(SettingsViewEnum switchTo)
	{
		_currentView?.QueueFree();
		_currentView = null;

		string pathToLoad = switchTo switch
		{
			SettingsViewEnum.General => "res://scenes/client/ui/menu/components/settings/views/general.tscn",
			SettingsViewEnum.Graphics => "res://scenes/client/ui/menu/components/settings/views/graphics.tscn",
			SettingsViewEnum.Advanced => "res://scenes/client/ui/menu/components/settings/views/advanced.tscn",
			_ => throw new ArgumentOutOfRangeException(nameof(switchTo), $"No scene defined for {switchTo}")
		};

		PackedScene scene = GD.Load<PackedScene>(pathToLoad);
		if (scene != null)
		{
			_currentView = scene.Instantiate<UISettingsView>();

			_viewContainer.AddChild(_currentView);
			ViewChanged?.Invoke(switchTo);

			// Update first focus
			if (_currentView.FirstFocus != null)
			{
				foreach (UISettingsCategoryButton btn in _categoryButtons)
				{
					btn.FocusNeighborRight = btn.GetPathTo(_currentView.FirstFocus);
				}
			}
			else
			{
				GD.PushWarning(switchTo, " doesn't have first focus, this might break gamepad functionality");
			}
		}
	}
}

public enum SettingsViewEnum
{
	General,
	Graphics,
	Admin,
	Advanced,
	Beta
}
