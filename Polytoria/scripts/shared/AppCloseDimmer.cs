// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;

namespace Polytoria.Shared;

public partial class AppCloseDimmer : Node
{
	public override void _Ready()
	{
		Globals.BeforeQuit += () =>
		{
			CanvasLayer layer = new()
			{
				Layer = 10000
			};
			ColorRect c = new()
			{
				Color = new(0, 0, 0, 0.5f),
				ZIndex = 1000,
				TopLevel = true
			};
			c.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			layer.AddChild(c);
			GetParent().AddChild(layer);
		};
	}
}
