using System;

namespace MyriadCreation.World.Settlement
{
public static class SettlementEconomy
{
	public static SettlementEconomySnapshot Read(SettlementData settlement)
	{
		if (settlement == null)
		{
			return default;
		}
		SettlementBlock block = settlement.GetBlock(SettlementBlockKind.Market);
		SettlementBlock block2 = settlement.GetBlock(SettlementBlockKind.Warehouse);
		SettlementBlock block3 = settlement.GetBlock(SettlementBlockKind.Workshop);
		SettlementBlock block4 = settlement.GetBlock(SettlementBlockKind.RoadNetwork);
		SettlementBlock block5 = settlement.GetBlock(SettlementBlockKind.Port);
		float marketSize = 0f;
		if (block != null)
		{
			marketSize = (float)block.level * 120f + (float)CountAttachments(block, SettlementBlockSubtype.Stall) * 30f + (float)CountAttachments(block, SettlementBlockSubtype.Exchange) * 80f + (float)CountAttachments(block, SettlementBlockSubtype.Caravanserai) * 40f + settlement.population * 0.05f + settlement.development * 2f + settlement.tradePower * 0.5f;
		}
		float warehouseCapacity = 0f;
		if (block2 != null)
		{
			warehouseCapacity = 800f * (float)Math.Max(1, block2.level) + (float)CountAttachments(block2, SettlementBlockSubtype.Granary) * 400f + (float)CountAttachments(block2, SettlementBlockSubtype.Depot) * 600f + (float)CountAttachments(block2, SettlementBlockSubtype.Cellar) * 200f;
		}
		int productionSlots = 0;
		if (block3 != null)
		{
			productionSlots = Math.Max(1, block3.level) + CountAttachments(block3, SettlementBlockSubtype.Forge) + CountAttachments(block3, SettlementBlockSubtype.Kiln) + CountAttachments(block3, SettlementBlockSubtype.Loom) + CountAttachments(block3, SettlementBlockSubtype.WorkshopHall);
		}
		float num = 0f;
		if (block4 != null)
		{
			num += (float)block4.level * 0.2f + (float)CountAttachments(block4, SettlementBlockSubtype.Bridge) * 0.1f + (float)CountAttachments(block4, SettlementBlockSubtype.PostStation) * 0.05f + (float)CountAttachments(block4, SettlementBlockSubtype.Causeway) * 0.05f;
		}
		if (block5 != null)
		{
			num += (float)block5.level * 0.25f + (float)CountAttachments(block5, SettlementBlockSubtype.Pier) * 0.05f + (float)CountAttachments(block5, SettlementBlockSubtype.Crane) * 0.05f;
		}
		num = Math.Min(num, 1.5f);
		return new SettlementEconomySnapshot(block != null, marketSize, warehouseCapacity, productionSlots, num);
	}

	public static float GetMarketSize(SettlementData settlement)
	{
		return Read(settlement).marketSize;
	}

	public static float GetWarehouseCapacity(SettlementData settlement)
	{
		return Read(settlement).warehouseCapacity;
	}

	public static int GetProductionSlots(SettlementData settlement)
	{
		return Read(settlement).productionSlots;
	}

	public static float GetTradeConnectivity(SettlementData settlement)
	{
		return Read(settlement).tradeConnectivity;
	}

	public static int CountAttachments(SettlementBlock block, SettlementBlockSubtype subtype)
	{
		if (block?.children == null)
		{
			return 0;
		}
		int num = 0;
		foreach (SettlementBlock child in block.children)
		{
			if (child != null)
			{
				if (child.subtype == subtype)
				{
					num++;
				}
				num += CountAttachments(child, subtype);
			}
		}
		return num;
	}
}
}
