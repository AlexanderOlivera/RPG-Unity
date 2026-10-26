# RPG 2D Top-Down — Implementación Completa (12 Requisitos)

## Objective
Convertir el prototipo actual (1 escena, 4 scripts, sin UI/audio/build) en un RPG 2D jugable y ejecutable en Windows con 5 escenarios conectados, IA con navegación de rejilla (A*), combate melee/ranged, inventario, UI completa, historia con Game Over y Victoria, menú y audio — reutilizando sprites, animaciones, packs y sonidos existentes.

## Decisión técnica de navegación (requisito 4)
Elegida la **opción B: A\* en rejilla propia**, sin dependencias externas.
- Unity 6000 no puede hornear NavMesh sobre `Tilemap` 2D sin una extensión externa (NavMeshPlus/NavMeshSurface2D), que descartamos.
- Solución implementada: `NavGridBuilder` rasteriza el área jugable de cada escena en una rejilla de celdas de 0.5 u marcando celdas bloqueadas por `Collider2D` estáticos (`Physics2D.OverlapBox` con `LayerMask` de obstáculos) → `GridPathfinder` calcula ruta A\* (coste 1 / diagonal 1.41, heurística octil) → `AgentMover2D` sigue la ruta con `Rigidbody2D.MovePosition` **en `FixedUpdate`** + separación entre agentes para evitar vibración.
- El informe final declarará explícitamente: *no se usa NavMesh; se usa A\* sobre rejilla construida desde los colliders de la escena*, con el motivo y la verificación correspondiente.

## Changes
- `Assets/Scripts/PlayerController.cs` - refactor: movimiento en `FixedUpdate`, 4 direcciones, flipX, melee (cooldown + chequeo de dirección) y ranged (proyectil), SFX, bloqueo durante pausa/diálogo.
- `Assets/Scripts/Health.cs` - refactor: eventos `OnDamaged/OnHealthChanged/OnDied`, flag `destroyOnDeath` (jugador NO se destruye), invulnerabilidad temporal, `ResetHealth()`.
- `Assets/Scripts/EnemyAI.cs` - refactor: aggro radius, leash a zona de vigilancia, movimiento por `AgentMover2D`, ataque por cooldown, recompensa de puntaje al morir.
- `Assets/Scripts/CameraFollow.cs` - refactor: `SmoothDamp` con `Time.deltaTime`, clamp a `Collider2D` de límites, snap inicial.
- `Assets/_RPG/Scripts/AI/` - (crear) `NavGridBuilder.cs`, `GridPathfinder.cs` (A\*), `AgentMover2D.cs`, `EnemyNavigation.cs`.
- `Assets/_RPG/Scripts/Core/` - (crear) `GameManager.cs`, `GameSession.cs` (score, kills, inventario, fragmentos, vida, escenario), `ScoreSystem.cs`, `InventorySystem.cs`, `ScenePortal.cs`, `GameBootstrap.cs`.
- `Assets/_RPG/Scripts/Player/` - (crear) `PlayerProjectile.cs`, `PlayerCombat.cs`.
- `Assets/_RPG/Scripts/Enemies/` - (crear) `BossController.cs`, `EnemyReward.cs`.
- `Assets/_RPG/Scripts/Items/` - (crear) `ItemDefinition.cs` (SO), `Collectible.cs`, `HealingItem.cs`.
- `Assets/_RPG/Scripts/NPC/` - (crear) `NpcInteractable.cs`, `DialogueDatabase.cs` (SO), `QuestTracker.cs`.
- `Assets/_RPG/Scripts/UI/` - (crear) `UIRootBuilder.cs` (Canvas+EventSystem+HUD por código, UGUI con texto integrado), `HudController.cs`, `InventoryPanel.cs`, `DialoguePanel.cs`, `PauseMenu.cs`, `GameOverScreen.cs`, `VictoryScreen.cs`, `MainMenuController.cs`, `OptionsMenu.cs`, `CreditsPanel.cs`.
- `Assets/_RPG/Scripts/Audio/` - (crear) `AudioManager.cs` (pool de AudioSource, música, master/music/sfx), `SfxLibrary.cs` (SO con tus 5 sonidos).
- `Assets/_RPG/Data/` - (crear) SO: `ItemDefinition` (moneda, gema, corazón, fragmento), `DifficultySettings`, `DialogueDatabase`.
- `Assets/_RPG/Prefabs/` - (crear) `Player`, `Enemy_Treant`, `Enemy_Mole`, `Boss`, `Coin`, `Gem`, `Heart`, `Fragment`, `Projectile`, `Npc`, `Portal` (todos con componentes y referencias ya cableadas).
- `Assets/_RPG/Scenes/` - (crear) `00_MainMenu`, `01_Bosque`, `02_Aldea`, `03_Cuevas`, `04_Ruinas`, `05_Santuario`; `Assets/Scenes/FreeInteriorsSampleScene.unity` se conserva intacta.
- `Assets/Animation/player.controller` - añadir parámetro/estado `AttackRanged` (reutiliza clips `Attack_front/back/side`) y uso real del trigger `Die`.
- `Assets/_RPG/Editor/GameSceneBuilder.cs` - (crear) menú `Aura/Build 5 Escenarios`: tilemaps con tus paletas, colliders de muros, `LimitesCamara`, spawn, enemigos, NPC, coleccionables, portales y rejilla de navegación.
- `Assets/_RPG/Editor/SelfTestRunner.cs` - (crear) menú `Aura/Run Self Test`: valida en Play Mode daño, melee, proyectil, moneda+score sin doble conteo, inventario, nav-grid, portal, pausa, Game Over, victoria y volumen; imprime `SELFTEST PASS/FAIL`.
- `ProjectSettings/EditorBuildSettings.asset` - añadir las 6 escenas en orden vía `manage_build action=scenes`.
- `Assets/sound/*.ogg` - usados por `SfxLibrary` (los mp3 duplicados se conservan).

