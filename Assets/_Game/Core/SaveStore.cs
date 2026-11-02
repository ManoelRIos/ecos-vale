using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ecos
{
    [Serializable]
    public sealed class SaveData : ProgressionState
    {
        public int version = SaveStore.CurrentVersion, seed = 71319;
        public Vector3 position = new Vector3(0, 5, 0);
        public float time = .30f;
    }

    public static class SaveStore
    {
        public const int CurrentVersion = 3;
        public static string PathName => Path.Combine(Application.persistentDataPath, "ecos-slot1.json");
        public static bool Exists => File.Exists(PathName);

        public static bool TryLoad(out SaveData data, out string error)
        {
            data = null;
            try
            {
                return TryParse(File.ReadAllText(PathName), out data, out error);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool TryParse(string json, out SaveData data, out string error)
        {
            data = null;
            error = null;
            try
            {
                var parsed = JsonUtility.FromJson<SaveData>(json);
                Prepare(parsed);
                data = parsed;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        static void Prepare(SaveData data)
        {
            if (data == null || data.version < 1 || data.version > CurrentVersion
                || data.inventory == null || data.inventory.stacks == null || data.inventory.capacity < 1
                || data.inventory.capacity > 128 || data.inventory.stacks.Count > data.inventory.capacity
                || !Finite(data.position.x) || !Finite(data.position.y) || !Finite(data.position.z)
                || !Finite(data.time) || data.time < 0 || data.time >= 1
                || data.experience < 0 || data.experience > 1000000)
                throw new InvalidDataException("Save incompatível ou inválido.");
            ValidateIds(data.collected);
            ValidateIds(data.discovered);
            ValidateIds(data.restored);
            foreach (var stack in data.inventory.stacks)
                if (stack == null || string.IsNullOrEmpty(stack.id) || stack.count < 1 || stack.count > 50)
                    throw new InvalidDataException("Inventário inválido.");
            if (data.version == 1)
            {
                // v1 had no camp/lens/chests/journals. Existing shrine progress is retained.
                ProgressionRules.MigrateLegacy(data);
            }
            if (data.version < 3)
            {
                if (data.observedSpecies == null) data.observedSpecies = new List<string>();
                data.version = CurrentVersion;
            }
            ValidateIds(data.openedCaches);
            ValidateIds(data.readClues);
            ValidateIds(data.observedSpecies);
            foreach (string species in data.observedSpecies)
                if (!CreatureCatalog.IsSpecies(species))
                    throw new InvalidDataException("Espécie inválida no guia de campo.");
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        static void ValidateIds(List<string> ids)
        {
            if (ids == null || ids.Count > 20000)
                throw new InvalidDataException("Lista de progresso inválida.");
            var unique = new HashSet<string>();
            foreach (string id in ids)
                if (string.IsNullOrWhiteSpace(id) || !unique.Add(id))
                    throw new InvalidDataException("Identificador de progresso inválido ou duplicado.");
        }

        public static bool Write(SaveData data, out string error)
        {
            error = null;
            try
            {
                Prepare(data);
                string path = PathName, temporary = path + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(path))
                {
                    File.Copy(path, path + ".bak", true);
                    File.Replace(temporary, path, null);
                }
                else
                    File.Move(temporary, path);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }
    }
}
