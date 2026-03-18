// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Creator.Properties;
using Polytoria.Shared;
using static Polytoria.Creator.CreatorSettings;

namespace Polytoria.Creator.UI.Components;

public partial class SettingsPropertyUI : Node
{
	[Export] private Label _propNameLabel = null!;
	[Export] private Control _propContainer = null!;

	public SettingsProperty Property = null!;

	public override void _Ready()
	{
		_propNameLabel.Text = Property.DisplayName;

		IProperty input = Globals.LoadProperty(Property.ValueType);

		input.PropertyType = Property.ValueType;
		_propContainer.AddChild((Node)input);

		// Apply min/max value for floats if set
		if (input is SingleProperty sp && Property.MaxValue != 0)
		{
			sp.MinValue = Property.MinValue;
			sp.MaxValue = Property.MaxValue;
			sp.AllowGreater = false;
			sp.AllowLesser = false;
		}

		((Control)input).SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		// Wait one frame for property to be ready
		Callable.From(() =>
		{
			input.SetValue(Property.Value);

			input.ValueChanged += val =>
			{
				Property.Value = val;
			};
		}).CallDeferred();
	}
}
