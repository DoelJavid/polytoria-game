// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Polytoria.Shared;

public partial class PTCompositor : Control
{
	private Texture2D _renderResult = null!;
	private PTCompositorLayer? _rootLayer = null;
	private List<Material> _materials = new();

	public SubViewport RootViewport { get; private set; } = null!;

	public PTCompositor()
	{
		RootViewport = new();
		_renderResult = RootViewport.GetTexture();
	}
	public PTCompositor(SubViewport customRoot)
	{
		RootViewport = customRoot;
		_renderResult = RootViewport.GetTexture();
	}

	public override void _EnterTree()
	{
		RecomputeViews();
	}

	public override void _Process(double delta)
	{
		PTCompositorLayer layer = _rootLayer;
		Vector2I compositorSize = new((int)Size.X, (int)Size.Y);
		RootViewport.Size = compositorSize;
		while (layer != null)
		{
			layer.Size = compositorSize;
			layer = layer.NextLayer;
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		DrawTextureRect(
			_renderResult,
			new(Position.X, Position.Y, Position.X + Size.X, Position.Y + Size.Y),
			false
		);
	}

	// TODO: This is naive. Make a better method of doing this.
	// Ideally, something that doesn't clear all viewports.
	private void RecomputeViews()
	{
		if (_rootLayer != null)
		{
			PTCompositorLayer layer = _rootLayer;
			while (layer != null)
			{
				layer.QueueFree();
				layer = layer.NextLayer;
			}
			_rootLayer = null;
		}

		PTCompositorLayer? lastLayer = null;
		foreach (Material mat in _materials)
		{
			PTCompositorLayer layer = new(mat);
			layer.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

			if (lastLayer == null)
			{
				GD.Print("Set root");
				_rootLayer = layer;
				layer.RenderTexture = RootViewport.GetTexture();
			}
			else
			{
				layer.LastLayer = lastLayer;
			}

			AddChild(layer, true, Node.InternalMode.Back);
			lastLayer = layer;
		}

		_renderResult = lastLayer?.GetTexture() ?? _rootLayer.GetTexture();
	}

	public void AddRenderPass(Material mat, int idx = 0)
	{
		if (idx <= 0)
		{
			_materials.Add(mat);
			RecomputeViews();
		}
		else
		{
			_materials.Insert(idx, mat);
			RecomputeViews();
		}
	}

	public void SetRenderPasses(Material[] mats)
	{
		ClearRenderPasses();
		_materials = new(mats);
		RecomputeViews();
	}

	public void RemoveRenderPass(Material mat)
	{
		_materials.Remove(mat);
		RecomputeViews();
	}

	public void ClearRenderPasses()
	{
		_materials = new();
		RecomputeViews();
	}
}

public partial class PTCompositorLayer : SubViewport
{
	private PTCompositorLayer? _lastLayer = null;
	private PTCompositorLayer? _nextLayer = null;
	private TextureRect _compositorRect = null!;

	internal Texture2D RenderTexture
	{
		get => _compositorRect.Texture;
		set => _compositorRect.Texture = value;
	}

	public Material Effect
	{
		get => _compositorRect.Material;
		set => _compositorRect.Material = value;
	}

	public PTCompositorLayer? NextLayer
	{
		get => _nextLayer;
		set
		{
			if (_nextLayer == value) return;
			if (_nextLayer != null) _nextLayer.LastLayer = null;

			_nextLayer = value;

			if (value != null && value.LastLayer != this) _nextLayer.LastLayer = this;
		}
	}

	public PTCompositorLayer? LastLayer
	{
		get => _lastLayer;
		set
		{
			if (_lastLayer == value) return;
			if (_lastLayer != null) _lastLayer.NextLayer = null;

			_lastLayer = value;

			if (value != null)
			{
				_compositorRect.Texture = value.GetTexture();
				if (value.NextLayer != this) value.NextLayer = this;
			}
		}
	}

	public PTCompositorLayer()
	{
		TransparentBg = true;
		Msaa2D = Viewport.Msaa.Disabled;

		CanvasLayer displayLayer = new();
		AddChild(displayLayer);

		_compositorRect = new();
		_compositorRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		displayLayer.AddChild(_compositorRect);
	}
	public PTCompositorLayer(Material effect)
		: this()
	{
		Effect = effect;
	}
}
