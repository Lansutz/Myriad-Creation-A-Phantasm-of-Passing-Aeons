using System;

namespace MyriadCreation.Core.Enums
{
[Flags]
public enum OccupationFlags
{
	None = 0,
	Farmer = 1,
	Artisan = 2,
	Merchant = 4,
	Scholar = 8,
	Soldier = 0x10,
	Official = 0x20,
	Priest = 0x40,
	Miner = 0x80,
	Mariner = 0x100,
	Pastoralist = 0x200,
	Physician = 0x400,
	Teacher = 0x800,
	Laborer = 0x1000
}
}
