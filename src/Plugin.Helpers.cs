using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.SteamAPI;

namespace K4AlwaysWeaponSkins;

public sealed partial class Plugin
{
	public readonly record struct AmmoState(int Clip1, int Clip2, int Reserve0, int Reserve1);

	private IPlayer? GetPlayerFromItemServices(nint itemServicesAddress)
	{
		try
		{
			var itemServices = Core.Memory.ToSchemaClass<CCSPlayer_ItemServices>(itemServicesAddress);
			var csPlayerPawn = itemServices.Pawn?.As<CCSPlayerPawn>();
			var controller = csPlayerPawn?.OriginalController.Value;

			if (controller?.IsValid != true)
				return null;

			return Core.PlayerManager.GetPlayer((int)controller.Index - 1);
		}
		catch
		{
			return null;
		}
	}

	private unsafe bool HasPlayerSkinForWeapon(IPlayer player, string weaponName, Team team)
	{
		if (_findMatchingWeapons == null || player.PlayerPawn == null)
			return false;

		try
		{
			var originalTeam = player.Controller.Team;
			bool teamChanged = false;

			try
			{
				if (originalTeam != team)
				{
					SetPlayerTeam(player, team);
					teamChanged = true;
				}

				using var vector = new ManagedCUtlVector<nint>(0, 16);
				using var nameHandle = new InteropHelp.UTF8StringHandle(weaponName);

				fixed (CUtlVector<nint>* pVector = &vector.Value)
				{
					_findMatchingWeapons.Call(player.PlayerPawn.Address, nameHandle.DangerousGetHandle(), (int)team, 0, (nint)pVector);
				}

				if (vector.Value.Count > 0 && vector.Value[0] != IntPtr.Zero)
				{
					var econItem = Core.Memory.ToSchemaClass<CEconItemView>(vector.Value[0]);
					return econItem.ItemID > 0;
				}
			}
			finally
			{
				if (teamChanged)
					SetPlayerTeam(player, originalTeam);
			}

			return false;
		}
		catch (Exception ex)
		{
			Core.Logger.LogError(ex, "Error checking weapon skin");
			return false;
		}
	}

	private static void SetPlayerTeam(IPlayer player, Team team)
	{
		if (player.Controller != null)
		{
			player.Controller.TeamNum = (byte)team;
			player.Controller.TeamNumUpdated();
		}

		if (player.PlayerPawn != null)
		{
			player.PlayerPawn.TeamNum = (byte)team;
			player.PlayerPawn.TeamNumUpdated();
		}
	}

	private static Team GetOppositeTeam(Team team) => team == Team.T ? Team.CT : Team.T;
}
