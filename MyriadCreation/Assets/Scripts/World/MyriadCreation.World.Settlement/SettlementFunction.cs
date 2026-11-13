using System;

namespace MyriadCreation.World.Settlement
{
[Flags]
public enum SettlementFunction
{
	None = 0,
	Administrative = 1,
	Military = 2,
	Commercial = 4,
	Religious = 8,
	Mining = 0x10,
	Agricultural = 0x20,
	Crossing = 0x40,
	Tariff = 0x80,
	Port = 0x100,
	Fortress = 0x200,
	PostStation = 0x400,
	Cultural = 0x800,
	Industrial = 0x1000
}
}
