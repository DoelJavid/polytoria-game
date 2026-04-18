namespace Polytoria.Shared.Settings;

public interface ISettingsContext
{
	T Get<T>(string key);
	object? GetUntyped(string key);
}
