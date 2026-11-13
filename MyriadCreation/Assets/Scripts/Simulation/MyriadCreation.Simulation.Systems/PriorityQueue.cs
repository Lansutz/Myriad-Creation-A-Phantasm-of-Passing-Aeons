using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Systems
{
public class PriorityQueue<T, TPriority> where TPriority : IComparable<TPriority>
{
	private List<(T item, TPriority priority)> _elements = new List<(T, TPriority)>();

	public int Count => _elements.Count;

	public void Enqueue(T item, TPriority priority)
	{
		_elements.Add((item, priority));
		int num = _elements.Count - 1;
		while (num > 0)
		{
			int num2 = (num - 1) / 2;
			(T, TPriority) tuple = _elements[num2];
			ref TPriority item2 = ref tuple.Item2;
			TPriority item3 = _elements[num].priority;
			if (item2.CompareTo(item3) > 0)
			{
				List<(T, TPriority)> elements = _elements;
				int index = num2;
				List<(T item, TPriority priority)> elements2 = _elements;
				int index2 = num;
				tuple = _elements[num];
				(T, TPriority) value = _elements[num2];
				elements[index] = tuple;
				elements2[index2] = value;
				num = num2;
				continue;
			}
			break;
		}
	}

	public T Dequeue()
	{
		if (_elements.Count == 0)
		{
			throw new InvalidOperationException("Queue empty");
		}
		T item = _elements[0].item;
		int index = _elements.Count - 1;
		_elements[0] = _elements[index];
		_elements.RemoveAt(index);
		int num = 0;
		while (true)
		{
			int num2 = 2 * num + 1;
			int num3 = 2 * num + 2;
			int num4 = num;
			(T, TPriority) tuple;
			if (num2 < _elements.Count)
			{
				tuple = _elements[num2];
				ref TPriority item2 = ref tuple.Item2;
				TPriority item3 = _elements[num4].priority;
				if (item2.CompareTo(item3) < 0)
				{
					num4 = num2;
				}
			}
			if (num3 < _elements.Count)
			{
				tuple = _elements[num3];
				ref TPriority item4 = ref tuple.Item2;
				TPriority item5 = _elements[num4].priority;
				if (item4.CompareTo(item5) < 0)
				{
					num4 = num3;
				}
			}
			if (num4 == num)
			{
				break;
			}
			List<(T, TPriority)> elements = _elements;
			int index2 = num;
			List<(T item, TPriority priority)> elements2 = _elements;
			int index3 = num4;
			tuple = _elements[num4];
			(T, TPriority) value = _elements[num];
			elements[index2] = tuple;
			elements2[index3] = value;
			num = num4;
		}
		return item;
	}
}
}
