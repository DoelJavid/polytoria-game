using Godot;
using Polytoria.Shared;
using Polytoria.Shared.Settings;

namespace Polytoria.Client.Settings.Appliers;

public sealed partial class DisplaySettingsApplier : Node
{
	public override void _Ready()
	{
		ClientSettingsService.Instance.Changed += OnChanged;
		ApplyAll();
	}

	private void OnChanged(SettingChangedEvent change)
	{
		switch (change.Key)
		{
			case ClientSettingKeys.Display.Fullscreen:
				ApplyFullscreen();
				break;
			case ClientSettingKeys.Display.VSync:
				ApplyVsync();
				break;
			case ClientSettingKeys.Display.UiScale:
				ApplyUiScale();
				break;
		}
	}

	private void ApplyAll()
	{
		ApplyFullscreen();
		ApplyVsync();
		ApplyUiScale();
	}

	private void ApplyFullscreen()
	{
		bool fullscreen = ClientSettingsService.Instance.Get<bool>(ClientSettingKeys.Display.Fullscreen);
		DisplayServer.WindowSetMode(fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
	}

	private void ApplyVsync()
	{
		bool vsync = ClientSettingsService.Instance.Get<bool>(ClientSettingKeys.Display.VSync);
		DisplayServer.WindowSetVsyncMode(vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
	}

	private void ApplyUiScale()
	{
		float scale = ClientSettingsService.Instance.Get<float>(ClientSettingKeys.Display.UiScale);
		float finalScale;
		int screenId = DisplayServer.WindowGetCurrentScreen();
		float osScale = DisplayServer.ScreenGetScale(screenId);
		finalScale = scale * osScale;
		GetTree().Root.ContentScaleFactor = finalScale;
	}
}
