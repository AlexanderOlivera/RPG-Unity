# RPG 2D Top-Down — Implementación Completa (12 Requisitos)

## Objective
Convertir el prototipo actual (1 escena, 4 scripts, sin UI/audio/build) en un RPG 2D jugable y ejecutable en Windows con 5 escenarios conectados, IA con navegación, combate melee/ranged, inventario, UI completa, historia con Game Over y Victoria, menú y audio — reutilizando sprites, animaciones, packs y sonidos existentes.

## Changes
- `Assets/Scripts/PlayerController.cs` - refactor: movimiento en FixedUpdate, 4 direcciones, flipX, melee (cooldown, chequeo de dirección) y ranged (proyectil), SFX, respeta bloqueo de pausa/diálogo.
- `Assets/Scripts/Health.cs` - refactor: eventos `OnDamaged/OnHealthChanged/OnDied`, flag `destroyOnDeath` (jugador NO se destruye), ventana de invulnerabilidad, `ResetHealth()`.
- `Assets/Scripts/EnemyAI.cs` - refactor: aggro radius, leash a zona de vigilancia, movimiento por agente de navegación, ataque por cooldown, recompensa de puntaje al morir.
- `Assets/Scripts/CameraFollow.cs` - refactor: `SmoothDamp` con `Time.deltaTime`, clamp a `Collider2D` de límites, snap inicial, sin Lerp dependiente de FPS.
- `Assets/_RPG/Scripts/Core/` - (crear) `GameManager.cs`, `GameSession.cs` (estado persistente: score, kills, inventario, fragmentos, vida, escenario), `ScoreSystem.cs`, `InventorySystem.cs`, `ScenePortals.cs`, `GameBootstrap.cs` (crea managers/UI en cualquier escena al pulsar Play).
- `Assets/_RPG/Scripts/Player/` - (crear) `PlayerProjectile.cs`, `PlayerCombat.cs` (orquesta melee/ranged y cooldowns), `PlayerAnimatorBridge.cs`.
- `Assets/_RPG/Scripts/Enemies/` - (crear) `EnemyNavigation.cs` (wrapper NavMeshPlus/A*), `BossController.cs`, `EnemyReward.cs`.
- `Assets/_RPG/Scripts/Items/` - (crear) `ItemDefinition.cs` (ScriptableObject), `Collectible.cs`, `HealingItem.cs`, `DoorPortal.cs`.
- `Assets/_RPG/Scripts/NPC/` - (crear) `NpcInteractable.cs`, `DialogueDatabase.cs` (SO con líneas por NPC/escenario), `QuestTracker.cs`.
- `Assets/_RPG/Scripts/UI/` - (crear) `UIRootBuilder.cs` (Canvas+EventSystem+HUD 100% por código), `HudController.cs`, `InventoryPanel.cs`, `DialoguePanel.cs`, `PauseMenu.cs`, `GameOverScreen.cs`, `VictoryScreen.cs`, `MainMenuController.cs`, `OptionsMenu.cs`, `CreditsPanel.cs`.
- `Assets/_RPG/Scripts/Audio/` - (crear) `AudioManager.cs` (pool de AudioSource, música, master/music/sfx, persistente), `SfxLibrary.cs` (SO mapeando tus 5 sonidos + música).
- `Assets/_RPG/Data/` - (crear) ScriptableObjects: `ItemDefinition` (moneda, gema, corazón, fragmento), `DifficultySettings` (Normal/Difícil: daño/vida enemiga), `DialogueDatabase`.
- `Assets/_RPG/Prefabs/` - (crear) `Player.prefab`, `Enemy_Treant.prefab`, `Enemy_Mole.prefab`, `Boss.prefab`, `Coin.prefab`, `Gem.prefab`, `Heart.prefab`, `Fragment.prefab`, `Projectile.prefab`, `Npc.prefab`, `Portal.prefab`.
- `Assets/_RPG/Scenes/` - (crear) `00_MainMenu`, `01_Bosque`, `02_Aldea`, `03_Cuevas`, `04_Ruinas`, `05_Santuario`; `Assets/Scenes/FreeInteriorsSampleScene.unity` se conserva intacta como referencia.
- `Assets/Animation/player.controller` - añadir parámetro `AttackRanged` + estado `AttackRanged` (reutiliza clips `Attack_front/back/side`), y uso real del trigger `Die`.
- `Assets/_RPG/Editor/GameSceneBuilder.cs` - (crear) menú `Aura/Build 5 Escenarios` que arma las 5 escenas con tilemaps, colliders, límites de cámara, enemigos, NPC, coleccionables y portales.
- `Assets/_RPG/Editor/SelfTestRunner.cs` - (crear) menú `Aura/Run Self Test` que en Play Mode valida daño, melee, proyectil, moneda+score, inventario, portal, pausa, Game Over y victoria e imprime `SELFTEST PASS/FAIL` en consola.
- `ProjectSettings/EditorBuildSettings.asset` - añadir las 6 escenas en orden (vía `manage_build action=scenes`).
- `Assets/sound/*.ogg` - usados por `SfxLibrary` (mp3 duplicados se conservan, no se borran).

