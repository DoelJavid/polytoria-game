namespace Polytoria.Shared.Settings;

public interface ISettingOption
{
	object? UntypedValue { get; }
	string Label { get; }
	string Description { get; }
}

public class SettingOption<T> : ISettingOption
{
	public required T Value { get; init; }
	public required string Label { get; init; }
	public string Description { get; init; } = string.Empty;
	public object? UntypedValue => Value;
}
