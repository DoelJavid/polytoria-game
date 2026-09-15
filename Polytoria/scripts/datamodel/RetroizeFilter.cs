// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Attributes;

namespace Polytoria.Datamodel;

[Instantiable]
public partial class RetroizeFilter : BaseFilter
{
	internal override Shader _filterShader
	{
		get => GD.Load<Shader>("res://resources/shaders/filters/retroize.gdshader");
	}

	private int _pixelSize;
	private int _ditherStrength;
	private int _brightnessLevels;
	private Vector4I _colorDepth;

	[Editable, ScriptProperty, DefaultValue(3)]
	public int PixelSize
	{
		get => _pixelSize;
		set
		{
			_pixelSize = value;
			UpdateFilter();
		}
	}

	[Editable, ScriptProperty, DefaultValue(2)]
	public int DitherStrength
	{
		get => _ditherStrength;
		set
		{
			_ditherStrength = value;
			UpdateFilter();
		}
	}

	[Editable, ScriptProperty, DefaultValue(4)]
	public int RedBitDepth
	{
		get => _colorDepth.X;
		set
		{
			_colorDepth.X = value;
			UpdateFilter();
		}
	}

	[Editable, ScriptProperty, DefaultValue(4)]
	public int GreenBitDepth
	{
		get => _colorDepth.Y;
		set
		{
			_colorDepth.Y = value;
			UpdateFilter();
		}
	}

	[Editable, ScriptProperty, DefaultValue(4)]
	public int BlueBitDepth
	{
		get => _colorDepth.Z;
		set
		{
			_colorDepth.Z = value;
			UpdateFilter();
		}
	}

	[Editable, ScriptProperty, DefaultValue(4)]
	public int AlphaBitDepth
	{
		get => _colorDepth.W;
		set
		{
			_colorDepth.W = value;
			UpdateFilter();
		}
	}

	protected override void UpdateFilter()
	{
		_shaderMaterial.SetShaderParameter("pixel_size", _pixelSize);
		_shaderMaterial.SetShaderParameter("dither_strength", _ditherStrength);
		_shaderMaterial.SetShaderParameter("color_depth", _colorDepth);
		base.UpdateFilter();
	}
}
