// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using MemoryPack;
using System.Text.Json.Serialization;

namespace Polytoria.Utils.DTOs;

[MemoryPackable]
public partial class Transform3DDto
{
	// MemoryPack no like nested classes for some reason, so we gonna string it
	public string BasisX { get; set; } = null!;
	public string BasisY { get; set; } = null!;
	public string BasisZ { get; set; } = null!;
	public string Origin { get; set; } = null!;

	[MemoryPackConstructor, JsonConstructor]
	public Transform3DDto() { }

	public Transform3DDto(Transform3D transform)
	{
		BasisX = Vector3Dto.ToString(transform.Basis.X);
		BasisY = Vector3Dto.ToString(transform.Basis.Y);
		BasisZ = Vector3Dto.ToString(transform.Basis.Z);
		Origin = Vector3Dto.ToString(transform.Origin);
	}

	public Transform3D ToTransform3D()
	{
		Basis basis = new(
			Vector3Dto.FromString(BasisX),
			Vector3Dto.FromString(BasisY),
			Vector3Dto.FromString(BasisZ)
		);

		return new Transform3D(basis, Vector3Dto.FromString(Origin));
	}

	// String helpers because memory pack don't like nested objects
	public static Transform3DDto FromString(string str)
	{
		var parts = str.Split('|');
		return new Transform3DDto
		{
			BasisX = parts[0],
			BasisY = parts[1],
			BasisZ = parts[2],
			Origin = parts[3]
		};
	}

	public static string ToString(Transform3D transform)
	{
		return $"{Vector3Dto.ToString(transform.Basis.X)}|{Vector3Dto.ToString(transform.Basis.Y)}|{Vector3Dto.ToString(transform.Basis.Z)}|{Vector3Dto.ToString(transform.Origin)}";
	}

}
