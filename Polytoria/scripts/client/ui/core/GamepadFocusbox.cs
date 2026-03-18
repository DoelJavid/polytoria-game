// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;

namespace Polytoria.Client;

public partial class GamepadFocusbox : Control
{
	public float FocusLerpSpeed = 20;
	public Vector2 OutlineOffset = new(16, 16);
	private bool _isGamepadActive = false;
	private bool _gameFocus = false;
	private Vector2 _targetPos = Vector2.Zero;
	private Vector2 _targetSize = Vector2.Zero;

	public override void _Ready()
	{
		Visible = false;
		if (Input.GetConnectedJoypads().Count > 0)
		{
			OnGamepadConnected();
		}

		Input.Singleton.JoyConnectionChanged += OnJoyConnectionChanged;
	}

	private void OnJoyConnectionChanged(long device, bool connected)
	{
		if (device == 0)
		{
			if (connected)
			{
				OnGamepadConnected();
			}
			else
			{
				OnGamepadDisconnected();
			}
		}
	}

	private void OnGamepadConnected()
	{
		_isGamepadActive = true;
		SetProcess(true);
	}

	private void OnGamepadDisconnected()
	{
		_isGamepadActive = false;
		Visible = false;
		SetProcess(false);
	}

	public override void _Process(double delta)
	{
		if (!_isGamepadActive) { return; }

		Control? focusOwner = GetViewport().GuiGetFocusOwner();
		if (focusOwner != null)
		{
			if (focusOwner.Name == "InputFallback")
			{
				focusOwner = null;
			}
		}

		if (focusOwner != null)
		{
			_targetPos = focusOwner.GlobalPosition;
			_targetSize = focusOwner.Size;

			GlobalPosition = GlobalPosition.Lerp(_targetPos - OutlineOffset / 2, (float)(delta * FocusLerpSpeed));
			Size = Size.Lerp(_targetSize + OutlineOffset, (float)(delta * FocusLerpSpeed));

			Visible = true;
		}
		else
		{
			Visible = false;
		}
	}
}
