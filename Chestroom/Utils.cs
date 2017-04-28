using InfChests;
using Terraria;
using TShockAPI;

namespace ChestroomPlugin
{
	public static class Utils
	{
		public static void ConvertToAutoRefill(int start, int end)
		{
			for (int i = start; i < start + end; i++)
			{
				Chest c = Main.chest[i];

				if (c == null)
					continue;

				DB.addChest(new InfChest()
				{
					items = c.item,
					isPublic = true,
					refillTime = 0,
					userid = -1,
					x = c.x,
					y = c.y,
				});
			}
		}

		public static void informplayers(bool hard = false)
		{
			foreach (TSPlayer ts in TShock.Players)
			{
				if (ts == null || !ts.Active)
					continue;

				for (int i = 0; i < 255; i++)
				{
					for (int j = 0; j < Main.maxSectionsX; j++)
					{
						for (int k = 0; k < Main.maxSectionsY; k++)
							Netplay.Clients[i].TileSections[j, k] = false;
					}
				}
			}
		}
	}
}