## Relevant Assets and Quirks
- Unity **6000.3.7f1**, URP **17.3 con Renderer2D** (`Assets/Settings/Renderer2D.asset`), Cinemachine 3.1.7, Input System 1.18, Pixel Perfect 5.1.1, 2D feature set completo, `com.unity.modules.ai` (NavMesh 3D) presente.
- `activeInputHandler: 2` (Both) → el Input Manager clásico funciona; los 4 scripts usan `Input.GetAxisRaw`/`KeyCode`. Se mantiene legacy para no romper nada, pero se centraliza en `PlayerCombat`/`PlayerController` y se documentan las teclas en pantalla y en el menú de opciones.
- `Assets/InputSystem_Actions.inputactions` existe (plantilla por defecto, sin usar). No se cablea para no arriesgar el flujo que ya funciona.
- `EditorBuildSettings.asset` → `m_Scenes: []` (nada jugable en build): se corrige.
- Escena actual: cámara con `CameraFillow` + `PixelPerfectCamera` y un `CinemachineCamera` con `CinemachineConfiner2D` **sin `CinemachineBrain`** (confiner muerto). Se deja `CameraFollow` como sistema único y se elimina la ambigüedad desactivando el `CinemachineCamera`, en lugar de duplicar control de cámara.
- Escala: sprites de jugador/mole 0.16×0.24 u, treant 1.56×1.81 u, tiles de 1×1 u, nivel `Fondo` 35×190 u. Los colliders (player 0.68×0.48) son ~4× el sprite → se normaliza la escala de personajes en los prefabs (≈ ×4) y se ajustan colliders a la silueta.
- Sprites ~200 PPU vs `PixelPerfectCamera.assetsPPU 100` → se alinea `assetsPPU` a 200 y ref 854×480 para evitar shimmer.
- Animator jugador: `player.controller` con `Blend`/`X`/`Y`/`Speed`/`Attack`/`Die`; blend trees 2D-SimpleDirectional ya cubren las 4 direcciones con `CaminarAbajo/Arriba/Izquierda` + `Idle_*` + `Attack_front/back/side`. No hay clip de "shoot": el ataque a distancia reutiliza los clips `Attack_*`.
- Enemigos: `enemy_mole.controller` (1 estado, sin Attack), `enemy-treant.controller`; `EnemyAI.player` asignado, layer `Enemies` (3), `enemyLayers` bits=8 → el melee ya apunta a la layer correcta.
- Audio: 5 SFX en `Assets/sound` (slash, hurt, item, enemy-death, ancient_path) en mp3 **y** ogg, y **cero AudioSource en escena**.
- No hay prefabs de gameplay, ni UI, ni Canvas, ni scripts de escena/menú, ni tests. `Assets/Sprites Externos`, `RafaelMatos`, `RPGW_Caves`, `Paletas` aportan tiles/props para los 5 escenarios.
- Consola limpia y `hasCompilationErrors: false` al inicio → cualquier error nuevo es mío y lo detecto con `check_compile_errors`.

