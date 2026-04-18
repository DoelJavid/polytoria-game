using Godot;
using Polytoria.Shared;
using Polytoria.Shared.Settings;

namespace Polytoria.Client.Settings.Appliers;

public sealed partial class AudioSettingsApplier : Node
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
			case ClientSettingKeys.General.MasterVolume:
				ApplyVolume();
				break;
		}
	}

	private void ApplyAll()
	{
		ApplyVolume();
	}

	private void ApplyVolume()
	{
		float volume = ClientSettingsService.Instance.Get<float>(ClientSettingKeys.General.MasterVolume);
		AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb(volume / 100f));
	}
}