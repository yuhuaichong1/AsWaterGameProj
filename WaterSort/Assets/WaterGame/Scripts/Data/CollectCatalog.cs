using System.Collections.Generic;

namespace AsGame.Data
{
    public struct CollectEntry
    {
        public int id;
        public int unlockLevel;
        public string name;
    }

    public static class CollectCatalog
    {
        public static readonly List<CollectEntry> Drinks = new()
        {
            new CollectEntry { id = 1, unlockLevel = 2, name = "原味奶茶" },
            new CollectEntry { id = 2, unlockLevel = 4, name = "珍珠奶茶" },
            new CollectEntry { id = 3, unlockLevel = 6, name = "芒芒甘露" },
            new CollectEntry { id = 4, unlockLevel = 8, name = "西瓜啵啵" },
            new CollectEntry { id = 5, unlockLevel = 10, name = "芋泥啵啵" },
            new CollectEntry { id = 6, unlockLevel = 12, name = "布丁奶茶" },
            new CollectEntry { id = 7, unlockLevel = 14, name = "黑糖啵啵" },
            new CollectEntry { id = 8, unlockLevel = 16, name = "薄荷黑巧" },
        };
    }
}
