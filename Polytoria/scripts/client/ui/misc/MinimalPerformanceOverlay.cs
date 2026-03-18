// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;

namespace Polytoria.Client.UI;

public partial class MinimalPerformanceOverlay : Control
{
	private Label _fpsLabel = null!;
	private Label _pingLabel = null!;


	public override void _Ready()
	{
		_fpsLabel = GetNode<Label>("FPS/Layout/Label");
		_pingLabel = GetNode<Label>("Ping/Layout/Label");
		Visible = false;
		ClientSettings.Singleton.OnSettingChanged += OnSettingChanged;
		UpdateVisible();
		MainUpdateLoop();
	}

	private void OnSettingChanged(string name)
	{
		if (name == "PerformanceOverlayMode")
		{
			UpdateVisible();
		}
	}

	private void UpdateVisible()
	{
		Visible = ClientSettings.Singleton.Settings.PerformanceOverlayMode >= ClientSettingsData.PerformanceOverlayModeEnum.Minimal;
	}

	private void UpdateAll()
	{
		_fpsLabel.Text = Engine.GetFramesPerSecond().ToString();
		_pingLabel.Text = CoreUIRoot.Singleton.Root.Players.LocalPlayer.NetworkPing.ToString();
	}

	private async void MainUpdateLoop()
	{
		UpdateAll();
		await ToSignal(GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout);
		MainUpdateLoop();
	}
}
