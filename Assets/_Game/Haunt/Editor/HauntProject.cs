using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ecos.Haunt.Editor
{
    /// <summary>Entry scene commands; these never save or replace a scene during Play Mode.</summary>
    public static class HauntProject
    {
        public const string ScenePath = "Assets/_Game/Scenes/Haunt.unity";

        [MenuItem("Ecos/Abrir roguelike", false, 0)]
        public static void Open()
        {
            if (!CanOpen())
            {
                Debug.LogWarning("Ecos: pare o Play Mode e aguarde compilação/importação antes de abrir o roguelike.");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        // -executeMethod runs after script import; delay once more so the generated
        // project resources and any pending asset refresh finish before scene opening.
        public static void OpenFromCommandLine()
        {
            EditorApplication.delayCall -= OpenWhenReady;
            EditorApplication.delayCall += OpenWhenReady;
        }

        static void OpenWhenReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Ecos: pare o Play Mode antes de abrir o roguelike pelo atalho.");
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OpenWhenReady;
                return;
            }
            Open();
        }

        [MenuItem("Ecos/Abrir roguelike", true)]
        public static bool CanOpen()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode
                && !EditorApplication.isCompiling && !EditorApplication.isUpdating;
        }

        /// <summary>Batch import smoke check. Does not enter Play Mode or validate rendering.</summary>
        public static void SmokeCheck()
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
                throw new InvalidOperationException("A cena Haunt não foi importada.");
            var entry = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled);
            if (entry == null || entry.path != ScenePath)
                throw new InvalidOperationException("Haunt deve ser a primeira cena habilitada no build.");
            var previous = SceneManager.GetActiveScene();
            var existing = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !existing.isLoaded;
            var scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : existing;
            try
            {
                var games = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<HauntGame>(true)).ToArray();
                if (games.Length != 1 || !games[0].enabled)
                    throw new InvalidOperationException("A cena deve conter exatamente um HauntGame habilitado.");
                using (var art = new HauntPixelArt())
                {
                    foreach (string id in new[] { "player", "pumpkin", "ghost", "cultist", "bat", "boss", "floor0", "wall", "portal", "merchant", "bullet-player", "bullet-enemy" })
                    {
                        var sprite = art.Get(id);
                        if (!sprite || sprite.texture.filterMode != FilterMode.Point)
                            throw new InvalidOperationException("Sprite pixel art ausente ou com filtro incorreto: " + id);
                    }
                    if (!art.Material || !art.Material.shader)
                        throw new InvalidOperationException("Material de sprites ausente.");
                }
                var floor = DungeonFloor.Generate(71319, 1);
                if (floor.Rooms.Count < 7 || floor.Boss.Kind != RoomKind.Boss || !floor.Entrance.Cleared)
                    throw new InvalidOperationException("Geração inicial inválida.");
                Debug.Log("Haunt import smoke: scene bootstrap, pixel sprite resources, build entry and core generation PASS. Play Mode/rendering remain separate checks.");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
