namespace Polytoria.Shared.Settings;

public readonly record struct SettingChangedEvent(
	string Key,
	object? OldValue,
	object? NewValue,
	bool RequiresRestart
);
