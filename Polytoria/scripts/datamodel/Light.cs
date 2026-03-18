// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Attributes;
#if CREATOR
using Polytoria.Creator.Spatial;
#endif
using Polytoria.Shared;

namespace Polytoria.Datamodel;

[Abstract]
public partial class Light : Dynamic
{
	const float IntensityConversion = 4f;
	internal Light3D LightNode = null!;
	private Color _color = new(1, 1, 1);
	private float _brightness = 2;
	private float _specular = 0.5f;
	private bool _shadows = false;

	[Editable, ScriptProperty]
	public Color Color
	{
		get => _color;
		set
		{
			_color = value;
			LightNode.LightColor = value;
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty]
	public float Brightness
	{
		get => _brightness;
		set
		{
			_brightness = value;
			LightNode.LightEnergy = value / IntensityConversion;
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty]
	public float Specular
	{
		get => _specular;
		set
		{
			_specular = value;
			LightNode.LightSpecular = value;
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty]
	public bool Shadows
	{
		get => _shadows;
		set
		{
			_shadows = value;
			LightNode.ShadowEnabled = value;
			OnPropertyChanged();
		}
	}

	public override Node CreateGDNode()
	{
		return Globals.LoadNetworkedObjectScene(ClassName)!;
	}

	public override void Init()
	{
#if CREATOR
		GDNode.AddChild(new SpatialIcon(ClassName));
#endif
		base.Init();
	}
}
