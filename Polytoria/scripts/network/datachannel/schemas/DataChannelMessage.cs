// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using MemoryPack;

namespace Polytoria.Networking.DataChannel.Schemas;

[MemoryPackable]
[MemoryPackUnion(0, typeof(MessageAuthenticate))]
[MemoryPackUnion(1, typeof(MessageAuthRes))]
[MemoryPackUnion(2, typeof(MessageData))]
public partial interface IDataServerMessage
{
}

[MemoryPackable]
public partial class MessageAuthenticate : IDataServerMessage
{
	public string AuthToken = "";
	public int PeerID = 0;
}

[MemoryPackable]
public partial class MessageAuthRes : IDataServerMessage
{
}

[MemoryPackable]
public partial class MessageData : IDataServerMessage
{
	public byte[] Data = [];
}
