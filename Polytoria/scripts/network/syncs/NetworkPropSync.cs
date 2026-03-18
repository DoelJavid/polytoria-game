// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Attributes;
using Polytoria.Datamodel;
using Polytoria.Datamodel.Data;
using Polytoria.Datamodel.Services;
using Polytoria.Shared;
using Polytoria.Utils;
using Polytoria.Utils.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using static Polytoria.Datamodel.Services.NetworkService;

namespace Polytoria.Networking.Synchronizers;

[Internal]
public sealed partial class NetworkPropSync : Instance
{
	internal NetworkService NetService = null!;

	// Queue of pending prop updates (waiting for their NetworkedObject to spawn)
	private readonly Dictionary<string, List<NetPropReplicateData>> _pendingProps = [];

	// Queue of pending references (waiting for recipient to spawn)
	public readonly Dictionary<NetPropNetworkedObjectRef, NetworkedObject> PendingRefs = [];

	private static readonly bool _useNetworkLog = false;

	static NetworkPropSync()
	{
		_useNetworkLog = OS.HasFeature("netlog");
	}

	public static byte[] SerializePropValue(object? propValue)
	{
		if (propValue == null)
		{
			return [];
		}

		if (propValue is Vector2 v2) propValue = new Vector2Dto(v2);
		else if (propValue is Vector3 v3) propValue = new Vector3Dto(v3);
		else if (propValue is Color c) propValue = new ColorDto(c);
		else if (propValue is Transform3D t) propValue = new Transform3DDto(t);
		else if (propValue is ColorSeries cs) propValue = new ColorSeriesDto(cs);
		else if (propValue is NumberRange nr) propValue = new NumberRangeDto(nr);

		Type propType = propValue.GetType();

		if (propType.IsEnum)
		{
			// Enums serialized as int
			int intValue = Convert.ToInt32(propValue);
			return SerializeUtils.Serialize(intValue);
		}

		return SerializeUtils.Serialize(propType, propValue);
	}

	public static object? DeserializePropValue(byte[] data, Type targetType)
	{
		if (data.Length == 0)
		{
			return null;
		}
		if (targetType.IsAssignableTo(typeof(NetworkedObject)))
		{
			if (data.Length == 0)
			{
				return null;
			}

			NetPropNetworkedObjectRef nref = SerializeUtils.Deserialize<NetPropNetworkedObjectRef>(
				data
			)!;

			return nref;
		}

		object? intermediateValue = null;

		if (targetType == typeof(Vector2))
		{
			Vector2Dto? dto = SerializeUtils.Deserialize<Vector2Dto?>(data);
			if (dto != null) intermediateValue = dto.ToVector2();
		}
		else if (targetType == typeof(Vector3))
		{
			Vector3Dto? dto = SerializeUtils.Deserialize<Vector3Dto?>(data);
			if (dto != null) intermediateValue = dto.ToVector3();
		}
		else if (targetType == typeof(Color))
		{
			ColorDto? dto = SerializeUtils.Deserialize<ColorDto?>(data);
			if (dto != null) intermediateValue = dto.ToColor();
		}
		else if (targetType == typeof(Transform3D))
		{
			Transform3DDto? dto = SerializeUtils.Deserialize<Transform3DDto?>(data);
			if (dto != null) intermediateValue = dto.ToTransform3D();
		}
		else if (targetType == typeof(ColorSeries))
		{
			ColorSeriesDto? dto = SerializeUtils.Deserialize<ColorSeriesDto?>(data);
			if (dto != null) intermediateValue = dto.ToColorRange();
		}
		else if (targetType == typeof(NumberRange))
		{
			NumberRangeDto? dto = SerializeUtils.Deserialize<NumberRangeDto?>(data);
			if (dto != null) intermediateValue = dto.ToNumberRange();
		}
		else
		{
			// Standard source-generated type info
			if (targetType.IsEnum)
			{
				int enumValue = SerializeUtils.Deserialize<int>(data);
				if (Enum.IsDefined(targetType, enumValue))
				{
					intermediateValue = Enum.ToObject(targetType, enumValue);
				}
				else
				{
					PT.PrintErr("Enum not defined: ", targetType.Name, ": ", enumValue);
					return null;
				}
			}
			else
			{
				try
				{
					intermediateValue = SerializeUtils.Deserialize(targetType, data);
				}
				catch (Exception ex)
				{
					PT.PrintErr(ex);
					return null;
				}
			}
		}

		return intermediateValue;
	}

	public static async Task<byte[]> SerializePropValueAsync(object? propValue)
	{
		if (propValue == null)
		{
			return [];
		}
		if (propValue is Vector2 v2) propValue = new Vector2Dto(v2);
		else if (propValue is Vector3 v3) propValue = new Vector3Dto(v3);
		else if (propValue is Color c) propValue = new ColorDto(c);
		else if (propValue is Transform3D t) propValue = new Transform3DDto(t);
		Type propType = propValue.GetType();

		using var ms = new MemoryStream();
		if (propType.IsEnum)
		{
			// Enums serialized as int
			int intValue = Convert.ToInt32(propValue);
			await SerializeUtils.SerializeAsync(ms, intValue);
		}
		else
		{
			await SerializeUtils.SerializeAsync(propType, ms, propValue);
		}
		return ms.ToArray();
	}

