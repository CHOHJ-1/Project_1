#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Museum.Editor
{
    public static class MuseumProjectStartup
    {
        private const string ScenePath = "Assets/Museum/Scenes/MainScene.unity";

        // Open only on explicit user action, after Unity has finished restoring its own scene state.
        [MenuItem("Museum/Open Main Scene", false, 0)]
        public static void OpenMainScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            Scene current = SceneManager.GetActiveScene();
            if (SceneManager.sceneCount == 1 && current.path == ScenePath) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }
}
#endif
