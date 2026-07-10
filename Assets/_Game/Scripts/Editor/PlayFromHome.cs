using UnityEditor;
using UnityEditor.SceneManagement;

namespace Game.EditorTools
{
    /// Force le Play Mode à démarrer sur Home.unity (parité web : on arrive toujours
    /// sur la page d'accueil, retour playtest 2026-07-10 point 3). Débrayable via
    /// Tools/Game/Play From Home pour tester une map directement.
    [InitializeOnLoad]
    static class PlayFromHome
    {
        const string MenuPath = "Tools/Game/Play From Home";
        const string PrefKey = "Game.PlayFromHome";
        const string HomeScenePath = "Assets/_Game/Scenes/Home.unity";

        static PlayFromHome() { EditorApplication.delayCall += Apply; }

        static bool Enabled => EditorPrefs.GetBool(PrefKey, true);

        static void Apply()
        {
            Menu.SetChecked(MenuPath, Enabled);
            EditorSceneManager.playModeStartScene = Enabled
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeScenePath)
                : null;
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            EditorPrefs.SetBool(PrefKey, !Enabled);
            Apply();
        }
    }
}
