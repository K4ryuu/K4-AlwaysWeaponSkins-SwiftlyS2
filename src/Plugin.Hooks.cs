using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace K4AlwaysWeaponSkins;

public sealed partial class Plugin
{
	private GiveNamedItemDelegate OnGiveNamedItemHook(Func<GiveNamedItemDelegate> callNext)
	{
		return (pItemServices, weaponName) =>
		{
			IPlayer? player = null;
			Team originalTeam = Team.None;
			bool teamChanged = false;
			string? classname = null;

			try
			{
				classname = Marshal.PtrToStringUTF8(weaponName);
				if (string.IsNullOrEmpty(classname) || !classname.StartsWith("weapon_") || WeaponHelper.IsKnife(classname))
					return callNext()(pItemServices, weaponName);

				player = GetPlayerFromItemServices(pItemServices);
				if (player == null || !player.IsValid || player.IsFakeClient)
					return callNext()(pItemServices, weaponName);

				var playerTeam = player.Controller.Team;
				var oppositeTeam = GetOppositeTeam(playerTeam);

				if (HasPlayerSkinForWeapon(player, classname, playerTeam))
					return callNext()(pItemServices, weaponName);

				if (!HasPlayerSkinForWeapon(player, classname, oppositeTeam))
					return callNext()(pItemServices, weaponName);

				originalTeam = playerTeam;
				SetPlayerTeam(player, oppositeTeam);
				teamChanged = true;
			}
			catch (Exception ex)
			{
				Core.Logger.LogError(ex, "Error in GiveNamedItem hook (Pre)");
			}

			var result = callNext()(pItemServices, weaponName);

			try
			{
				if (teamChanged && player != null && player.IsValid)
					SetPlayerTeam(player, originalTeam);

				if (player != null && !string.IsNullOrEmpty(classname))
					_pickupLocks.Remove($"{player.SteamID}_{classname}");
			}
			catch (Exception ex)
			{
				Core.Logger.LogError(ex, "Error in GiveNamedItem hook (Post)");
			}

			return result;
		};
	}

	public static class WeaponHelper
	{
		private static readonly HashSet<CSWeaponType> SkinnableTypes =
		[
			CSWeaponType.WEAPONTYPE_KNIFE,
			CSWeaponType.WEAPONTYPE_MACHINEGUN,
			CSWeaponType.WEAPONTYPE_PISTOL,
			CSWeaponType.WEAPONTYPE_RIFLE,
			CSWeaponType.WEAPONTYPE_SNIPER_RIFLE,
			CSWeaponType.WEAPONTYPE_SHOTGUN,
			CSWeaponType.WEAPONTYPE_SUBMACHINEGUN,
			CSWeaponType.WEAPONTYPE_TASER
		];

		public static bool IsSkinnable(CSWeaponType type) => SkinnableTypes.Contains(type);

		public static bool IsKnife(string classname) =>
			classname.Contains("knife", StringComparison.OrdinalIgnoreCase) ||
			classname.Contains("bayonet", StringComparison.OrdinalIgnoreCase);
	}
}