	public static async Task<object?> DeserializePropValueAsync(byte[] data, Type targetType)
	{
		if (data.Length == 0)
		{
			return null;
		}
		using MemoryStream mem = new(data);
		if (targetType.IsAssignableTo(typeof(NetworkedObject)))
		{
			if (data.Length == 0)
			{
				return null;
			}
			NetPropNetworkedObjectRef? nref = await SerializeUtils.DeserializeAsync<NetPropNetworkedObjectRef>(
				mem
			)!;
			return nref;
		}
		object? intermediateValue = null;
		if (targetType == typeof(Vector2))
		{
			Vector2Dto? dto = await SerializeUtils.DeserializeAsync<Vector2Dto?>(mem);
			if (dto != null) intermediateValue = dto.ToVector2();
		}
		else if (targetType == typeof(Vector3))
		{
			Vector3Dto? dto = await SerializeUtils.DeserializeAsync<Vector3Dto?>(mem);
			if (dto != null) intermediateValue = dto.ToVector3();
		}
		else if (targetType == typeof(Color))
		{
			ColorDto? dto = await SerializeUtils.DeserializeAsync<ColorDto?>(mem);
			if (dto != null) intermediateValue = dto.ToColor();
		}
		else if (targetType == typeof(Transform3D))
		{
			Transform3DDto? dto = await SerializeUtils.DeserializeAsync<Transform3DDto?>(mem);
			if (dto != null) intermediateValue = dto.ToTransform3D();
		}
		else
		{
			// Standard source-generated type info
			if (targetType.IsEnum)
			{
				int enumValue = await SerializeUtils.DeserializeAsync<int>(mem);
				if (Enum.IsDefined(targetType, enumValue))
				{
					intermediateValue = Enum.ToObject(targetType, enumValue);
				}
				else
				{
					PT.PrintErr("Enum not defined: ", targetType.Name, ": ", enumValue);
					return null;
				}
			}
			else
			{
				try
				{
					intermediateValue = await SerializeUtils.DeserializeAsync(targetType, mem);
				}
				catch (Exception ex)
				{
					PT.PrintErr(ex);
					return null;
				}
			}
		}
		return intermediateValue;
	}

	public void BroadcastPropUpdate(NetworkedObject netObj, string propName, object? propValue, bool unreliable)
	{
		if (!netObj.IsNetworkReady) return;
		if (!netObj.Root.IsLoaded) return;

		if (propValue is NetworkedObject nobj)
		{
			propValue = nobj.GetObjectRef();
			if (propValue == null)
			{
				return;
			}
		}
		string netID = netObj.NetworkedObjectID;
		byte[] data = SerializePropValue(propValue);

		BroadcastPropUpdateRaw(netID, propName, data, unreliable);
	}

	public void BroadcastPropUpdateRaw(string netID, string propName, byte[] data, bool unreliable, int excludePeer = -1)
	{
		if (Root.Network.NetInstance == null) return;
		var methodName = unreliable ? nameof(NetRecvPropUpdateUnreliable) : nameof(NetRecvPropUpdate);

		foreach (var peer in Root.Network.NetInstance.PeerIds)
		{
			// Exclude the peer
			if (peer == excludePeer) continue;
			RpcId(peer, methodName, netID, propName, data);
		}
	}

	public void NetSendAllPropUpdate(NetworkedObject netObj, int toPeerId)
	{
		NetPropReplicateData[] propData = netObj.GetNetPropReplicateData();
		string netID = netObj.NetworkedObjectID;

		RpcId(toPeerId, nameof(NetRecvPropUpdateBatch), netID, JsonSerializer.Serialize(propData, NetDataGenerationContext.Default.NetPropReplicateDataArray));
	}

	[NetRpc(AuthorityMode.Authority, TransferMode = TransferMode.UnreliableOrdered, CallLocal = false, TransferChannel = 1)]
	private void NetRecvPropUpdateUnreliable(string netID, string propName, byte[] propValueRaw)
	{
		RecvPropUpdate(netID, propName, propValueRaw);
	}

	[NetRpc(AuthorityMode.Authority, TransferMode = TransferMode.Reliable, CallLocal = false, TransferChannel = 1)]
	private void NetRecvPropUpdate(string netID, string propName, byte[] propValueRaw)
	{
		RecvPropUpdate(netID, propName, propValueRaw);
	}

