// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Datamodel;

namespace Polytoria.Client.UI;

public partial class UIToolItem : Button
{
	public UIInventory Root = null!;
	public Tool LinkedTool = null!;
	public Player Player = null!;

	public bool IsInBackpack
	{
		get => _isInBackpack;
		set
		{
			_isInBackpack = value;
			UpdateLabel();
		}
	}

	[Export]
	public int ToolIndex
	{
		get => _toolIndex;
		set
		{
			_toolIndex = value;
			UpdateLabel();
		}
	}

	private int _toolIndex = 0;
	private bool _isInBackpack = false;

	private Label _toolNameLabel = null!;
	private TextureRect _toolIconRect = null!;
	private Label _toolIndexLabel = null!;
	private TouchScreenButton _touchscreenButton = null!;
	private Control _touchscreenBlock = null!;
	private Control _baseControl = null!;
	private bool _initialized = false;

	public override void _EnterTree()
	{
		if (!_initialized)
		{
			Init();
		}
	}

	public override void _ExitTree()
	{
		if (_initialized)
		{
			DeInit();
		}
		base._ExitTree();
	}

	private void Init()
	{
		_initialized = true;
		_baseControl = GetNode<Control>("Base");
		_toolNameLabel = _baseControl.GetNode<Label>("ToolNameLabel");
		_toolIconRect = _baseControl.GetNode<TextureRect>("Icon");
		_toolIndexLabel = _baseControl.GetNode<Label>("Index");
		_touchscreenBlock = _baseControl.GetNode<Control>("TouchscreenBlock");
		_touchscreenButton = _touchscreenBlock.GetNode<TouchScreenButton>("TSB");
		UpdateLabel();
		UpdateName();
		LinkedTool.PropertyChanged.Connect(OnToolPropChanged);
		LinkedTool.Equipped.Connect(OnToolEquipped);
		LinkedTool.Unequipped.Connect(OnToolUnequipped);
		Toggled += OnToggled;

		if (LinkedTool.Root.Input.IsTouchscreen)
		{
			_touchscreenBlock.Visible = true;
			_touchscreenButton.Pressed += OnPressed;
		}
		else
		{
			_touchscreenBlock.QueueFree();
		}

		if (LinkedTool.ToolImgTexture != null)
		{
			InsertToolImage();
		}
		LinkedTool.ToolImgTextureLoaded += InsertToolImage;
	}

	private void InsertToolImage()
	{
		Texture2D? img = LinkedTool.ToolImgTexture;
		_toolIconRect.Texture = LinkedTool.ToolImgTexture;
		_toolNameLabel.Visible = img == null;
	}

	private void DeInit()
	{
		_initialized = false;
		LinkedTool.ToolImgTextureLoaded -= InsertToolImage;
		Toggled -= OnToggled;
		LinkedTool.PropertyChanged.Disconnect(OnToolPropChanged);
		LinkedTool.Equipped.Disconnect(OnToolEquipped);
		LinkedTool.Unequipped.Disconnect(OnToolUnequipped);
	}

	private void UpdateLabel()
	{
		if (_toolIndexLabel == null)
		{
			return;
		}
		_toolIndexLabel.Visible = !IsInBackpack;
		_toolIndexLabel.Text = (ToolIndex + 1).ToString();
	}

	private void OnPressed()
	{
		ButtonPressed = !ButtonPressed;
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		Root.StartDragFrom(this);
		return "tool:" + LinkedTool.NetworkedObjectID;
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (data.VariantType == Variant.Type.String)
		{
			string str = data.AsString();

			if (str.StartsWith("tool:"))
			{
				return true;
			}
		}
		return false;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (data.VariantType == Variant.Type.String)
		{
			string str = data.AsString();

			if (str.StartsWith("tool:"))
			{
				string netId = str.Replace("tool:", "");
				Tool? tool = Root.GetToolFromNetworkID(netId);

				if (tool != null)
				{
					Root.MoveToolSlot(Root.GetToolItemFromTool(tool)!, this);
				}
			}
		}
	}

	private void OnToolPropChanged(string propName)
	{
		if (propName == "Name")
		{
			UpdateName();
		}
	}

	private void UpdateName()
	{
		_toolNameLabel.Text = LinkedTool.Name;
	}

	private void OnToolEquipped()
	{
		SetPressedNoSignal(true);
	}

	private void OnToolUnequipped()
	{
		SetPressedNoSignal(false);
	}

	private void OnToggled(bool to)
	{
		if (to)
		{
			Player.EquipTool(LinkedTool);
		}
		else
		{
			Player.UnequipTool();
		}
	}
}
