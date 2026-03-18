// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;

namespace Polytoria.Client.UI;

public partial class SoundSliderField : SliderField, ISettingField
{
	public override void _Ready()
	{
		base._Ready();
		ValueChanged += (double value) =>
		{
			AudioStreamPlayer player = new()
			{
				Stream = GD.Load<AudioStream>("res://assets/audio/built-in/jump.ogg")
			};
			AddChild(player);
			player.Play();
			player.Finished += player.QueueFree;
		};
	}
}
