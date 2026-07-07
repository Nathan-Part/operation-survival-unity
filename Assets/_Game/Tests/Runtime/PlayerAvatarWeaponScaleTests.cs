using Game.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Runtime.Tests
{
    /// Garde-fou contre la régression "arme géante en 3ᵉ personne" (2026-07-08) :
    /// AttachWeapon compensait aveuglément l'échelle du bone de la main (nécessaire pour
    /// le rig du perso, cf. blue-soldier.glb -> RightHand.lossyScale = 0.01) sans corriger
    /// le propre défaut d'export du modèle d'arme (ar-15.glb mesuré ~2 m au lieu de ~0,9 m),
    /// ce qui rendait l'arme tenue quasi aussi grande que le personnage.
    public class PlayerAvatarWeaponScaleTests
    {
        const string CharacterPath = "Assets/_Game/Art/Characters/blue-soldier.glb";
        const string WeaponPath = "Assets/_Game/Art/Weapons/ar-15.glb";
        const string WeaponDataPath = "Assets/_Game/Data/Weapons/default.asset";

        [Test]
        public void AttachWeapon_HeldWeaponWorldSizeStaysSane()
        {
            var charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
            var weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPath);
            var weaponData = AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponDataPath);
            Assert.IsNotNull(charPrefab, "GLB perso introuvable — asset déplacé/renommé ?");
            Assert.IsNotNull(weaponPrefab, "GLB arme introuvable — asset déplacé/renommé ?");
            Assert.IsNotNull(weaponData, "WeaponData 'default' introuvable — asset déplacé/renommé ?");

            var avatarGo = new GameObject("TestPlayerAvatar");
            try
            {
                var avatar = avatarGo.AddComponent<PlayerAvatar>();
                avatar.Init(charPrefab, null, null, weaponPrefab, weaponData.modelScale);

                var hand = PlayerAvatar.FindHandBone(avatarGo.transform);
                Assert.IsNotNull(hand, "Bone main droite introuvable sur blue-soldier.glb — rig changé ?");
                Assert.AreEqual(1, hand.childCount, "L'arme tenue devrait être l'unique enfant du bone main.");

                var held = hand.GetChild(0);
                var renderers = held.GetComponentsInChildren<Renderer>();
                Assert.Greater(renderers.Length, 0, "L'arme tenue n'a aucun Renderer — impossible de mesurer sa taille.");

                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                float longestAxis = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                // Cible réaliste pour un fusil d'assaut : ~0,7-1,2 m. Marge large pour ne pas être
                // fragile si le mesh est légèrement retouché, mais bien assez stricte pour attraper
                // un facteur d'échelle oublié (x100 => ~30-200 m, ou une arme quasi invisible).
                Assert.That(longestAxis, Is.InRange(0.3f, 1.5f),
                    $"Arme tenue hors gabarit réaliste (plus grande dimension = {longestAxis:F2} m) — " +
                    "régression probable sur la compensation d'échelle d'AttachWeapon.");
            }
            finally
            {
                Object.DestroyImmediate(avatarGo);
            }
        }
    }
}