	private void RecvPropUpdate(string netID, string propName, byte[] propValueRaw)
	{
		NetworkedObject? netObj = NetService.Root.GetNetObjectFromID(netID);

		if (netObj != null)
		{
			netObj.RecvPropUpdate(propName, propValueRaw);
		}
		else
		{
			// Queue the update until the netObj exists
			if (!_pendingProps.TryGetValue(netID, out List<NetPropReplicateData>? value))
			{
				value = [];
				_pendingProps[netID] = value;
			}

			value.Add(new NetPropReplicateData
			{
				name = propName,
				valueRaw = propValueRaw
			});
		}
	}

	public void BroadcastPropUpdateToServer(NetworkedObject netObj, string propName, object? propValue, bool unreliable)
	{
		if (!netObj.IsNetworkReady) return;
		if (!netObj.Root.IsLoaded) return;

		if (propValue is NetworkedObject nobj)
		{
			propValue = nobj.GetObjectRef();
			if (propValue == null)
			{
				return;
			}
		}
		string netID = netObj.NetworkedObjectID;
		byte[] data = SerializePropValue(propValue);

		if (unreliable)
		{
			RpcId(1, nameof(NetRecvPropUpdateToServerUnreliable), netID, propName, data);
		}
		else
		{
			RpcId(1, nameof(NetRecvPropUpdateToServer), netID, propName, data);
		}
	}

	[NetRpc(AuthorityMode.Any, TransferMode = TransferMode.Reliable)]
	private void NetRecvPropUpdateToServer(string netID, string propName, byte[] propValueRaw)
	{
		RecvPropUpdateToServer(RemoteSenderId, netID, propName, propValueRaw, false);
	}

	[NetRpc(AuthorityMode.Any, TransferMode = TransferMode.Unreliable)]
	private void NetRecvPropUpdateToServerUnreliable(string netID, string propName, byte[] propValueRaw)
	{
		RecvPropUpdateToServer(RemoteSenderId, netID, propName, propValueRaw, true);
	}

	private void RecvPropUpdateToServer(int peerID, string netID, string propName, byte[] propValueRaw, bool isUnreliable)
	{
		NetworkedObject? netObj = NetService.Root.GetNetObjectFromID(netID);

		if (netObj != null)
		{
			PropertyInfo? propInfo = netObj.GetSyncProperty(propName);

			// Target property doesn't exist
			if (propInfo == null) return;

			SyncVarAttribute? sv = propInfo.GetCustomAttribute<SyncVarAttribute>();

			bool hasAuthority = false;

			if (sv != null)
			{
				if (sv.AllowAuthorWrite && netObj.NetworkAuthority == peerID)
				{
					// Has authority from AllowAuthorWrite
					hasAuthority = true;
				}

				if (sv.ServerOnly && peerID != 1)
				{
					// Disallow if from non server
					hasAuthority = false;
				}
			}
			else
			{
				// Check normally via NetPropAuthority
				hasAuthority = CheckAuthority(peerID, netObj.NetPropAuthority);
			}

			if (hasAuthority)
			{
				netObj.RecvPropUpdate(propName, propValueRaw);
				BroadcastPropUpdateRaw(netObj.NetworkedObjectID, propName, propValueRaw, isUnreliable, peerID);
			}
		}
	}

	[NetRpc(AuthorityMode.Authority, TransferMode = TransferMode.Reliable, TransferChannel = 1)]
	private void NetRecvPropUpdateBatch(string nodePath, string propDataRaw)
	{
		NetworkedObject? netObj = NetService.Root.GetNetObj(nodePath);
		NetPropReplicateData[] propReplicates = JsonSerializer.Deserialize(propDataRaw, NetDataGenerationContext.Default.NetPropReplicateDataArray)!;

		if (netObj != null)
		{
			foreach (NetPropReplicateData r in propReplicates)
			{
				netObj.RecvPropUpdate(r.name, r.valueRaw);
			}
		}
		else
		{
			// Queue the batch until netObj exists
			if (!_pendingProps.TryGetValue(nodePath, out List<NetPropReplicateData>? value))
			{
				value = [];
				_pendingProps[nodePath] = value;
			}

			value.AddRange(propReplicates);
		}
	}

	/// Flush pending props
	public void FlushPendingProps(NetworkedObject netObj)
	{
		string netID = netObj.NetworkedObjectID;
		if (_pendingProps.TryGetValue(netID, out List<NetPropReplicateData>? queued))
		{
			foreach (NetPropReplicateData r in queued)
			{
				netObj.RecvPropUpdate(r.name, r.valueRaw);
			}

			_pendingProps.Remove(netID);
		}
	}

	// Resolve objects that point to another object
	public void LookForResolvePending(NetworkedObject netObj)
	{
		foreach ((NetPropNetworkedObjectRef nref, NetworkedObject target) in PendingRefs)
		{
			if (nref.NetID == netObj.NetworkedObjectID)
			{
				try
				{
					nref.TargetProp!.SetValue(target, netObj);
				}
				catch (Exception ex)
				{
					GD.PushError(nref.TargetProp, $" set failure (id {nref.NetID}) ", ex);
				}
				PendingRefs.Remove(nref);
				break;
			}
		}
	}

}
