using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Memory;
using SwiftlyS2.Shared.Plugins;

namespace K4AlwaysWeaponSkins;

[PluginMetadata(
	Id = "k4.alwaysweaponskins",
	Version = "1.0.0",
	Name = "K4 - Always Weapon Skins",
	Author = "K4ryuu",
	Description = "Apply inventory skins to opposing teams as well."
)]
public sealed partial class Plugin(ISwiftlyCore core) : BasePlugin(core)
{
	private PluginConfig _config = new();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void FindMatchingWeaponsDelegate(nint pPawn, nint weaponName, int team, byte searchInventory, nint outVector);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate nint GiveNamedItemDelegate(nint pItemServices, nint weaponName);

	private IUnmanagedFunction<FindMatchingWeaponsDelegate>? _findMatchingWeapons;
	private IUnmanagedFunction<GiveNamedItemDelegate>? _giveNamedItem;
	private Guid _giveNamedItemHookId;

	private readonly Dictionary<int, Dictionary<string, AmmoState>> _savedAmmo = [];
	private readonly HashSet<string> _pickupLocks = [];

	public override void Load(bool hotReload)
	{
		LoadConfig();
		InitializeNativeFunctions();
		Core.GameEvent.HookPost<EventItemPickup>(OnItemPickup);
	}

	public override void Unload()
	{
		if (_giveNamedItem != null && _giveNamedItemHookId != Guid.Empty)
			_giveNamedItem.RemoveHook(_giveNamedItemHookId);

		_savedAmmo.Clear();
		_pickupLocks.Clear();
	}

	private void LoadConfig()
	{
		const string FileName = "config.jsonc";
		const string Section = "K4AlwaysWeaponSkins";

		Core.Configuration
			.InitializeJsonWithModel<PluginConfig>(FileName, Section)
			.Configure(cfg => cfg.AddJsonFile(Core.Configuration.GetConfigPath(FileName), optional: false, reloadOnChange: false));

		_config = Core.Configuration.Manager.GetSection(Section).Get<PluginConfig>() ?? new();
	}

	private void InitializeNativeFunctions()
	{
		try
		{
			if (Core.GameData.TryGetSignature("CCSPlayer_FindMatchingWeaponsForTeamLoadout", out var findWeaponsAddr))
			{
				_findMatchingWeapons = Core.Memory.GetUnmanagedFunctionByAddress<FindMatchingWeaponsDelegate>(findWeaponsAddr);
				Core.Logger.LogDebug("FindMatchingWeaponsForTeamLoadout @ 0x{Address:X}", findWeaponsAddr);
			}
			else
			{
				Core.Logger.LogWarning("FindMatchingWeaponsForTeamLoadout signature not found.");
			}

			if (Core.GameData.TryGetOffset("CCSPlayer_ItemServices::GiveNamedItem", out var giveItemOffset))
			{
				var vtable = Core.Memory.GetVTableAddress("server", "CCSPlayer_ItemServices");
				if (vtable.HasValue)
				{
					_giveNamedItem = Core.Memory.GetUnmanagedFunctionByVTable<GiveNamedItemDelegate>(vtable.Value, (int)giveItemOffset);
					_giveNamedItemHookId = _giveNamedItem.AddHook(OnGiveNamedItemHook);
					Core.Logger.LogDebug("GiveNamedItem hook @ vtable offset {Offset}", giveItemOffset);
				}
				else
				{
					Core.Logger.LogWarning("CCSPlayer_ItemServices vtable not found.");
				}
			}
			else
			{
				Core.Logger.LogWarning("GiveNamedItem offset not found.");
			}
		}
		catch (Exception ex)
		{
			Core.Logger.LogError(ex, "Failed to initialize native functions");
		}
	}
}
