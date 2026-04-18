using System;
using System.Collections.Generic;
using System.Linq;

namespace Polytoria.Shared.Settings;

public abstract class SettingDef
{
	public required string Key { get; init; }
	public required string SectionKey { get; init; }
	public required string Label { get; init; }
	public string Description { get; init; } = string.Empty;

	public required SettingValueKind ValueKind { get; init; }
	public required SettingControlKind ControlKind { get; init; }

	public bool RequiresRestart { get; init; }
	public bool IsAdvanced { get; init; }

	public Func<ISettingsContext, bool>? VisibleWhen { get; init; }
	public Func<ISettingsContext, bool>? EnabledWhen { get; init; }

	public abstract object UntypedDefault { get; }
	public virtual object? UntypedMinValue => null;
	public virtual object? UntypedMaxValue => null;
	public virtual object? UntypedStep => null;
	public virtual IReadOnlyList<ISettingOption>? UntypedOptions => null;
	public abstract object ConvertToType(object? value);
}

public class SettingDef<T> : SettingDef
{
	public required T DefaultValue { get; init; }
	public T? MinValue { get; init; }
	public T? MaxValue { get; init; }
	public T? Step { get; init; }
	public IReadOnlyList<SettingOption<T>>? Options { get; init; }

	public override object UntypedDefault => DefaultValue!;
	public override object? UntypedMinValue => MinValue;
	public override object? UntypedMaxValue => MaxValue;
	public override object? UntypedStep => Step;
	public override IReadOnlyList<ISettingOption>? UntypedOptions => Options?.Cast<ISettingOption>().ToArray();

	public override object ConvertToType(object? value)
	{
		if (value == null)
		{
			return DefaultValue!;
		}

		if (value is T typed)
		{
			return typed;
		}

		if (typeof(T).IsEnum)
		{
			if (value is string stringValue)
			{
				return Enum.Parse(typeof(T), stringValue, true);
			}

			return Enum.ToObject(typeof(T), value);
		}

		return (T)Convert.ChangeType(value, typeof(T));
	}
}