## Steps
- [ ] 1. **Fase A — Respaldo y estabilización**: snapshot en `RPG-Unity_BACKUP_AURA_<ts>` con `execute_command` (robocopy, sin borrar nada), `git status` si existe repo; corregir `CameraFollow.cs` (SmoothDamp + clamp a límites), `Health.cs` (eventos, `destroyOnDeath=false` en jugador, invulnerabilidad), `PlayerController.cs` (FixedUpdate + flipX + melee con dirección y cooldown) y `EnemyAI.cs` (aggro/leash/FixedUpdate) manteniendo el flujo actual. Termina con `check_compile_errors` limpio. [depends: none]
- [ ] 2. **Fase A2 — Navegación de enemigos (NavMesh 2D)**: instalar `NavMeshPlus` como git package (o fallback A* grid si no compila, según respuesta a la pregunta 1), añadir `NavMeshSurface2D` y hornear en los escenarios, crear `EnemyNavigation.cs` y conectar `EnemyAI` al agente; validar con Play Mode que los enemigos rodean obstáculos, no atraviesan paredes y no vibran (`read_console` + capturas). [depends: 1]
- [ ] 3. **Fase B — Mecánicas principales**: crear `GameSession`, `GameManager`, `ScoreSystem`, `InventorySystem`, `PlayerCombat`, `PlayerProjectile`, `Collectible`, `HealingItem`, `ItemDefinition`/`DifficultySettings` SO, `NpcInteractable` + `DialogueDatabase`, `AudioManager` + `SfxLibrary`; extender `player.controller` con `AttackRanged`; crear prefabs `Player`, `Projectile`, `Coin`, `Gem`, `Heart`, `Npc` con componentes y referencias ya cableadas. Verificar compilación. [depends: 1,2]
- [ ] 4. **Fase B2 — UI y audio conectados**: `UIRootBuilder` + `HudController` (vida, monedas, puntaje, enemigos derrotados, botón inventario, mensajes), `InventoryPanel`, `DialoguePanel`, `PauseMenu`, `GameOverScreen`, `VictoryScreen`, `MainMenuController`, `OptionsMenu` (volumen master/música/SFX reales), `CreditsPanel`; registrar `GameBootstrap` y `AudioManager` persistentes; botones cableados con listeners reales. Verificar compilación + Play Mode. [depends: 3]
- [ ] 5. **Fase C — 5 escenarios y progresión**: `GameSceneBuilder` genera `01_Bosque`, `02_Aldea`, `03_Cuevas`, `04_Ruinas`, `05_Santuario` con tilemaps de tus paletas, `LimitesCamara`, spawn, 3-4 enemigos con zona de vigilancia, 1 NPC con diálogo, monedas/gemas/corazón, portales entre escenarios y boss final en `05_Santuario`; crear `00_MainMenu`; hornear NavMesh de cada escena; añadir las 6 escenas a Build Settings en orden. [depends: 4]
- [ ] 6. **Fase C2 — Historia, Game Over y Victoria**: `QuestTracker` con objetivo por escenario (4 fragmentos + derrotar al Guardián), diálogos de introducción, pantalla de derrota real (juego se pausa, opciones Reintentar/Menú), reinicio que resetea `GameSession` y recarga la escena, victoria al matar al boss y pantalla final con puntaje. Verificar compilación. [depends: 5]
- [ ] 7. **Fase D — Verificación y entrega**: ejecutar `Aura/Run Self Test` en Play Mode en cada escena y leer los `SELFTEST PASS/FAIL` (daño, melee, proyectil, moneda+score sin doble conteo, inventario, portal, pausa, Game Over, victoria, volumen), capturas de HUD/cada escenario, `manage_build` (build settings + intento de build Windows a `Builds/RPG.exe`), e informe final con la tabla de los 12 requisitos (implementado / prueba / resultado / pendientes). [depends: 6]

## Verify
- `check_compile_errors` sin errores tras cada fase (y `read_console` sin excepciones).
- Play Mode automatizado con `SelfTestRunner`: líneas `SELFTEST PASS` para daño melee, daño de proyectil, recogida + puntaje único, inventario, transición entre escenarios, pausa, Game Over → reinicio y victoria.
- Prueba manual guiada: recorrer `00_MainMenu → 01 → 02 → 03 → 04 → 05`, hablar con los 5 NPC, matar enemigos (no atraviesan muros ni vibran), recoger monedas, morir y reiniciar, matar al boss y ver victoria.
- `manage_build` deja las 6 escenas en Build Settings y genera `Builds/RPG.exe` si el módulo Windows está instalado (si no, pasos exactos: File → Build Profiles → Windows x86_64 → Build).