## Relevant Assets and Quirks
- Unity **6000.3.7f1**, URP **17.3 + Renderer2D** (`Assets/Settings/Renderer2D.asset`), Cinemachine 3.1.7, Input System 1.18, Pixel Perfect 5.1.1, `com.unity.modules.ai` (NavMesh 3D, no apto para Tilemap 2D).
- `activeInputHandler: 2` (Both) → el Input Manager clásico funciona; los 4 scripts usan `Input.GetAxisRaw`/`KeyCode`: se mantiene legacy (probado) y se documentan las teclas en HUD y opciones.
- `Assets/InputSystem_Actions.inputactions` existe sin usar (plantilla). No se cablea para no arriesgar el flujo que ya funciona.
- `EditorBuildSettings.asset` → `m_Scenes: []`: se corrige (bloqueante para build).
- Cámara: `CameraFillow` activo + `PixelPerfectCamera`, y un `CinemachineCamera` con `CinemachineConfiner2D` **sin `CinemachineBrain`** (confiner muerto). Se unifica en `CameraFollow` y se desactiva el `CinemachineCamera`, sin duplicar control de cámara.
- Escala: jugador/mole 0.16×0.24 u, treant 1.56×1.81 u, tiles 1×1 u, nivel `Fondo` 35×190 u; colliders (player 0.68×0.48) ~4× el sprite → se normaliza la escala de personajes en prefabs y se ajustan colliders a la silueta.
- Sprites ~200 PPU vs `PixelPerfectCamera.assetsPPU 100` → se alinea a 200 con ref 854×480.
- `player.controller`: `Blend/X/Y/Speed/Attack/Die`; blend trees 2D-SimpleDirectional ya cubren las 4 direcciones (`Caminar*/Idle_*/Attack_*`). No hay clip de disparo: el ataque a distancia reutiliza `Attack_*`.
- Enemigos: `enemy_mole.controller` (1 estado), `enemy-treant.controller`; layer `Enemies` (3) y `enemyLayers` bits=8 → el melee ya apunta bien.
- Audio: 5 SFX en `Assets/sound` (slash, hurt, item, enemy-death, ancient_path) duplicados en mp3/ogg y **cero AudioSource en escena**.
- Sin prefabs de gameplay, sin Canvas/UI, sin menú, sin tests. Tiles/props disponibles en `Sprites Externos` (Cainos), `RafaelMatos`, `RPGW_Caves`, `Paletas`.
- Consola limpia y `hasCompilationErrors: false` al inicio → cualquier error nuevo es mío y lo detecto con `check_compile_errors`.

