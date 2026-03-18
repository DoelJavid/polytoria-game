// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Polytoria.Scripting.Luau;

/// <summary>
/// Lua types
/// </summary>
public enum LuaType
{
	/// <summary>
	/// 
	/// </summary>
	None = -1,
	/// <summary>
	/// LUA_TNIL
	/// </summary>
	Nil = 0,
	/// <summary>
	/// LUA_TBOOLEAN
	/// </summary>
	Boolean = 1,
	/// <summary>
	/// LUA_TLIGHTUSERDATA
	/// </summary>
	LightUserData = 2,
	/// <summary>
	/// LUA_TNUMBER
	/// </summary>
	Number = 3,
	/// <summary>
	/// LUA_TVECTOR
	/// </summary>
	Vector = 4,
	/// <summary>
	/// LUA_TSTRING
	/// </summary>
	String = 5,
	/// <summary>
	/// LUA_TTABLE
	/// </summary>
	Table = 6,
	/// <summary>
	/// LUA_TFUNCTION
	/// </summary>
	Function = 7,
	/// <summary>
	/// LUA_TUSERDATA
	/// </summary>
	UserData = 8,
	/// <summary>
	/// LUA_TTHREAD
	/// </summary>
	/// //
	Thread = 9,
	/// <summary>
	/// LUA_TBUFFER
	/// </summary>
	/// //
	Buffer = 10
}
