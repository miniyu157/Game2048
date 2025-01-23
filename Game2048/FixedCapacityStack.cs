using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game2048
{
    public class FixedCapacityStack<T>
    {
        private readonly LinkedList<T> _list = new LinkedList<T>();
        private readonly int _capacity;

        public FixedCapacityStack(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentException("Capacity must be greater than zero.", nameof(capacity));
            }
            _capacity = capacity;
        }

        public void Push(T item)
        {
            if (_list.Count == _capacity)
            {
                // Remove the bottom element to maintain the capacity
                _list.RemoveFirst();
            }
            _list.AddLast(item);
        }

        public T Pop()
        {
            if (_list.Count == 0)
            {
                throw new InvalidOperationException("The stack is empty.");
            }
            var value = _list.Last.Value;
            _list.RemoveLast();
            return value;
        }

        public IEnumerable<T> GetElements()
        {
            return _list;
        }

        public override string ToString()
        {
            return string.Join(" ", _list);
        }
    }
}
