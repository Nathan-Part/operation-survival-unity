using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime.UI.Tactical
{
    /// Construit les elements Tactical (TacticalPanel/TacticalButton) EN CODE et les substitue a un
    /// placeholder UXML existant (meme nom, memes classes, memes enfants deplaces).
    ///
    /// Pourquoi pas des tags UXML custom (<Tactical.TacticalPanel>) : teste et abandonne (playtest
    /// Phase 5.1, 2026-07-09) — meme avec [UxmlElement] (Unity 6) et une reimportation forcee des
    /// .uxml, l'importeur UXML de cette version d'editeur (6000.5.2f1) continue de logger "is
    /// missing a UxmlElementAttribute and has no registered factory method" alors que la reflexion
    /// confirme l'attribut present sur le type compile — signe que le generateur de source qui
    /// enregistre la factory ne tourne pas pour l'assembly Game.Runtime dans ce contexte. Plutot que
    /// de perdre plus de temps a diagnostiquer un gap d'outillage, les elements Tactical sont
    /// instancies directement en C#, ce qui marche inconditionnellement.
    ///
    /// Appeler ces methodes AVANT toute recherche `root.Q&lt;Button&gt;("nom")`/`Q&lt;VisualElement&gt;("nom")`
    /// qui cable des callbacks : l'element de remplacement porte le meme nom, donc les Query()
    /// suivantes le trouvent normalement.
    public static class TacticalUiKit
    {
        public static TacticalPanel ReplaceWithPanel(VisualElement placeholder, TacticalCorners corners, bool showAccent = false)
        {
            if (placeholder == null) return null;
            var parent = placeholder.parent;
            if (parent == null) return null;

            var panel = new TacticalPanel
            {
                name = placeholder.name,
                Corners = corners,
                ShowAccent = showAccent,
            };
            foreach (var cls in placeholder.GetClasses()) panel.AddToClassList(cls);

            int index = parent.IndexOf(placeholder);
            var children = new System.Collections.Generic.List<VisualElement>(placeholder.Children());
            foreach (var child in children) panel.Add(child); // Add() detache l'enfant de son parent precedent

            parent.Insert(index, panel);
            parent.Remove(placeholder);
            return panel;
        }

        public static TacticalButton ReplaceWithButton(Button placeholder, TacticalCorners corners, float cutSize, Color colorA, Color colorB)
        {
            if (placeholder == null) return null;
            var parent = placeholder.parent;
            if (parent == null) return null;

            var button = new TacticalButton
            {
                name = placeholder.name,
                text = placeholder.text,
                Corners = corners,
                CutSize = cutSize,
                ColorA = colorA,
                ColorB = colorB,
            };
            foreach (var cls in placeholder.GetClasses()) button.AddToClassList(cls);

            int index = parent.IndexOf(placeholder);
            var children = new System.Collections.Generic.List<VisualElement>(placeholder.Children());
            foreach (var child in children) button.Add(child);

            parent.Insert(index, button);
            parent.Remove(placeholder);
            return button;
        }
    }
}
