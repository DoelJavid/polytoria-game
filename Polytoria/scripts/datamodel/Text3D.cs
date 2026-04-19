// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Attributes;
using Polytoria.Datamodel.Resources;
using Polytoria.Enums;
using Polytoria.Shared;

namespace Polytoria.Datamodel;

[Instantiable]
public sealed partial class Text3D : Dynamic
{
	private const float FontSizeConversion = 10.5f;

	private string _text = "";
	private Label3D _label3D = null!;
	private RichTextLabel _richLabel = null!;
	private Label _testLabel = null!;
	private Sprite3D _sprite3D = null!;
	private SubViewport _subViewport = null!;
	private BuiltInFontAsset.BuiltInTextFontPresetEnum _fontPreset = BuiltInFontAsset.BuiltInTextFontPresetEnum.Montserrat;
	private FontAsset? _fontAsset;
	private int _outlineWidth;
	private Color _outlineColor;

	private float _fontSize = 16;
	private bool _useRichText = false;
	private bool _shaded = false;

	private TextHorizontalAlignmentEnum _horizontalAlignment = TextHorizontalAlignmentEnum.Center;
	private TextVerticalAlignmentEnum _verticalAlignment = TextVerticalAlignmentEnum.Middle;

	[Editable, ScriptProperty]
	public string Text
	{
		get => _text;
		set
		{
			_text = value;
			_label3D.Text = _text;
			_richLabel.Text = _text;
			RecomputeSize();
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty]
	public float FontSize
	{
		get => _fontSize;
		set
		{
			_fontSize = value;
			int setto = (int)(value * FontSizeConversion);
			_label3D.FontSize = setto;
			_testLabel.AddThemeFontSizeOverride("font_size", setto);
			_richLabel.AddThemeFontSizeOverride("normal_font_size", setto);
			_richLabel.AddThemeFontSizeOverride("bold_font_size", setto);
			_richLabel.AddThemeFontSizeOverride("bold_italics_font_size", setto);
			_richLabel.AddThemeFontSizeOverride("italics_font_size", setto);
			_richLabel.AddThemeFontSizeOverride("mono_font_size", setto);
			RecomputeSize();
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty]
	public Color Color
	{
		get => _label3D.Modulate;
		set
		{
			_richLabel.Modulate = value;
			_label3D.Modulate = value;
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty]
	public int OutlineWidth
	{
		get => _outlineWidth;
		set
		{
			_outlineWidth = value;
			int setto = (int)(_outlineWidth * FontSizeConversion);
			_label3D.OutlineSize = setto;
			_richLabel.AddThemeConstantOverride("outline_size", setto);
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty]
	public Color OutlineColor
	{
		get => _outlineColor;
		set
		{
			_outlineColor = value;
			_label3D.OutlineModulate = value;
			_richLabel.AddThemeColorOverride("font_outline_color", value);
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty]
	public bool FaceCamera
	{
		get => _sprite3D.Billboard == BaseMaterial3D.BillboardModeEnum.Enabled;
		set
		{
			_label3D.Billboard = value ? BaseMaterial3D.BillboardModeEnum.Enabled : BaseMaterial3D.BillboardModeEnum.Disabled;
			_sprite3D.Billboard = _label3D.Billboard;
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty]
	public TextHorizontalAlignmentEnum HorizontalAlignment
	{
		get => _horizontalAlignment;
		set
		{
			_horizontalAlignment = value;

			switch (_horizontalAlignment)
			{
				case TextHorizontalAlignmentEnum.Left:
					_label3D.HorizontalAlignment = Godot.HorizontalAlignment.Left;
					break;
				case TextHorizontalAlignmentEnum.Center:
					_label3D.HorizontalAlignment = Godot.HorizontalAlignment.Center;
					break;
				case TextHorizontalAlignmentEnum.Right:
					_label3D.HorizontalAlignment = Godot.HorizontalAlignment.Right;
					break;
			}
			;

			RecomputeSize();
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty]
	public TextVerticalAlignmentEnum VerticalAlignment
	{
		get => _verticalAlignment;
		set
		{
			_verticalAlignment = value;

			switch (_verticalAlignment)
			{
				case TextVerticalAlignmentEnum.Top:
					_label3D.VerticalAlignment = Godot.VerticalAlignment.Top;
					break;
				case TextVerticalAlignmentEnum.Middle:
					_label3D.VerticalAlignment = Godot.VerticalAlignment.Center;
					break;
				case TextVerticalAlignmentEnum.Bottom:
					_label3D.VerticalAlignment = Godot.VerticalAlignment.Bottom;
					break;
			}
			;

			RecomputeSize();
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty]
	public FontAsset? FontAsset
	{
		get => _fontAsset;
		set
		{
			if (_fontAsset != null && _fontAsset != value)
			{
				_fontAsset.ResourceLoaded -= OnFontLoaded;
				_fontAsset.UnlinkFrom(this);
			}
			_fontAsset = value;
			SetFontTo(null);

			if (_fontAsset != null)
			{
				_fontAsset.LinkTo(this);
				_fontAsset.ResourceLoaded += OnFontLoaded;
				_fontAsset.QueueLoadResource();
			}
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty, NoSync, Attributes.Obsolete("Use FontAsset instead"), CloneIgnore, SaveIgnore]
	public BuiltInFontAsset.BuiltInTextFontPresetEnum Font
	{
		get => _fontPreset;
		set
		{
			_fontPreset = value;
			FontAsset = new BuiltInFontAsset()
			{
				FontPreset = _fontPreset
			};
		}
	}

	/// <summary>
	/// NOTE: This is experimental
	/// </summary>
	[Editable, ScriptProperty]
	public bool UseRichText
	{
		get => _useRichText;
		set
		{
			_useRichText = value;

			_label3D.Visible = !value;
			_sprite3D.Visible = value;
		}
	}

	[Editable, ScriptProperty, DefaultValue(false)]
	public bool Shaded
	{
		get => _shaded;
		set
		{
			_shaded = value;

			_label3D.Shaded = value;
			_sprite3D.Shaded = value;
		}
	}

	private void OnFontLoaded(Resource resource)
	{
		SetFontTo((Font)resource);
	}

	private void SetFontTo(Font? f)
	{
		_label3D.Font = f;
		if (f != null)
		{
			_testLabel.AddThemeFontOverride("tfont", f);
			_richLabel.AddThemeFontOverride("normal_font", f);
			_richLabel.AddThemeFontOverride("mono_font", f);
		}
		RecomputeSize();
	}

	private void RecomputeSize()
	{
		_testLabel.Text = _text;
		Callable.From(() =>
		{
			if (!Node.IsInstanceValid(_richLabel)) return;
			Vector2 offset = Vector2.Zero;
			Vector2I size = (Vector2I)_richLabel.Size;
			_subViewport.Size = size;
			switch (_horizontalAlignment)
			{
				case TextHorizontalAlignmentEnum.Left:
					offset.X = 0;
					break;
				case TextHorizontalAlignmentEnum.Center:
					offset.X = -(size.X / 2);
					break;
				case TextHorizontalAlignmentEnum.Right:
					offset.X = -size.X;
					break;
			}
			;

			switch (_verticalAlignment)
			{
				case TextVerticalAlignmentEnum.Top:
					offset.Y = 0;
					break;
				case TextVerticalAlignmentEnum.Middle:
					offset.Y = -(size.Y / 2);
					break;
				case TextVerticalAlignmentEnum.Bottom:
					offset.Y = -size.Y;
					break;
			}
			;

			_sprite3D.Offset = offset;

			_subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
		}).CallDeferred();
	}

	public override Node CreateGDNode()
	{
		return Globals.LoadNetworkedObjectScene(ClassName)!;
	}

	public override void Init()
	{
		_label3D = GDNode.GetNode<Label3D>("Label3D");
		_sprite3D = GDNode.GetNode<Sprite3D>("Sprite3D");
		_subViewport = GDNode.GetNode<SubViewport>("SubViewport");

		_sprite3D.Texture = _subViewport.GetTexture();

		_richLabel = _subViewport.GetNode<RichTextLabel>("RichTextLabel");
		_testLabel = _subViewport.GetNode<Label>("Label");

		_subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;

		Text = "Text";
		HorizontalAlignment = TextHorizontalAlignmentEnum.Center;
		VerticalAlignment = TextVerticalAlignmentEnum.Middle;
		FontSize = 16;
		Color = new(1, 1, 1);
		OutlineWidth = 0;
		OutlineColor = new(0, 0, 0);
		UseRichText = false;

		base.Init();
	}

	internal override void OnNodeSizeChanged(Vector3 newSize)
	{
		_label3D.Scale = newSize;
		_sprite3D.Scale = newSize;
		base.OnNodeSizeChanged(newSize);
	}
}
