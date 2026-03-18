// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Attributes;

namespace Polytoria.Datamodel;

[Instantiable]
public sealed partial class SpotLight : Light
{
	private float _range = 30;
	private float _angle = 30;

	public override void Init()
	{
		LightNode = GDNode.GetNode<SpotLight3D>("SpotLight3D");

		base.Init();
	}

	[Editable, ScriptProperty]
	public float Range
	{
		get => _range;
		set
		{
			_range = value;
			((SpotLight3D)LightNode).SpotRange = value;
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty]
	public float Angle
	{
		get => _angle;
		set
		{
			_angle = value;
			((SpotLight3D)LightNode).SpotAngle = value;
			OnPropertyChanged();
		}
	}
}
