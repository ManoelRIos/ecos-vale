using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ecos
{
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1, seed = 71319;
        public Vector3 position = new Vector3(0, 5, 0);
        public InventoryState inventory = new InventoryState();
        public List<string> collected = new List<string>(), discovered = new List<string>(), restored = new List<string>();
        public bool rewarded;
        public float time = .30f;
    }

    public static class SaveStore
    {
        public static string PathName => Path.Combine(Application.persistentDataPath, "ecos-slot1.json");
        public static bool Exists => File.Exists(PathName);

        public static bool TryLoad(out SaveData data, out string error)
        {
            data = null;
            error = null;
            try
            {
                var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(PathName));
                if (d == null || d.version != 1 || d.inventory == null || d.collected == null || d.discovered == null || d.restored == null || d.inventory.stacks == null || float.IsNaN(d.time) || float.IsInfinity(d.time) || d.time < 0 || d.time >= 1 || d.inventory.capacity < 1 || d.inventory.stacks.Count > d.inventory.capacity || float.IsNaN(d.position.x) || float.IsNaN(d.position.y) || float.IsNaN(d.position.z) || float.IsInfinity(d.position.x) || float.IsInfinity(d.position.y) || float.IsInfinity(d.position.z))
                    throw new InvalidDataException("Save incompatível ou inválido.");
                foreach (var s in d.inventory.stacks)
                    if (s == null || string.IsNullOrEmpty(s.id) || s.count < 1 || s.count > 50)
                        throw new InvalidDataException("Inventário inválido.");
                data = d;
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public static bool Write(SaveData d, out string error)
        {
            error = null;
            try
            {
                string path = PathName, tmp = path + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(tmp, JsonUtility.ToJson(d, true));
                if (File.Exists(path))
                {
                    File.Copy(path, path + ".bak", true);
                    File.Replace(tmp, path, null);
                }
                else
                    File.Move(tmp, path);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }
    }
}
