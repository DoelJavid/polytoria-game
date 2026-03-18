// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Attributes;
using Polytoria.Datamodel.Interfaces;

namespace Polytoria.Datamodel;

[Instantiable]
public partial class PhysicalModel : Physical, IGroup
{
	internal RigidBody3D RigidBody = null!;

	public override Node CreateGDNode()
	{
		return new RigidBody3D();
	}

	public override void InitGDNode()
	{
		base.InitGDNode();
		RigidBody = (RigidBody3D)GDNode;
		RigidBody.GravityScale = 2;
	}

	public override void Init()
	{
		base.Init();
		Anchored = true;
		CanCollide = true;
	}

	protected override void ApplyFreeze(bool to)
	{
		RigidBody.Freeze = to;
		base.ApplyFreeze(to);
	}
}
