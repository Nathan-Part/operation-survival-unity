using System.Collections;
using Game.Data.Rules;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Game.Runtime
{
    /// Déclenche le déploiement (bouton "Déployer" / tuile "Entraînement" / Entrée-Espace) :
    /// persiste la save, pose le paramètre one-shot LaunchParams.Training (consommé par
    /// SliceBootstrap au chargement de la scène jouable), affiche l'overlay plein écran
    /// "#boot-loading" au moins une frame avant le LoadScene pour éviter un flash de scène
    /// vide (anti-flash, parité web §3.7), puis charge la map résolue par MapRules.
    [RequireComponent(typeof(UIDocument))]
    public class GameLauncher : MonoBehaviour
    {
        // Query UI Toolkit différée à Start() : au Play Mode, l'instance de scène est
        // rechargée depuis le disque, donc toute référence VisualElement prise à l'édition
        // serait de toute façon invalide pour l'instance réelle (même logique que HomeController).
        VisualElement overlay;
        VisualElement progressBar;
        bool deploying;

        void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            overlay = root.Q<VisualElement>("boot-loading");
            progressBar = root.Q<VisualElement>("boot-loading-bar");
        }

        /// Persiste la save puis lance la map choisie. `training` bascule SliceBootstrap en mode
        /// entraînement (munitions infinies, pas de vagues) — voir LaunchParams.
        public void Deploy(MetaProgress meta, bool training)
        {
            if (deploying || meta == null || meta.Data == null) return;
            deploying = true;

            meta.Persist(); // web: aucune perte de progression au déploiement
            LaunchParams.Training = training;

            string mapId = meta.Data.settings.mapId;
            StartCoroutine(LoadAfterOverlay(mapId));
        }

        // Playtest Phase 5.1 (2026-07-09, G1) : chargement asynchrone avec vraie barre de progression
        // au lieu d'un LoadScene synchrone bloquant derriere un voile anti-flash sans retour visuel.
        // allowSceneActivation reste faux jusqu'a 90% (palier standard Unity : la derniere tranche
        // 90-100% est l'activation elle-meme, quasi instantanee, la retarder evite un saut visuel).
        IEnumerator LoadAfterOverlay(string mapId)
        {
            overlay?.RemoveFromClassList("hidden");
            SetProgress(0f);
            yield return null; // laisse l'overlay s'afficher au moins une frame avant le chargement (anti-flash)
            yield return null;

            var op = SceneManager.LoadSceneAsync(MapRules.SceneNameFor(mapId));
            op.allowSceneActivation = false;
            while (op.progress < 0.9f)
            {
                SetProgress(op.progress / 0.9f);
                yield return null;
            }
            SetProgress(1f);
            op.allowSceneActivation = true;
        }

        void SetProgress(float t)
        {
            if (progressBar != null) progressBar.style.width = Length.Percent(Mathf.Clamp01(t) * 100f);
        }
    }
}
