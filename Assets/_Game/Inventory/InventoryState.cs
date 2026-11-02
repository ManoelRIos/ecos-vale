using System;
using System.Collections.Generic;

namespace Ecos
{
    [Serializable]
    public sealed class ItemStack
    {
        public string id;
        public int count;
        public ItemStack(string id, int count)
        {
            this.id = id;
            this.count = count;
        }
    }

    [Serializable]
    public sealed class InventoryState
    {
        public int capacity = 12;
        public List<ItemStack> stacks = new List<ItemStack>();
        public int Count(string id)
        {
            int n = 0;
            foreach (var s in stacks)
                if (s.id == id)
                    n += s.count;
            return n;
        }

        public bool Add(string id, int count, int limit = 50)
        {
            if (string.IsNullOrEmpty(id) || count <= 0 || limit <= 0)
                return false;
            int space = (capacity - stacks.Count) * limit;
            foreach (var s in stacks)
                if (s.id == id)
                    space += Math.Max(0, limit - s.count);
            if (count > space)
                return false;
            foreach (var s in stacks)
            {
                if (s.id != id)
                    continue;
                int n = Math.Min(count, Math.Max(0, limit - s.count));
                s.count += n;
                count -= n;
            }

            while (count > 0)
            {
                int n = Math.Min(count, limit);
                stacks.Add(new ItemStack(id, n));
                count -= n;
            }

            return true;
        }

        public bool Remove(string id, int count)
        {
            if (count <= 0 || Count(id) < count)
                return false;
            for (int i = stacks.Count - 1; i >= 0 && count > 0; i--)
            {
                var s = stacks[i];
                if (s.id != id)
                    continue;
                int n = Math.Min(count, s.count);
                s.count -= n;
                count -= n;
                if (s.count == 0)
                    stacks.RemoveAt(i);
            }

            return true;
        }
    }
}
