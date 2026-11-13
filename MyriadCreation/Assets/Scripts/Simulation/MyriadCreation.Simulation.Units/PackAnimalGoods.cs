using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Units
{
public static class PackAnimalGoods
{
	public static bool TryGetKind(int goodsId, out PackAnimalKind kind)
	{
		switch (goodsId)
		{
		case 20:
			kind = PackAnimalKind.Horse;
			return true;
		case 21:
			kind = PackAnimalKind.Ox;
			return true;
		case 23:
			kind = PackAnimalKind.Camel;
			return true;
		case 24:
			kind = PackAnimalKind.Donkey;
			return true;
		case 25:
			kind = PackAnimalKind.Mule;
			return true;
		case 26:
			kind = PackAnimalKind.Yak;
			return true;
		case 27:
			kind = PackAnimalKind.Reindeer;
			return true;
		case 28:
			kind = PackAnimalKind.Llama;
			return true;
		default:
			kind = PackAnimalKind.Exotic;
			return false;
		}
	}
}
}
