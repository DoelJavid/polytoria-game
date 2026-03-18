// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Attributes;
using Polytoria.Scripting;
using System;

namespace Polytoria.Datamodel.Resources;

/// <summary>
/// Base class for asset that is based on Resource
/// </summary>
[Abstract]
public partial class ResourceAsset : BaseAsset
{
	private bool _queueLoadResource = false;
	public event Action<Resource>? ResourceLoaded;
	public Resource? Resource { get; private set; }
	public bool IsResourceLoaded = false;
	public PTSignal ResourceLoadedInternal { get; private set; } = new();

	public override void EnterTree()
	{
		LoadResource();
		base.EnterTree();
	}

	public override void PreDelete()
	{
		ResourceLoadedInternal.DisconnectAll();
		base.PreDelete();
	}

	public override void Process(double delta)
	{
		if (_queueLoadResource)
		{
			_queueLoadResource = false;
			LoadResource();
		}
		base.Process(delta);
	}

	public void QueueLoadResource()
	{
		_queueLoadResource = true;
	}

	public virtual void LoadResource() { }

	protected void InvokeResourceLoaded(Resource resource)
	{
		Resource = resource;
		IsResourceLoaded = true;
		ResourceLoadedInternal.Invoke();
		ResourceLoaded?.Invoke(resource);
	}
}
