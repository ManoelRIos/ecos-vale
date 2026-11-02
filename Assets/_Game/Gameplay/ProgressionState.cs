using System;
using System.Collections.Generic;

namespace Ecos
{
    /// <summary>Serializable progression without engine objects, shared by saves and authority rules.</summary>
    [Serializable]
    public class ProgressionState
    {
        public InventoryState inventory = new InventoryState();
        public List<string> collected = new List<string>();
        public List<string> discovered = new List<string>();
        public List<string> restored = new List<string>();
        public List<string> openedCaches = new List<string>();
        public List<string> readClues = new List<string>();
        public bool rewarded;
        public bool campBuilt;
        public bool lensCrafted;
        public int experience;
    }
}
