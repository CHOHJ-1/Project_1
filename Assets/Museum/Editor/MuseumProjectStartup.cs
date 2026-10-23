#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Museum.Editor
{
    public static class MuseumProjectStartup
    {
        private const string ScenePath = "Assets/Museum/Scenes/MainScene.unity";
        private static string SessionKey => "Museum.MainSceneOpened." + Application.dataPath;

        [InitializeOnLoadMethod]
        private static void ScheduleFirstOpen()
        {
            if (!Application.isBatchMode)
                EditorApplication.delayCall += OpenInFreshProject;
        }

        private static void OpenInFreshProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(SessionKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OpenInFreshProject;
                return;
            }
            SessionState.SetBool(SessionKey, true);
            if (SceneManager.sceneCount != 1) return;
            Scene current = SceneManager.GetActiveScene();
            // Never replace a saved scene, a dirty scene, or unsaved user-created objects.
            if (!current.IsValid() || current.isDirty || !string.IsNullOrEmpty(current.path)) return;
            if (current.GetRootGameObjects().Any(root =>
                root.GetComponent<Camera>() == null && root.GetComponent<Light>() == null)) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Museum/Open Main Scene", false, 0)]
        public static void OpenMainScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }
}
#endif
