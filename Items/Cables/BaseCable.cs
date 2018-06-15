using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PotentiaTransport.Global;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheOneLibrary.Base.Items;

namespace PotentiaTransport.Items.Cables
{
	public abstract class BaseCable : BaseItem
	{
		public virtual int MaxIO => 0;

		public override void SetDefaults()
		{
			item.width = 12;
			item.height = 12;
			item.maxStack = 999;
			item.rare = 0;
			item.useStyle = ItemUseStyleID.SwingThrow;
			item.useTime = 10;
			item.useAnimation = 10;
			item.consumable = true;
			item.useTurn = true;
			item.autoReuse = true;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			tooltips.Add(new TooltipLine(mod, "PotentiaCore:TransferRate", $"Transfers electricity at {MaxIO} W"));
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool ConsumeItem(Player player)
		{
			if (player.altFunctionUse == 2) PTWorld.Instance.layer.Remove(player);
			else return PTWorld.Instance.layer.Place(player, Name);

			return false;
		}

		public override bool UseItem(Player player) => Vector2.Distance(Main.MouseWorld, player.Center) < 160;
	}
}