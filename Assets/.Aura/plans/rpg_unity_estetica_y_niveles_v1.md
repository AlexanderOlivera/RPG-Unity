# RPG-Unity — Niveles ampliados, reescalado de entidades y rediseño de UI

## Objective
Ampliar los 5 escenarios con decoración densa por bioma, unificar el tamaño de NPCs y enemigos con el treant y rehacer la interfaz con un kit visual temático, para después retomar los sistemas pendientes y la entrega.

## Changes
- `Assets/_RPG/Scripts/UI/UiKit.cs` — (crear) paleta, marcos con borde, textos con sombra, botones con estados, barra de vida y utilidades de anclaje.
- `Assets/_RPG/Scripts/UI/UIRoot.cs` — `BuildUi()` reescrito con el kit; conserva la API pública y los nombres `PausePanel`, `GameOverPanel`, `VictoryPanel`, `InventoryPanel`, `DialoguePanel`.
- `Assets/_RPG/Scripts/UI/MainMenuUI.cs` — `BuildUi()` reescrito con el mismo kit (título, menú, opciones, créditos).
- `Assets/_RPG/Editor/GameSceneBuilder.cs` — nuevas dimensiones, `Blocks` de estructura por bioma, sistema de props con zonas seguras, reescalado de prefabs, colliders reajustados y re-layout de enemigos/objetos/portales.
- `Assets/_RPG/Scenes/00_MainMenu.unity` … `05_Santuario.unity` — regeneradas por el builder.

## Relevant Assets and Quirks
- Props Cainos ya disponibles en `Assets/Sprites Externos/Cainos/Pixel Art Top Down - Basic/Prefab/`: `Plant/PF Plant - Tree 01..03`, `Bush 01..06`, `Grass 01..15`, `Flower 01`; `Props/PF Props - Stone 01..07`, `Pillar 01/02`, `Rune Pillar X2/X3`, `Rune Pillar Broken`, `Gravestone 01..03`, `Statue 01`, `Altar 01`, `Stone Coffin 01 H`, `Stone Bench 01 E/S/W`, `Barrel 01`, `Crate 01/02`, `Pot 01..03`, `Chest 01`, `Brick 01..03`, `Road Sign 01 E/W`.
- Sorting layers en uso: `fondo`=2 (props), `player`=7, `enemy`=8, `npc`=9; `Floor` sortingOrder −20, muros −5, coleccionables 10 → los props deben ir en `fondo` para quedar bajo los personajes.
- Los props **no llevan collider** (decoración): `NavGridBuilder` rasteriza solo la layer `Walls`, y los enemigos son `Kinematic` con collider trigger. Lo que debe bloquear se declara en `Blocks` (layer `Walls`).
- Escalas actuales en el builder: Player **4**, NPC **4**, `Enemy_Mole` **4**, `Enemy_Treant` **1**, `Boss_Guardian` **1.35** → el treant es la referencia de tamaño.
- Sprites generados ya se importan con `FullRect` (necesario para `SpriteDrawMode.Tiled` en suelo y muros).
- No hay namespaces y `PlayerController`/`EnemyAI`/`Health`/`CameraFollow` no se renombran (serialización de escenas y prefabs).
- Portales se disparan por trigger; ya existe gracia de 1 s en `ScenePortal` y spawn en x = 5.5 (fuera del portal de salida).

## Steps
- [x] 1. Crear `UiKit.cs` con la paleta (tinta/pergamino/oro/musgo/sangre), `Frame` con borde, `Label` con `Shadow`, `Button` con `ColorBlock`, `Bar` (Image tipo Filled) y `Place/Stretch` para anclajes. [depends: none]
- [x] 2. Reescribir `UIRoot.BuildUi()`: HUD superior-izquierda con barra de vida + chips de monedas/puntos/enemigos/fragmentos, tarjeta de objetivo abajo-izquierda, botones Inventario/Pausa arriba-derecha, avisos tipo píldora, diálogo con banda inferior y placa de nombre, y tarjetas centradas para pausa/derrota/victoria sobre fondo atenuado; conectar la barra en `UpdateHealthDisplay()`. [depends: 1]
- [x] 3. Reescribir `MainMenuUI.BuildUi()`: título con regla dorada, subtítulo, pila vertical de 4 botones con borde, tarjeta de opciones (volumen general/música/efectos con `SmallButton` ±, dificultad) y tarjeta de créditos. [depends: 1]
- [x] 4. Reescalar en `GameSceneBuilder`: `Enemy_Mole` 4 → **1**, `Boss_Guardian` 1.35 → **1** (tinte rojizo para distinguirlo), NPC 4 → **1** y Player 4 → **1** (sin esto el héroe quedaría 4× más grande que los NPC); `Enemy_Treant` se queda en **1**; reajustar colliders locales (Player/NPC/Mole `0.55×0.30` offset `(0,−0.5)`). [depends: none]
- [x] 5. Ampliar mapas a `01_Bosque 48×28`, `02_Aldea 52×30`, `03_Cuevas 46×28`, `04_Ruinas 50×30`, `05_Santuario 40×26` y reubicar en cada escena: spawn `(5.5, Height/2)`, portal de salida `(Width−1.5, Height/2)`, portal de vuelta `(1.5, Height/2)`, 8 enemigos (6 moles + 2 treants; el Santuario con jefe) y 14 coleccionables (8 monedas, 3 gemas, 2 corazones, 1 fragmento), más 6–8 `Blocks` nuevos por escena. [depends: 4]
- [x] 6. Añadir `Decorate()` al builder: por bioma, 70–90 props con `Random.InitState(hash de escena)`, escala 0.85–1.25 y flipX aleatorio, evitando spawn/portales/NPC/enemigos/objetos/`Blocks` (radio 3 u) y entre sí (0.8 u); más una franja de "camino" con sprite propio y detalles de suelo (piedras/hojas). [depends: 5]
- [x] 7. Ejecutar `AuraBuilderRunner.Execute` (regenera las 6 escenas), `check_compile_errors` limpio y verificación en Play Mode de HUD nuevo, diálogo, pausa, derrota/victoria, escalas y densidad de props por escenario; después build de Windows. [depends: 2,3,6]

## Verify
- `check_compile_errors` sin errores y consola sin excepciones al cargar cada una de las 6 escenas.
- Captura de cada bioma mostrando mapa ampliado, props en capas correctas (props bajo los personajes) y personajes del mismo tamaño que el treant.
- Play Mode 00_MainMenu → 01_Bosque: HUD con barra de vida y chips, objetivo legible, botones Inventario/Pausa funcionando, diálogo con placa de nombre, pausa/derrota/victoria con tarjetas centradas.
- Portal `01_Bosque → 02_Aldea` cambia de escena y el jugador permanece; los enemigos siguen persiguiendo sin atravesar muros ni props.
- Build de Windows en `Builds/RPG.exe` con las 6 escenas y captura del ejecutable en marcha.