## Steps
- [ ] 1. **Fase A — Respaldo y estabilización**: snapshot con `execute_command` (robocopy a `RPG-Unity_BACKUP_AURA_<ts>`, sin borrar nada) + `git status` si hay repo; corregir `CameraFollow.cs` (SmoothDamp + clamp), `Health.cs` (eventos, `destroyOnDeath=false` en jugador, invulnerabilidad), `PlayerController.cs` (FixedUpdate, flipX, melee con dirección y cooldown) y `EnemyAI.cs` (aggro/leash); `check_compile_errors` limpio. [depends: none]
- [ ] 2. **Fase A2 — Navegación A\* (requisito 4)**: crear `NavGridBuilder.cs` (rasteriza la escena en rejilla 0.5 u desde colliders estáticos), `GridPathfinder.cs` (A\*), `AgentMover2D.cs` (`MovePosition` en `FixedUpdate`, separación anti-vibración) y `EnemyNavigation.cs`; conectar `EnemyAI` a esa navegación y validar en Play Mode que rodea muros y no los atraviesa (`read_console` + captura). [depends: 1]
- [ ] 3. **Fase B — Mecánicas principales**: crear `GameSession`, `GameManager`, `ScoreSystem`, `InventorySystem`, `PlayerCombat`, `PlayerProjectile`, `Collectible`, `HealingItem`, SO `ItemDefinition`/`DifficultySettings`, `NpcInteractable` + `DialogueDatabase`, `AudioManager` + `SfxLibrary`; añadir `AttackRanged` a `player.controller`; crear prefabs `Player`, `Projectile`, `Coin`, `Gem`, `Heart`, `Npc` con referencias cableadas; `check_compile_errors`. [depends: 1,2]
- [ ] 4. **Fase B2 — UI y audio conectados**: `UIRootBuilder` + `HudController` (vida, monedas, puntaje, enemigos derrotados, botón inventario, mensajes), `InventoryPanel`, `DialoguePanel`, `PauseMenu`, `GameOverScreen`, `VictoryScreen`, `MainMenuController`, `OptionsMenu` (volumen master/música/SFX reales), `CreditsPanel`; `GameBootstrap` y `AudioManager` persistentes; botones con listeners reales; verificar compilación + Play Mode. [depends: 3]
- [ ] 5. **Fase C — 5 escenarios y progresión**: `GameSceneBuilder` genera `01_Bosque`, `02_Aldea`, `03_Cuevas`, `04_Ruinas`, `05_Santuario` (tilemaps de tus paletas, `LimitesCamara`, spawn, 3-4 enemigos con zona de vigilancia, 1 NPC, monedas/gemas/corazón, portales y boss final) + `00_MainMenu`; construir rejilla de navegación por escena; añadir las 6 escenas a Build Settings en orden. [depends: 4]
- [ ] 6. **Fase C2 — Historia, Game Over y Victoria**: `QuestTracker` (4 fragmentos + derrotar al Guardián), diálogos de introducción por escenario, derrota real (pausa + Reintentar/Menú), reinicio que resetea `GameSession` y recarga la escena, victoria al matar al boss con pantalla final y puntaje; `check_compile_errors`. [depends: 5]
- [ ] 7. **Fase D — Verificación y entrega**: ejecutar `Aura/Run Self Test` en Play Mode por escenario leyendo `SELFTEST PASS/FAIL`, capturas de HUD y de cada escenario, `manage_build` (build settings + intento de build Windows a `Builds/RPG.exe`) e informe final con la tabla de los 12 requisitos (implementado / prueba realizada / resultado / pendientes). [depends: 6]

## Verify
- `check_compile_errors` sin errores tras cada fase + `read_console` sin excepciones.
- Play Mode con `SelfTestRunner`: `SELFTEST PASS` para daño melee, daño de proyectil, recogida con puntaje único, inventario, navegación A\* (enemigo rodea obstáculo y no lo atraviesa), transición entre escenarios, pausa, Game Over → reinicio, victoria.
- Recorrido guiado `00_MainMenu → 01 → 02 → 03 → 04 → 05`, hablando con los 5 NPC y recogiendo coleccionables.
- `manage_build` deja las 6 escenas en Build Settings y genera `Builds/RPG.exe` (si el módulo Windows no está instalado, se reporta y se dan los pasos exactos en Unity).
- En el informe final se declara explícitamente qué se probó ejecutando Play Mode y qué solo se revisó por código; nada se marca como verificado sin prueba.
