# Dette technique — pipeline d'import JSON → ScriptableObjects

> Relevé par la revue finale de la branche `feat/spec-import-scriptableobjects` (2026-07-06). **Rien de bloquant** — la branche est jugée « ready to merge », tous les invariants durs tiennent et sont testés. Ces points sont des améliorations à faire lors d'une future passe de durcissement (idéalement avant/pendant la phase 4 « systèmes méta », quand l'import resservira).

## Non bloquant, à améliorer un jour

1. **Duplication de helpers entre importeurs.** `Set<T>(ref …)`, le bucketing Created/Updated/Unchanged, et les comparateurs `SkillsEqual`/`BonusesEqual` sont copiés dans les 5 importeurs de catalogue. Ils sont purs → à centraliser dans `AssetImportUtil`. (Touche 5 fichiers : refactor à faire d'un bloc, avec re-run complet des tests.)

2. **Reporting incohérent des config singletons.** Sur les 6 configs singleton, seules `EconomyConfig` et `WaveConfig` s'ajoutent à `report.Updated` (les 4 autres — Movement/Upgrade/EnemyCombat/PowerUp — sont silencieuses). Conséquence : un re-import idempotent affiche toujours `Updated ≥ 2`. À normaliser (soit toutes silencieuses, soit toutes reportées, idéalement avec un dirty-check).

3. **Idempotence sous-testée pour 4 domaines.** Seul `WeaponImporterTests` vérifie qu'un 2ᵉ import ne marque rien « Updated » (per-entrée). Enemies/characters/items/powerups n'ont pas d'assertion d'idempotence dédiée (l'invariant tient par inspection + le helper partagé `Set(ref)`). Ajouter une assertion d'idempotence par domaine.

4. **Ref d'arme non résolue → null silencieux.** `EnemyImporter` met `weapon = null` si l'id d'arme du JSON ne correspond à aucune `WeaponData`, sans warning (contrairement au chemin des stats manquantes qui appelle `WarnDefault`). Un id mal orthographié passerait inaperçu. Ajouter un warning par symétrie.

5. **Tests de préservation de ref d'asset — pas de round-trip disque.** Les tests type `Import_NeverOverwritesHandAssignedAssetRefs` assignent un `new GameObject(...)` en mémoire et ne forcent pas un rechargement de domaine, donc ils prouvent que l'importeur n'écrase pas le champ *en session*, pas que la ref survit à la sérialisation disque. L'invariant qui compte (l'importeur n'écrit jamais les champs d'art) est prouvé par inspection sur tous les importeurs.

6. **Texte de warning cosmétique.** `EnemyImporter.WarnDefault` affiche le nom de membre C# (`HpMult`) au lieu de la clé JSON (`hpMult`).

7. **`SaveAssets()` par asset (O(N) flushes).** Déviation assumée du plan (save groupé) vers un save eager par asset, choisie pour la stabilité empirique dans l'environnement Unity piloté par MCP. Perf non pertinente à cette échelle (catalogues ≤ 24 entrées, import éditeur manuel). Si un jour gros catalogue : `AssetDatabase.StartAssetEditing()/StopAssetEditing()` autour de la boucle + un seul `SaveAssets()`.

## Ce qui est solide (ne pas « re-corriger »)

- Discipline art-ref honorée uniformément (aucun importeur n'écrit `modelPrefab`/`icon`/`image`/`animationClip` ; seule exception sanctionnée : `EnemyData.weapon`).
- Couplage `UpgradeConfig` (armes + joueur écrivent le même singleton sans s'écraser) : correct et testé.
- Fix `_doc` dans `JsonFile.Read` : général et nécessaire (corrige un vrai crash de désérialisation de dictionnaire).
- Layering d'assemblies runtime / editor / tests : propre, sans fuite editor dans le runtime.
- Formules (`WaveRules`/`EconomyRules`/`UpgradeFormulas`/`StatFormulas`) : séparées de la donnée importée et testées contre valeurs de référence.
