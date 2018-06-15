using Microsoft.Xna.Framework;
using PotentiaTransport.Global;
using Terraria;
using Terraria.ID;
using TheOneLibrary.Base.Items;

namespace PotentiaTransport.Items
{
	public class Wrench : BaseItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Wrench");
			Tooltip.SetDefault("Used for modifying cables");
		}

		public override void SetDefaults()
		{
			item.width = 12;
			item.height = 12;
			item.useStyle = ItemUseStyleID.SwingThrow;
			item.useTime = 10;
			item.useAnimation = 10;
			item.useTurn = true;
			item.autoReuse = true;
		}

		public override bool UseItem(Player player)
		{
			if (Vector2.Distance(Main.MouseWorld, player.Center) > 160) return false;

			PTWorld.Instance.layer.Modify(player);
			return true;
		}
	}
}