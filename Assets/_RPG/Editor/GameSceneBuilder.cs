using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Genera todo el contenido jugable: sprites auxiliares, prefabs, escenarios
/// (5 niveles + menú) y la configuración de compilación.
/// Menú: Aura / 1. Generar contenido completo
/// </summary>
public static class GameSceneBuilder
{
    const string Root = "Assets/_RPG";
    const string ArtPath = Root + "/Art";
    const string PrefabPath = Root + "/Prefabs";
    const string ScenePath = Root + "/Scenes";
    const string ResourcesPath = Root + "/Resources";

    static readonly string[] SceneNames =
    {
        "00_MainMenu", "01_Bosque", "02_Aldea", "03_Cuevas", "04_Ruinas", "05_Santuario", "06_Creditos"
    };

    [MenuItem("Aura/1. Generar contenido completo")]
    public static void BuildAll()
    {
        EnsureFolders();
        EnsureLayer("Walls");

        Sprite floor = MakeSprite("floor", 16, 16, FloorPainter);
        Sprite wall = MakeSprite("wall", 16, 16, WallPainter);
        Sprite coin = MakeSprite("coin", 12, 12, CoinPainter);
        Sprite gem = MakeSprite("gem", 12, 14, GemPainter);
        Sprite heart = MakeSprite("heart", 12, 11, HeartPainter);
        Sprite fragment = MakeSprite("fragment", 12, 12, FragmentPainter);
        Sprite bolt = MakeSprite("projectile", 8, 8, BoltPainter);
        Sprite portal = MakeSprite("portal", 16, 28, PortalPainter);

        Sprite heroSprite = LoadSprite("Assets/Sprites Externos/Tiny RPG Adventure/hero-walk/hero-walk-front.png");
        Sprite moleSprite = LoadSprite("Assets/Sprites Externos/Tiny RPG Adventure/mole/mole-idle-front.gif");
        Sprite treantSprite = LoadSprite("Assets/Sprites Externos/Tiny RPG Adventure/treant/treant-walk-front.gif");
        if (heroSprite == null) heroSprite = LoadSprite("Assets/Sprites Externos/Tiny RPG Adventure/hero-walk/hero-walk-front.gif");

        // Fotogramas completos de caminata para los aldeanos: las hojas "hero-walk"
        // ya están recortadas en frames de 32x32, así que el NPC puede animarse.
        Sprite[] npcWalkDown = LoadSprites("Assets/Sprites Externos/Tiny RPG Adventure/hero-walk/hero-walk-front.png");
        Sprite[] npcWalkUp = LoadSprites("Assets/Sprites Externos/Tiny RPG Adventure/hero-walk/hero-walk-back.png");
        Sprite[] npcWalkSide = LoadSprites("Assets/Sprites Externos/Tiny RPG Adventure/hero-walk/hero-walk-side}.png");

        GameObject projectilePrefab = BuildProjectilePrefab(bolt);
        GameObject playerPrefab = BuildPlayerPrefab(heroSprite, projectilePrefab);
        GameObject molePrefab = BuildEnemyPrefab("Enemy_Mole", moleSprite, "enemy_mole.controller", 0.85f,
            40, 4.6f, 7, 1.3f, 8f, 10f, 120, false);
        GameObject treantPrefab = BuildEnemyPrefab("Enemy_Treant", treantSprite, "enemy-treant.controller", 1.55f,
            80, 2.6f, 13, 1.6f, 7f, 10f, 280, false);
        GameObject bossPrefab = BuildEnemyPrefab("Boss_Guardian", treantSprite, "enemy-treant.controller", 2.5f,
            260, 2.8f, 18, 1.35f, 13f, 18f, 1200, true);

        // Enemigo nuevo: espora de raíz negra. Rápida, frágil y estalla al morir.
        GameObject sporePrefab = BuildEnemyPrefab("Enemy_Spore", moleSprite, "enemy_mole.controller", 0.62f,
            16, 4.8f, 5, 1.0f, 7f, 9f, 70, false);
        TuneSporePrefab(sporePrefab);
        sporePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + "/Enemy_Spore.prefab");

        // El jefe final invoca esporas durante el combate.
        AttachSummoner(bossPrefab, sporePrefab);
        bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + "/Boss_Guardian.prefab");

        GameObject collectiblePrefab = BuildCollectiblePrefab(coin);
        GameObject npcPrefab = BuildNpcPrefab(heroSprite, npcWalkDown, npcWalkUp, npcWalkSide);
        GameObject portalPrefab = BuildPortalPrefab(portal);
        GameObject bush = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sprites Externos/Cainos/Pixel Art Top Down - Basic/Prefab/Plant/PF Plant - Bush 01.prefab");

        BuildSfxLibrary();
        BuildDialogueDatabase();

        BuildMainMenu();

        BuildLevelScene(new LevelConfig
        {
            SceneName = "01_Bosque",
            Width = 48,
            Height = 28,
            FloorColor = new Color(0.22f, 0.34f, 0.18f),
            WallColor = new Color(0.16f, 0.22f, 0.13f),
            Title = "Bosque de la Arboleda",
            Story = new[]
            {
                "El sello que contenía la corrupción se ha roto en cuatro fragmentos.",
                "El bosque que te vio nacer es ahora el primero en marchitarse.",
                "Recupera el Fragmento del Bosque y busca la salida hacia la aldea."
            },
            Npcs = new[]
            {
                new NpcSpec
                {
                    Name = "Aldeano Herido", Role = "Superviviente", Position = new Vector2(7f, 20f),
                    Lines = new[]
                    {
                        "Viajero... el sello se rompió y la corrupción brotó de la tierra.",
                        "Cuatro fragmentos cayeron: bosque, aldea, cuevas y ruinas.",
                        "Toma el fragmento que brilla entre los árboles y sigue el camino al este.",
                        "Si me compras una venda con tus monedas, podré seguir caminando contigo."
                    },
                    TradeItem = "Venda de hierbas", TradePrice = 6, Effect = TradeEffect.Heal, TradePower = 40,
                    TradeDescription = "cura 40 de vida al instante"
                },
                new NpcSpec
                {
                    Name = "Leñadora Bela", Role = "Guía del bosque", Position = new Vector2(34f, 22f),
                    WanderRadius = 4.5f,
                    Lines = new[]
                    {
                        "Conozco cada claro de esta arboleda... o conocía, antes de que los troncos se torcieran.",
                        "Cuando un enemigo se acerca, corre. Yo también corro: no me da vergüenza decirlo.",
                        "Afilo las hachas desde niña. Con este filo tus golpes pesarán un 15% más, para siempre."
                    },
                    TradeItem = "Filo del bosque", TradePrice = 18, Effect = TradeEffect.Power, TradePower = 15,
                    TradeDescription = "daño de tus ataques +15% para siempre"
                }
            },
            PlayerSpawn = new Vector2(5.5f, 14f),
            Enemies = new[]
            {
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(13f, 7f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(16f, 14f) },
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(20f, 7f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(24f, 13f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(31f, 19f) },
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(36f, 8f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(41f, 22f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(45f, 16f) },
                new EnemySpec { Prefab = sporePrefab, Position = new Vector2(14f, 20f) },
                new EnemySpec { Prefab = sporePrefab, Position = new Vector2(28f, 22f) },
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(33f, 12f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(43f, 6f) }
            },
            Items = new[]
            {
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(8f, 5f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(11f, 16f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(15f, 5f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(19f, 17f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(24f, 4f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(30f, 6f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(38f, 24f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(45f, 7f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(27f, 10f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(35f, 20f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(46f, 19f) },
                new ItemSpec { Kind = CollectibleKind.Heart, Position = new Vector2(5f, 4f) },
                new ItemSpec { Kind = CollectibleKind.Heart, Position = new Vector2(29f, 15f) },
                new ItemSpec { Kind = CollectibleKind.Fragment, Position = new Vector2(17f, 11f) }
            },
            Portals = new[]
            {
                new PortalSpec { Target = "02_Aldea", Position = new Vector2(46.5f, 14f), RequiredFragments = 0 },
                new PortalSpec { Target = "00_MainMenu", Position = new Vector2(1.5f, 14f), RequiredFragments = 0 }
            },
            Blocks = new[]
            {
                new Rect(12f, 3f, 2f, 3f), new Rect(20f, 13f, 3f, 2f), new Rect(30f, 4f, 3f, 2f),
                new Rect(36f, 17f, 3f, 3f), new Rect(41f, 9f, 2f, 3f), new Rect(26f, 20f, 4f, 2f)
            }
        }, playerPrefab, collectiblePrefab, npcPrefab, portalPrefab, floor, wall, coin, gem, heart, fragment, bush);

        BuildLevelScene(new LevelConfig
        {
            SceneName = "02_Aldea",
            Width = 52,
            Height = 30,
            FloorColor = new Color(0.34f, 0.29f, 0.22f),
            WallColor = new Color(0.24f, 0.2f, 0.16f),
            Title = "Aldea de Thal",
            Story = new[]
            {
                "La aldea resiste tras empalizadas improvisadas.",
                "Los supervivientes dicen que el segundo fragmento cayó en el pozo.",
                "Ayuda a la herrera y abre el paso hacia las cuevas."
            },
            Npcs = new[]
            {
                new NpcSpec
                {
                    Name = "Herrera Mira", Role = "Herrera", Position = new Vector2(9f, 22f),
                    Lines = new[]
                    {
                        "¡Un sello roto! Por eso las sombras devoran los caminos.",
                        "El fragmento del pozo está protegido por enemigos. Lleva cuidado.",
                        "Si reúnes los cuatro, el Guardián del Santuario despertará... y podrás vencerlo.",
                        "Forjo hojas como antes forjaba rejas. Con esta, tus golpes harán un 20% más de daño para siempre."
                    },
                    TradeItem = "Filo forjado", TradePrice = 24, Effect = TradeEffect.Power, TradePower = 20,
                    TradeDescription = "daño de tus ataques +20% para siempre"
                },
                new NpcSpec
                {
                    Name = "Panadero Ordo", Role = "Aldeano", Position = new Vector2(38f, 24f),
                    WanderRadius = 5f,
                    Lines = new[]
                    {
                        "La aldea aguanta porque todos hacemos algo: yo horneo, Mira forja, los niños vigilan.",
                        "Antes vendía pan; ahora vendo vendas. Los tiempos cambian el oficio, no las manos.",
                        "Cuando llegue el Guardián, quiero tener la despensa llena. ¿Me ayudas?"
                    },
                    TradeItem = "Venda de hierbas", TradePrice = 6, Effect = TradeEffect.Heal, TradePower = 40,
                    TradeDescription = "cura 40 de vida al instante"
                }
            },
            PlayerSpawn = new Vector2(5.5f, 15f),
            Enemies = new[]
            {
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(12f, 7f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(18f, 15f) },
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(23f, 8f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(27f, 16f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(33f, 22f) },
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(38f, 10f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(43f, 24f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(47f, 17f) },
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(14f, 20f) },
                new EnemySpec { Prefab = sporePrefab, Position = new Vector2(25f, 11f) },
                new EnemySpec { Prefab = sporePrefab, Position = new Vector2(31f, 26f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(45f, 6f) }
            },
            Items = new[]
            {
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(6f, 5f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(11f, 18f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(16f, 4f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(21f, 18f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(28f, 5f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(34f, 7f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(40f, 26f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(48f, 12f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(25f, 18f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(39f, 20f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(49f, 25f) },
                new ItemSpec { Kind = CollectibleKind.Heart, Position = new Vector2(14f, 11f) },
                new ItemSpec { Kind = CollectibleKind.Heart, Position = new Vector2(31f, 19f) },
                new ItemSpec { Kind = CollectibleKind.Fragment, Position = new Vector2(19f, 12f) }
            },
            Portals = new[]
            {
                new PortalSpec { Target = "03_Cuevas", Position = new Vector2(50.5f, 15f), RequiredFragments = 0 },
                new PortalSpec { Target = "01_Bosque", Position = new Vector2(1.5f, 15f), RequiredFragments = 0 }
            },
            Blocks = new[]
            {
                new Rect(10f, 8f, 3f, 2f), new Rect(15f, 13f, 2f, 4f), new Rect(23f, 3f, 3f, 2f),
                new Rect(30f, 7f, 3f, 2f), new Rect(35f, 19f, 2f, 3f), new Rect(42f, 4f, 3f, 2f)
            }
        }, playerPrefab, collectiblePrefab, npcPrefab, portalPrefab, floor, wall, coin, gem, heart, fragment, bush);

        BuildLevelScene(new LevelConfig
        {
            SceneName = "03_Cuevas",
            Width = 46,
            Height = 28,
            FloorColor = new Color(0.19f, 0.19f, 0.24f),
            WallColor = new Color(0.12f, 0.12f, 0.16f),
            Title = "Cuevas de Raíz Negra",
            Story = new[]
            {
                "Bajo la aldea, la corrupción crece como raíces negras.",
                "El tercer fragmento late en el fondo de la cueva.",
                "El minero conoce el camino, pero los túneles están infestados."
            },
            Npcs = new[]
            {
                new NpcSpec
                {
                    Name = "Minero Torvo", Role = "Minero", Position = new Vector2(7f, 21f),
                    Lines = new[]
                    {
                        "No bajes sin luz... aunque aquí la luz ya no sirve de mucho.",
                        "Vi el fragmento entre los hongos del fondo. Algo grande lo custodia.",
                        "Sal por el túnel del este: lleva a las ruinas del santuario viejo.",
                        "Tengo un amuleto de los antiguos: si caes, el sello te levanta una vez."
                    },
                    TradeItem = "Amuleto de la segunda vida", TradePrice = 30, Effect = TradeEffect.ExtraLife, TradePower = 1,
                    TradeDescription = "revives una vez al caer en combate"
                },
                new NpcSpec
                {
                    Name = "Nara la Perdida", Role = "Niña de la aldea", Position = new Vector2(36f, 20f),
                    WanderRadius = 6f,
                    Lines = new[]
                    {
                        "Me perdí buscando gemas y ahora las paredes respiran.",
                        "Cuando veo uno de esos bichos, corro en zigzag. ¡Funciona!",
                        "Si me compras una venda, podré volver a casa sola."
                    },
                    TradeItem = "Venda de hierbas", TradePrice = 6, Effect = TradeEffect.Heal, TradePower = 40,
                    TradeDescription = "cura 40 de vida al instante"
                }
            },
            PlayerSpawn = new Vector2(5.5f, 14f),
            Enemies = new[]
            {
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(11f, 5f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(15f, 14f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(19f, 6f) },
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(23f, 10f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(29f, 7f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(34f, 19f) },
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(39f, 11f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(43f, 22f) },
                new EnemySpec { Prefab = sporePrefab, Position = new Vector2(12f, 22f) },
                new EnemySpec { Prefab = sporePrefab, Position = new Vector2(26f, 16f) },
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(31f, 12f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(41f, 5f) }
            },
            Items = new[]
            {
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(7f, 4f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(11f, 16f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(15f, 3f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(19f, 17f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(26f, 6f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(31f, 23f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(38f, 17f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(44f, 8f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(24f, 5f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(41f, 24f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(45f, 14f) },
                new ItemSpec { Kind = CollectibleKind.Heart, Position = new Vector2(13f, 9f) },
                new ItemSpec { Kind = CollectibleKind.Heart, Position = new Vector2(33f, 14f) },
                new ItemSpec { Kind = CollectibleKind.Fragment, Position = new Vector2(20f, 12f) }
            },
            Portals = new[]
            {
                new PortalSpec { Target = "04_Ruinas", Position = new Vector2(44.5f, 14f), RequiredFragments = 0 },
                new PortalSpec { Target = "02_Aldea", Position = new Vector2(1.5f, 14f), RequiredFragments = 0 }
            },
            Blocks = new[]
            {
                new Rect(8f, 9f, 3f, 2f), new Rect(14f, 4f, 2f, 3f), new Rect(19f, 6f, 3f, 2f),
                new Rect(27f, 9f, 3f, 2f), new Rect(34f, 14f, 2f, 3f), new Rect(39f, 4f, 3f, 2f)
            }
        }, playerPrefab, collectiblePrefab, npcPrefab, portalPrefab, floor, wall, coin, gem, heart, fragment, bush);

        BuildLevelScene(new LevelConfig
        {
            SceneName = "04_Ruinas",
            Width = 50,
            Height = 30,
            FloorColor = new Color(0.3f, 0.28f, 0.25f),
            WallColor = new Color(0.2f, 0.19f, 0.18f),
            Title = "Ruinas del Santuario",
            Story = new[]
            {
                "Las ruinas del santuario antiguo se alzan entre la niebla.",
                "El último fragmento está aquí, pero la puerta al Santuario exige los cuatro.",
                "El erudito sabe cómo abrir el sello."
            },
            Npcs = new[]
            {
                new NpcSpec
                {
                    Name = "Erudito Vahl", Role = "Erudito", Position = new Vector2(8f, 22f),
                    Lines = new[]
                    {
                        "Este era el santuario donde se forjó el sello, hace cien inviernos.",
                        "Con los cuatro fragmentos la puerta del este se abrirá.",
                        "Dentro espera el Guardián. No es malvado: está corrompido. Libéralo.",
                        "Y escucha: cuando lo enfrentes, invocará esporas. Esquiva con Shift."
                    },
                    TradeItem = "Bendición del sello", TradePrice = 20, Effect = TradeEffect.Vitality, TradePower = 15,
                    TradeDescription = "+15 de vida máxima y cura 15"
                },
                new NpcSpec
                {
                    Name = "Novicio Ivo", Role = "Guardián novicio", Position = new Vector2(40f, 10f),
                    WanderRadius = 4f,
                    Lines = new[]
                    {
                        "Me quedé a cuidar las ruinas. Alguien tenía que hacerlo.",
                        "Los muros repiten lo que escuchan: si gritas, la corrupción te encuentra.",
                        "Te vendo un escudo de corteza. No dejes que el Guardián te toque."
                    },
                    TradeItem = "Escudo de corteza", TradePrice = 14, Effect = TradeEffect.Shield, TradePower = 30,
                    TradeDescription = "absorbe 30 de daño antes de tu vida"
                }
            },
            PlayerSpawn = new Vector2(5.5f, 15f),
            Enemies = new[]
            {
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(12f, 8f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(16f, 17f) },
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(21f, 9f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(25f, 17f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(31f, 22f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(36f, 8f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(42f, 24f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(46f, 12f) },
                new EnemySpec { Prefab = sporePrefab, Position = new Vector2(13f, 22f) },
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(27f, 11f) },
                new EnemySpec { Prefab = sporePrefab, Position = new Vector2(35f, 26f) },
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(43f, 5f) }
            },
            Items = new[]
            {
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(6f, 5f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(10f, 18f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(14f, 4f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(18f, 18f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(24f, 5f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(30f, 8f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(37f, 25f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(46f, 27f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(28f, 13f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(44f, 19f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(48f, 7f) },
                new ItemSpec { Kind = CollectibleKind.Heart, Position = new Vector2(13f, 13f) },
                new ItemSpec { Kind = CollectibleKind.Heart, Position = new Vector2(33f, 21f) },
                new ItemSpec { Kind = CollectibleKind.Fragment, Position = new Vector2(19f, 11f) }
            },
            Portals = new[]
            {
                new PortalSpec { Target = "05_Santuario", Position = new Vector2(48.5f, 15f), RequiredFragments = 4 },
                new PortalSpec { Target = "03_Cuevas", Position = new Vector2(1.5f, 15f), RequiredFragments = 0 }
            },
            Blocks = new[]
            {
                new Rect(9f, 9f, 3f, 3f), new Rect(16f, 5f, 2f, 3f), new Rect(21f, 12f, 3f, 3f),
                new Rect(30f, 8f, 3f, 3f), new Rect(37f, 18f, 2f, 3f), new Rect(44f, 5f, 3f, 2f)
            }
        }, playerPrefab, collectiblePrefab, npcPrefab, portalPrefab, floor, wall, coin, gem, heart, fragment, bush);

        BuildLevelScene(new LevelConfig
        {
            SceneName = "05_Santuario",
            Width = 40,
            Height = 26,
            FloorColor = new Color(0.14f, 0.22f, 0.2f),
            WallColor = new Color(0.09f, 0.14f, 0.13f),
            Title = "Santuario del Sello",
            Story = new[]
            {
                "Los cuatro fragmentos vibran en tu mochila.",
                "El Guardián del Sello te espera, consumido por la corrupción.",
                "Derrótalo para restaurar el sello y salvar la Arboleda."
            },
            Npcs = new[]
            {
                new NpcSpec
                {
                    Name = "Espíritu del Sello", Role = "Guardián antiguo", Position = new Vector2(6f, 21f),
                    Lines = new[]
                    {
                        "Has traído los cuatro fragmentos. El santuario te reconoce.",
                        "El Guardián ya no distingue amigos de enemigos: la corrupción lo habita.",
                        "Libéralo y el sello volverá a cerrarse. Todo depende de ti.",
                        "Toma esta bendición: si el Guardián te derriba, el sello te levantará una vez más."
                    },
                    TradeItem = "Sello de reserva", TradePrice = 25, Effect = TradeEffect.ExtraLife, TradePower = 1,
                    TradeDescription = "revives una vez al caer en combate"
                },
                new NpcSpec
                {
                    Name = "Eco de la Arboleda", Role = "Memoria del bosque", Position = new Vector2(30f, 22f),
                    WanderRadius = 5f,
                    Lines = new[]
                    {
                        "Soy lo que el bosque recuerda de sí mismo.",
                        "Cada aldeano que escuchaste dejó una semilla en esta sala.",
                        "Cuando el Guardián invoque sus esporas, muévete. El valor también es saber correr."
                    },
                    TradeItem = "Venda de hierbas", TradePrice = 6, Effect = TradeEffect.Heal, TradePower = 40,
                    TradeDescription = "cura 40 de vida al instante"
                }
            },
            PlayerSpawn = new Vector2(5.5f, 13f),
            Enemies = new[]
            {
                new EnemySpec { Prefab = bossPrefab, Position = new Vector2(32f, 13f), Scale = 1f },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(14f, 6f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(15f, 16f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(12f, 23f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(21f, 7f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(28f, 21f) },
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(26f, 14f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(37f, 22f) },
                new EnemySpec { Prefab = sporePrefab, Position = new Vector2(18f, 9f) },
                new EnemySpec { Prefab = sporePrefab, Position = new Vector2(24f, 23f) },
                new EnemySpec { Prefab = treantPrefab, Position = new Vector2(34f, 8f) },
                new EnemySpec { Prefab = molePrefab, Position = new Vector2(36f, 17f) }
            },
            Items = new[]
            {
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(8f, 5f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(10f, 19f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(13f, 10f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(22f, 5f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(31f, 9f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(37f, 24f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(20f, 24f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(35f, 5f) },
                new ItemSpec { Kind = CollectibleKind.Coin, Position = new Vector2(16f, 22f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(25f, 4f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(38f, 17f) },
                new ItemSpec { Kind = CollectibleKind.Gem, Position = new Vector2(26f, 19f) },
                new ItemSpec { Kind = CollectibleKind.Heart, Position = new Vector2(7f, 11f) },
                new ItemSpec { Kind = CollectibleKind.Heart, Position = new Vector2(30f, 17f) }
            },
            Portals = new[]
            {
                new PortalSpec { Target = "04_Ruinas", Position = new Vector2(1.5f, 13f), RequiredFragments = 0 }
            },
            Blocks = new[]
            {
                new Rect(11f, 9f, 2f, 2f), new Rect(17f, 14f, 3f, 2f), new Rect(24f, 8f, 3f, 2f),
                new Rect(29f, 18f, 3f, 2f), new Rect(35f, 4f, 2f, 3f)
            }
        }, playerPrefab, collectiblePrefab, npcPrefab, portalPrefab, floor, wall, coin, gem, heart, fragment, bush);

        BuildCreditsScene();

        ConfigureBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ScenePath + "/00_MainMenu.unity");
        Debug.Log("[Aura] Contenido generado: 5 escenarios + menú + créditos, prefabs, sprites y build settings.");
    }

    // ---------------- Configuración ----------------

    class EnemySpec
    {
        public GameObject Prefab;
        public Vector2 Position;
        public float Scale = 1f;
    }

    class ItemSpec
    {
        public CollectibleKind Kind;
        public Vector2 Position;
    }

    class PortalSpec
    {
        public string Target;
        public Vector2 Position;
        public int RequiredFragments;
    }

    class NpcSpec
    {
        public string Name;
        public string Role = "Habitante";
        public string[] Lines;
        public Vector2 Position;
        public float WanderRadius = 3.5f;

        public string TradeItem;
        public string TradeDescription;
        public int TradePrice;
        public TradeEffect Effect = TradeEffect.Heal;
        public int TradePower = 40;
    }

    class LevelConfig
    {
        public string SceneName;
        public int Width;
        public int Height;
        public Color FloorColor;
        public Color WallColor;
        public string Title;
        public string[] Story;
        public NpcSpec[] Npcs;
        public Vector2 PlayerSpawn;
        public EnemySpec[] Enemies;
        public ItemSpec[] Items;
        public PortalSpec[] Portals;
        public Rect[] Blocks;
    }

    // ---------------- Construcción de escenarios ----------------

    static void BuildLevelScene(LevelConfig cfg, GameObject playerPrefab, GameObject collectiblePrefab,
        GameObject npcPrefab, GameObject portalPrefab, Sprite floor, Sprite wall, Sprite coin, Sprite gem,
        Sprite heart, Sprite fragment, GameObject bushPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Suelo
        GameObject floorGo = new GameObject("Floor");
        SpriteRenderer floorSr = floorGo.AddComponent<SpriteRenderer>();
        floorSr.sprite = floor;
        floorSr.color = cfg.FloorColor;
        floorSr.drawMode = SpriteDrawMode.Tiled;
        floorSr.size = new Vector2(cfg.Width, cfg.Height);
        floorSr.sortingOrder = -20;
        floorGo.transform.position = new Vector3(cfg.Width / 2f, cfg.Height / 2f, 0f);

        // Muros perimetrales
        GameObject wallsRoot = new GameObject("Walls");
        CreateWall(wallsRoot.transform, "Wall_South", new Vector2(cfg.Width / 2f, -0.5f), new Vector2(cfg.Width + 2f, 1f), wall, cfg.WallColor);
        CreateWall(wallsRoot.transform, "Wall_North", new Vector2(cfg.Width / 2f, cfg.Height + 0.5f), new Vector2(cfg.Width + 2f, 1f), wall, cfg.WallColor);
        CreateWall(wallsRoot.transform, "Wall_West", new Vector2(-0.5f, cfg.Height / 2f), new Vector2(1f, cfg.Height + 2f), wall, cfg.WallColor);
        CreateWall(wallsRoot.transform, "Wall_East", new Vector2(cfg.Width + 0.5f, cfg.Height / 2f), new Vector2(1f, cfg.Height + 2f), wall, cfg.WallColor);

        if (cfg.Blocks != null)
        {
            for (int i = 0; i < cfg.Blocks.Length; i++)
            {
                Rect r = cfg.Blocks[i];
                CreateWall(wallsRoot.transform, "Obstacle_" + i, new Vector2(r.x + r.width / 2f, r.y + r.height / 2f), new Vector2(r.width, r.height), wall, cfg.WallColor * 0.9f);
            }
        }

        // Límites de cámara
        GameObject limits = new GameObject("LimitesCamara");
        limits.transform.position = new Vector3(cfg.Width / 2f, cfg.Height / 2f, 0f);
        BoxCollider2D limitsCol = limits.AddComponent<BoxCollider2D>();
        limitsCol.isTrigger = true;
        limitsCol.size = new Vector2(cfg.Width - 1f, cfg.Height - 1f);

        // Luz global 2D (necesaria para que los sprites URP 2D se vean)
        GameObject lightGo = new GameObject("Global Light 2D");
        Light2D light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = Color.white;
        light.intensity = 1f;

        // Jugador
        GameObject playerGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        playerGo.transform.position = cfg.PlayerSpawn;
        playerGo.name = "Player";

        // Cámara
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = cfg.WallColor * 0.5f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(cfg.PlayerSpawn.x, cfg.PlayerSpawn.y, -10f);
        CameraFollow follow = camGo.AddComponent<CameraFollow>();
        follow.target = playerGo.transform;
        follow.bounds = limitsCol;
        follow.offset = new Vector3(0f, 0f, -10f);

        // Enemigos
        GameObject enemiesRoot = new GameObject("Enemies");
        if (cfg.Enemies != null)
        {
            for (int i = 0; i < cfg.Enemies.Length; i++)
            {
                EnemySpec spec = cfg.Enemies[i];
                GameObject enemyGo = (GameObject)PrefabUtility.InstantiatePrefab(spec.Prefab);
                enemyGo.transform.SetParent(enemiesRoot.transform);
                enemyGo.transform.position = spec.Position;
                enemyGo.name = spec.Prefab.name + "_" + i;
                if (Mathf.Abs(spec.Scale - 1f) > 0.001f) enemyGo.transform.localScale = Vector3.one * spec.Scale;
                EnemyAI ai = enemyGo.GetComponent<EnemyAI>();
                if (ai != null) ai.player = playerGo.transform;
            }
        }

        // Coleccionables
        GameObject itemsRoot = new GameObject("Collectibles");
        if (cfg.Items != null)
        {
            for (int i = 0; i < cfg.Items.Length; i++)
            {
                ItemSpec spec = cfg.Items[i];
                GameObject itemGo = (GameObject)PrefabUtility.InstantiatePrefab(collectiblePrefab);
                itemGo.transform.SetParent(itemsRoot.transform);
                itemGo.transform.position = spec.Position;
                itemGo.name = spec.Kind + "_" + i;
                Collectible col = itemGo.GetComponent<Collectible>();
                SpriteRenderer sr = itemGo.GetComponent<SpriteRenderer>();
                col.kind = spec.Kind;
                if (sr != null)
                {
                    if (spec.Kind == CollectibleKind.Coin) { sr.sprite = coin; sr.color = new Color(1f, 0.85f, 0.2f); }
                    else if (spec.Kind == CollectibleKind.Gem) { sr.sprite = gem; sr.color = new Color(0.4f, 0.9f, 1f); }
                    else if (spec.Kind == CollectibleKind.Heart) { sr.sprite = heart; sr.color = new Color(1f, 0.4f, 0.45f); }
                    else { sr.sprite = fragment; sr.color = new Color(0.55f, 1f, 0.6f); }
                }
            }
        }

        // NPCs: dos por escenario. Caminan, huyen de los enemigos y comercian.
        GameObject npcsRoot = new GameObject("NPCs");
        if (npcPrefab != null && cfg.Npcs != null)
        {
            for (int i = 0; i < cfg.Npcs.Length; i++)
            {
                NpcSpec spec = cfg.Npcs[i];
                GameObject npcGo = (GameObject)PrefabUtility.InstantiatePrefab(npcPrefab);
                npcGo.transform.SetParent(npcsRoot.transform);
                npcGo.transform.position = spec.Position;
                npcGo.name = "NPC_" + spec.Name.Replace(" ", "");

                NpcInteractable npc = npcGo.GetComponent<NpcInteractable>();
                if (npc != null)
                {
                    npc.npcName = spec.Name;
                    npc.role = spec.Role;
                    npc.lines = spec.Lines;
                    npc.tradeItemName = spec.TradeItem;
                    npc.tradeDescription = spec.TradeDescription;
                    npc.tradePrice = spec.TradePrice;
                    npc.tradeEffect = spec.Effect;
                    npc.tradePower = spec.TradePower;
                }

                NpcWanderer wanderer = npcGo.GetComponent<NpcWanderer>();
                if (wanderer != null) wanderer.wanderRadius = spec.WanderRadius;

                SpriteRenderer sr = npcGo.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = i == 0 ? new Color(0.72f, 0.86f, 1f) : new Color(0.98f, 0.86f, 0.72f);
            }
        }

        // Portales
        GameObject portalsRoot = new GameObject("Portals");
        if (cfg.Portals != null)
        {
            for (int i = 0; i < cfg.Portals.Length; i++)
            {
                PortalSpec spec = cfg.Portals[i];
                GameObject portalGo = (GameObject)PrefabUtility.InstantiatePrefab(portalPrefab);
                portalGo.transform.SetParent(portalsRoot.transform);
                portalGo.transform.position = spec.Position;
                portalGo.name = "Portal_to_" + spec.Target;
                ScenePortal sp = portalGo.GetComponent<ScenePortal>();
                sp.targetScene = spec.Target;
                sp.requiredFragments = spec.RequiredFragments;
                sp.label = spec.Target;

                // Etiqueta del portal
                GameObject labelGo = new GameObject("Label");
                labelGo.transform.SetParent(portalGo.transform, false);
                labelGo.transform.position = (Vector3)spec.Position + new Vector3(0f, 1.9f, 0f);
                TextMesh tm = labelGo.AddComponent<TextMesh>();
                tm.text = PortalLabel(spec.Target) + (spec.RequiredFragments > 0 ? "  (" + spec.RequiredFragments + " fragmentos)" : "");
                tm.fontSize = 48;
                tm.characterSize = 0.06f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = new Color(0.8f, 1f, 0.9f);
            }
        }

        // Decoración densa: props del pack Cainos repartidos por toda la superficie
        DecorateScene(cfg, floor);

        EditorSceneManager.SaveScene(scene, ScenePath + "/" + cfg.SceneName + ".unity");
    }

    // ---------------- Decoración ----------------

    struct SpriteMetrics
    {
        public float Width;
        public float Height;
    }

    /// <summary>Tamaño del sprite en unidades de mundo con el transform a escala 1.</summary>
    static SpriteMetrics MeasureSprite(Sprite sprite)
    {
        SpriteMetrics m = new SpriteMetrics { Width = 1f, Height = 1f };
        if (sprite == null) return m;
        m.Width = Mathf.Max(0.05f, sprite.bounds.size.x);
        m.Height = Mathf.Max(0.05f, sprite.bounds.size.y);
        return m;
    }

    /// <summary>
    /// Reparte props del pack Cainos por todo el mapa (la densidad crece con el área),
    /// traza el sendero principal y respeta portales, NPC, jugador, enemigos, objetos y obstáculos.
    /// </summary>
    static void DecorateScene(LevelConfig cfg, Sprite floorSprite)
    {
        Random.InitState(cfg.SceneName.GetHashCode());

        GameObject decorRoot = new GameObject("Decor");
        float midY = cfg.Height / 2f;

        BiomeDecor spec = DecorSpec(cfg.SceneName);

        // Sendero propio de cada ámbito (las cuevas no tienen camino).
        if (floorSprite != null && spec.PathWidth > 0f)
        {
            GameObject road = new GameObject("Path");
            road.transform.SetParent(decorRoot.transform);
            SpriteRenderer roadSr = road.AddComponent<SpriteRenderer>();
            roadSr.sprite = floorSprite;
            roadSr.color = spec.PathColor;
            roadSr.drawMode = SpriteDrawMode.Tiled;
            roadSr.size = new Vector2(cfg.Width - 5f, spec.PathWidth);
            roadSr.sortingOrder = -18;
            road.transform.position = new Vector3(cfg.Width / 2f, midY, 0f);
        }

        string[] propPaths = DecorPrefabsByKeywords(spec.Props);
        if (propPaths.Length == 0)
        {
            Debug.LogWarning("[Aura] Sin props de decoración para " + cfg.SceneName);
            return;
        }

        string[] perimeterPaths = spec.Perimeter != null && spec.Perimeter.Length > 0
            ? DecorPrefabsByKeywords(spec.Perimeter)
            : null;
        string[] scatterPaths = spec.Scatter != null && spec.Scatter.Length > 0
            ? DecorPrefabsByKeywords(spec.Scatter)
            : null;

        List<Vector2> reserved = new List<Vector2>();
        reserved.Add(cfg.PlayerSpawn);
        if (cfg.Npcs != null)
            for (int i = 0; i < cfg.Npcs.Length; i++) reserved.Add(cfg.Npcs[i].Position);
        reserved.Add(new Vector2(1.5f, midY));
        reserved.Add(new Vector2(cfg.Width - 1.5f, midY));
        if (cfg.Enemies != null)
            for (int i = 0; i < cfg.Enemies.Length; i++) reserved.Add(cfg.Enemies[i].Position);
        if (cfg.Items != null)
            for (int i = 0; i < cfg.Items.Length; i++) reserved.Add(cfg.Items[i].Position);

        // 1) Sombra del dosel: profundidad bajo los props (densidad y color por bioma).
        AddCanopyShadows(cfg, floorSprite, decorRoot.transform, spec.CanopyDivisor, spec.CanopyTint);

        // 2) Perímetro: cada ámbito cierra el mapa con SU material (árboles, pilares, faroles).
        if (perimeterPaths != null)
            AddPerimeterDressing(cfg, perimeterPaths, reserved, decorRoot.transform, spec.PerimeterStep, spec.PerimeterSecondRing);

        List<Vector2> placed = new List<Vector2>();
        List<Vector2> clusterCenters = new List<Vector2>();
        int clusters = Mathf.Clamp(Mathf.RoundToInt(cfg.Width * cfg.Height / 22f), 40, 90);
        int attempts = 0;
        int clusterCount = 0;

        while (clusterCount < clusters && attempts < clusters * 30)
        {
            attempts++;
            Vector2 center = new Vector2(Random.Range(2.5f, cfg.Width - 2.5f), Random.Range(2.5f, cfg.Height - 2.5f));

            // Cuevas pegan los claros a la pared; la aldea los agrupa contra los muros.
            center = PushToEdge(center, cfg, spec.EdgeBias);

            if (IsReserved(center, reserved, 2.4f)) continue;
            if (IsBlocked(center, cfg.Blocks, 1.6f)) continue;
            // Separación entre claros (no contra cada prop suelto: eso agotaba el mapa
            // a los pocos claros y dejaba el bosque casi vacío).
            if (IsReserved(center, clusterCenters, 2.3f)) continue;
            clusterCenters.Add(center);

            // Cada claro se compone con la forma propia del bioma: mata, retícula, anillo o columnata.
            int members = Random.Range(spec.MembersMin, spec.MembersMax + 1);
            float extent = Random.Range(spec.SpreadMin, spec.SpreadMax);
            float axis = Random.value * Mathf.PI * 2f;
            float phase = Random.value * Mathf.PI * 2f;

            for (int m = 0; m < members; m++)
            {
                Vector2 point = center;
                if (m > 0)
                {
                    point = ClusterPoint(center, spec, m, members, extent, axis, phase);
                    if (point.x < 1.5f || point.y < 1.5f || point.x > cfg.Width - 1.5f || point.y > cfg.Height - 1.5f) continue;
                    if (IsReserved(point, reserved, 1.8f)) continue;
                    if (IsBlocked(point, cfg.Blocks, 1.0f)) continue;
                }

                if (!PlaceProp(propPaths, point, decorRoot.transform)) continue;
                placed.Add(point);
            }
            clusterCount++;
        }

        // 3) Relleno suelto: hierba en el bosque, escombro en las ruinas, guijarro en la cueva.
        AddScatter(cfg, scatterPaths, reserved, decorRoot.transform,
            spec.ScatterDivisor, spec.ScatterMinScale, spec.ScatterMaxScale);

        // 4) Fauna luminosa: solo donde tiene sentido (divisor 0 = ninguna).
        AddFireflies(cfg, decorRoot.transform, spec.FireflyDivisor, spec.FireflyTint);

        Debug.Log("[Aura] " + cfg.SceneName + ": " + placed.Count + " props en " + clusterCount +
                  " claros sobre " + cfg.Width + "x" + cfg.Height);
    }

    /// <summary>Acerca el claro al borde más próximo según lo pida el bioma.</summary>
    static Vector2 PushToEdge(Vector2 center, LevelConfig cfg, float bias)
    {
        if (bias <= 0f || Random.value > bias) return center;

        float toX = Mathf.Min(center.x, cfg.Width - center.x);
        float toY = Mathf.Min(center.y, cfg.Height - center.y);
        float t = Random.Range(0.45f, 0.92f);

        if (toX < toY)
            center.x = Mathf.Lerp(center.x, center.x < cfg.Width * 0.5f ? 2.5f : cfg.Width - 2.5f, t);
        else
            center.y = Mathf.Lerp(center.y, center.y < cfg.Height * 0.5f ? 2.5f : cfg.Height - 2.5f, t);

        return center;
    }

    /// <summary>Posición del miembro m del claro, según la forma propia del bioma.</summary>
    static Vector2 ClusterPoint(Vector2 center, BiomeDecor spec, int m, int members,
                                float extent, float axis, float phase)
    {
        switch (spec.Shape)
        {
            // Ordenación recta: columnata rota, puestos de plaza, ringlera de altares.
            case DecorShape.Row:
            {
                float t = (m / (float)Mathf.Max(1, members - 1) - 0.5f) * extent;
                Vector2 dir = new Vector2(Mathf.Cos(axis), Mathf.Sin(axis));
                Vector2 perp = new Vector2(-dir.y, dir.x);
                return center + dir * t + perp * Random.Range(-0.4f, 0.4f);
            }

            // Anillo: corro ceremonial alrededor de un punto sagrado.
            case DecorShape.Ring:
            {
                float a = phase + Mathf.PI * 2f * m / Mathf.Max(1, members - 1);
                float r = Random.Range(spec.SpreadMin, spec.SpreadMax);
                return center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }

            // Retícula: corral ordenado de enseres, como un patio de aldea.
            case DecorShape.Grid:
            {
                int cols = Mathf.Max(2, Mathf.CeilToInt(Mathf.Sqrt(members)));
                int col = m % cols;
                int row = m / cols;
                float spacing = extent / Mathf.Max(1, cols - 1);
                return center + new Vector2((col - (cols - 1) * 0.5f) * spacing,
                                            (row - (cols - 1) * 0.5f) * spacing);
            }

            // Mata compacta de vegetación o roca.
            default:
            {
                float a = Random.value * Mathf.PI * 2f;
                float r = Random.Range(spec.SpreadMin, spec.SpreadMax);
                return center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
        }
    }

    /// <summary>Manchas oscuras de copa: simulan la sombra de árboles que no se ven.</summary>
    static void AddCanopyShadows(LevelConfig cfg, Sprite floorSprite, Transform parent, float divisor, Color canopyTint)
    {
        if (floorSprite == null || divisor <= 0f) return;

        GameObject root = new GameObject("Dosel");
        root.transform.SetParent(parent);

        int count = Mathf.Clamp(Mathf.RoundToInt(cfg.Width * cfg.Height / divisor), 30, 160);
        for (int i = 0; i < count; i++)
        {
            GameObject patch = new GameObject("Sombra_" + i);
            patch.transform.SetParent(root.transform);

            SpriteRenderer sr = patch.AddComponent<SpriteRenderer>();
            sr.sprite = floorSprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(Random.Range(3.5f, 11f), Random.Range(2.5f, 7f));
            sr.color = new Color(canopyTint.r, canopyTint.g, canopyTint.b, Random.Range(0.10f, 0.26f));
            sr.sortingOrder = -19;

            patch.transform.position = new Vector3(Random.Range(1f, cfg.Width - 1f), Random.Range(1f, cfg.Height - 1f), 0f);
        }
    }

    /// <summary>Cierra el mapa con el material propio del bioma: árboles, pilares, faroles...</summary>
    static void AddPerimeterDressing(LevelConfig cfg, string[] treePaths, List<Vector2> reserved,
                                     Transform parent, float step, bool secondRing)
    {
        if (treePaths == null || treePaths.Length == 0) return;

        GameObject root = new GameObject("Cortina");
        root.transform.SetParent(parent);

        for (float x = 1.8f; x < cfg.Width - 1.2f; x += step)
        {
            TryPerimeterTree(treePaths, new Vector2(x + Random.Range(-0.45f, 0.45f), Random.Range(1.3f, 2.6f)), reserved, root.transform);
            TryPerimeterTree(treePaths, new Vector2(x + Random.Range(-0.45f, 0.45f), cfg.Height - Random.Range(1.3f, 2.6f)), reserved, root.transform);
        }
        for (float y = 3.2f; y < cfg.Height - 2.8f; y += step)
        {
            TryPerimeterTree(treePaths, new Vector2(Random.Range(1.3f, 2.7f), y), reserved, root.transform);
            TryPerimeterTree(treePaths, new Vector2(cfg.Width - Random.Range(1.3f, 2.7f), y), reserved, root.transform);
        }

        // Segunda cortina interior: cierra el mapa y evita el efecto "caja vacía".
        if (!secondRing) return;

        for (float x = 2.6f; x < cfg.Width - 2f; x += 2.4f)
        {
            if (Random.value < 0.68f)
                TryPerimeterTree(treePaths, new Vector2(x + Random.Range(-0.6f, 0.6f), Random.Range(3.1f, 4.6f)), reserved, root.transform);
            if (Random.value < 0.68f)
                TryPerimeterTree(treePaths, new Vector2(x + Random.Range(-0.6f, 0.6f), cfg.Height - Random.Range(3.1f, 4.6f)), reserved, root.transform);
        }
        for (float y = 4.6f; y < cfg.Height - 4f; y += 3.1f)
        {
            if (Random.value < 0.55f)
                TryPerimeterTree(treePaths, new Vector2(Random.Range(3.2f, 4.6f), y), reserved, root.transform);
            if (Random.value < 0.55f)
                TryPerimeterTree(treePaths, new Vector2(cfg.Width - Random.Range(3.2f, 4.6f), y), reserved, root.transform);
        }
    }

    static void TryPerimeterTree(string[] treePaths, Vector2 point, List<Vector2> reserved, Transform parent)
    {
        // Nunca tapar portales, spawn ni aldeanos: son los pasos obligatorios del mapa.
        if (IsReserved(point, reserved, 2.4f)) return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(treePaths[Random.Range(0, treePaths.Length)]);
        if (prefab == null) return;

        GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        tree.transform.SetParent(parent);
        tree.transform.position = new Vector3(point.x, point.y, 0f);
        tree.transform.localScale = Vector3.one * Random.Range(0.85f, 1.15f);

        if (Random.value < 0.5f)
        {
            SpriteRenderer[] rends = tree.GetComponentsInChildren<SpriteRenderer>();
            for (int r = 0; r < rends.Length; r++) rends[r].flipX = true;
        }

        Collider2D[] colliders = tree.GetComponentsInChildren<Collider2D>();
        for (int c = 0; c < colliders.Length; c++) Object.DestroyImmediate(colliders[c]);
    }

    /// <summary>Relleno suelto del bioma: hierba en el bosque, adoquín en la aldea, guijarro en la cueva.</summary>
    static void AddScatter(LevelConfig cfg, string[] propPaths, List<Vector2> reserved, Transform parent,
                           float divisor, float minScale, float maxScale)
    {
        if (propPaths == null || propPaths.Length == 0) return;
        if (divisor <= 0f) return;

        GameObject root = new GameObject("Maleza");
        root.transform.SetParent(parent);

        int target = Mathf.Clamp(Mathf.RoundToInt(cfg.Width * cfg.Height / divisor), 90, 300);
        int done = 0;
        int attempts = 0;

        while (done < target && attempts < target * 8)
        {
            attempts++;
            Vector2 point = new Vector2(Random.Range(2f, cfg.Width - 2f), Random.Range(2f, cfg.Height - 2f));
            if (IsReserved(point, reserved, 1.4f)) continue;
            if (IsBlocked(point, cfg.Blocks, 0.4f)) continue;
            if (!PlaceProp(propPaths, point, root.transform, minScale, maxScale)) continue;
            done++;
        }
    }

    /// <summary>Fauna luminosa propia del bioma (divisor 0 = esta escena no lleva ninguna).</summary>
    static void AddFireflies(LevelConfig cfg, Transform parent, float divisor, Color tint)
    {
        if (divisor <= 0f) return;

        Sprite dot = FireflySprite();
        if (dot == null) return;

        GameObject root = new GameObject("Luciernagas");
        root.transform.SetParent(parent);

        int count = Mathf.Clamp(Mathf.RoundToInt(cfg.Width * cfg.Height / divisor), 30, 90);
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Luciernaga_" + i);
            go.transform.SetParent(root.transform);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dot;
            sr.sortingOrder = 6;
            sr.color = new Color(tint.r, tint.g, tint.b, 0.8f);

            go.transform.position = new Vector3(Random.Range(2f, cfg.Width - 2f), Random.Range(2f, cfg.Height - 2f), 0f);
            go.transform.localScale = Vector3.one * Random.Range(0.06f, 0.16f);

            Firefly firefly = go.AddComponent<Firefly>();
            firefly.radius = Random.Range(0.6f, 1.8f);
            firefly.speed = Random.Range(0.35f, 0.8f);
        }
    }

    /// <summary>Instancia un prop aleatorio en el punto dado y le quita los colliders.</summary>
    static bool PlaceProp(string[] propPaths, Vector2 point, Transform parent, float minScale = 0.8f, float maxScale = 1.25f)
    {
        if (propPaths == null || propPaths.Length == 0) return false;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(propPaths[Random.Range(0, propPaths.Length)]);
        if (prefab == null) return false;

        GameObject prop = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        prop.transform.SetParent(parent);
        prop.transform.position = new Vector3(point.x, point.y, 0f);
        if (Random.value < 0.5f) prop.transform.localScale = Vector3.one * Random.Range(minScale, maxScale);
        if (Random.value < 0.5f)
        {
            SpriteRenderer[] rends = prop.GetComponentsInChildren<SpriteRenderer>();
            for (int r = 0; r < rends.Length; r++) rends[r].flipX = true;
        }

        // La decoración nunca debe estorbar el paso ni bloquear la IA.
        Collider2D[] colliders = prop.GetComponentsInChildren<Collider2D>();
        for (int c = 0; c < colliders.Length; c++) Object.DestroyImmediate(colliders[c]);

        return true;
    }

    /// <summary>Forma en que se compone cada claro: es lo que hace que dos biomas no se lean igual.</summary>
    enum DecorShape
    {
        Blob,
        Grid,
        Ring,
        Row
    }

    /// <summary>Identidad visual de un ámbito. Ningún término se comparte entre biomas.</summary>
    class BiomeDecor
    {
        public string[] Props;
        public string[] Scatter;
        public float ScatterDivisor = 3f;
        public float ScatterMinScale = 0.5f;
        public float ScatterMaxScale = 1.15f;
        public string[] Perimeter;
        public float PerimeterStep = 2.5f;
        public bool PerimeterSecondRing;
        public float CanopyDivisor = 10f;
        public Color CanopyTint = new Color(0.05f, 0.07f, 0.06f);
        public float FireflyDivisor;
        public Color FireflyTint = new Color(1f, 0.95f, 0.62f);

        // Composición del claro.
        public DecorShape Shape = DecorShape.Blob;
        public int MembersMin = 4;
        public int MembersMax = 8;
        public float SpreadMin = 0.8f;
        public float SpreadMax = 3f;
        public float EdgeBias;

        // Sendero (0 = este ámbito no tiene camino).
        public float PathWidth = 3f;
        public Color PathColor = new Color(0.92f, 0.88f, 0.74f, 0.42f);
    }

    static BiomeDecor DecorSpec(string sceneName)
    {
        switch (sceneName)
        {
            // Bosque: matas de vegetación densas y redondas, sendero de tierra, luciérnagas.
            case "01_Bosque":
                return new BiomeDecor
                {
                    Props = new[] { "bush", "grass", "flower", "tree" },
                    Scatter = new[] { "grass" },
                    ScatterDivisor = 6f,
                    ScatterMinScale = 0.5f,
                    ScatterMaxScale = 1.15f,
                    Perimeter = new[] { "tree" },
                    PerimeterStep = 3f,
                    PerimeterSecondRing = true,
                    CanopyDivisor = 12f,
                    CanopyTint = new Color(0.03f, 0.09f, 0.05f),
                    FireflyDivisor = 16f,
                    FireflyTint = new Color(1f, 0.95f, 0.62f),
                    Shape = DecorShape.Blob,
                    MembersMin = 5,
                    MembersMax = 11,
                    SpreadMin = 0.8f,
                    SpreadMax = 3.1f,
                    EdgeBias = 0.15f,
                    PathWidth = 3.2f,
                    PathColor = new Color(0.93f, 0.86f, 0.66f, 0.40f)
                };

            // Aldea: patios ordenados contra los muros, calzada ancha, sin luciérnagas.
            case "02_Aldea":
                return new BiomeDecor
                {
                    Props = new[]
                    {
                        "barrel", "crate", "pot", "well", "wooden gate", "road sign",
                        "stone bench", "chest", "stone lantern"
                    },
                    Scatter = new[] { "brick" },
                    ScatterDivisor = 9f,
                    ScatterMinScale = 0.45f,
                    ScatterMaxScale = 0.8f,
                    Perimeter = new[] { "stone lantern", "wooden gate" },
                    PerimeterStep = 6.4f,
                    PerimeterSecondRing = false,
                    CanopyDivisor = 28f,
                    CanopyTint = new Color(0.10f, 0.08f, 0.05f),
                    FireflyDivisor = 0f,
                    FireflyTint = new Color(1f, 0.95f, 0.62f),
                    Shape = DecorShape.Grid,
                    MembersMin = 5,
                    MembersMax = 9,
                    SpreadMin = 2f,
                    SpreadMax = 3.4f,
                    EdgeBias = 0.45f,
                    PathWidth = 5f,
                    PathColor = new Color(0.72f, 0.70f, 0.68f, 0.55f)
                };

            // Cuevas: roca y sepulcros pegados a la pared, sin camino, luciérnagas frías.
            case "03_Cuevas":
                return new BiomeDecor
                {
                    Props = new[]
                    {
                        "stone 0", "stone cube", "rune pillar", "gravestone", "stone coffin",
                        "stone lantern", "pillar"
                    },
                    Scatter = new[] { "stone 0" },
                    ScatterDivisor = 10f,
                    ScatterMinScale = 0.35f,
                    ScatterMaxScale = 0.7f,
                    Perimeter = new[] { "stone 0", "pillar" },
                    PerimeterStep = 4.4f,
                    PerimeterSecondRing = true,
                    CanopyDivisor = 10f,
                    CanopyTint = new Color(0.02f, 0.03f, 0.08f),
                    FireflyDivisor = 44f,
                    FireflyTint = new Color(0.55f, 0.82f, 1f),
                    Shape = DecorShape.Blob,
                    MembersMin = 6,
                    MembersMax = 12,
                    SpreadMin = 1f,
                    SpreadMax = 3.6f,
                    EdgeBias = 0.85f,
                    PathWidth = 0f,
                    PathColor = new Color(1f, 1f, 1f, 0f)
                };

            // Ruinas: arquitectura caída (arcos, estatuas) y sepulcros. Los tramos de
            // escalinata son piezas modulares: esparcidos sueltos no leen como ruina.
            case "04_Ruinas":
                return new BiomeDecor
                {
                    Props = new[]
                    {
                        "pillar", "statue", "rune pillar", "gravestone", "stone bench",
                        "struct - gate", "altar", "stone coffin", "stone cube"
                    },
                    Scatter = new[] { "brick" },
                    ScatterDivisor = 8f,
                    ScatterMinScale = 0.4f,
                    ScatterMaxScale = 0.75f,
                    Perimeter = new[] { "pillar", "statue" },
                    PerimeterStep = 7.2f,
                    PerimeterSecondRing = false,
                    CanopyDivisor = 18f,
                    CanopyTint = new Color(0.07f, 0.06f, 0.07f),
                    FireflyDivisor = 0f,
                    FireflyTint = new Color(1f, 0.95f, 0.62f),
                    Shape = DecorShape.Row,
                    MembersMin = 4,
                    MembersMax = 8,
                    SpreadMin = 2.4f,
                    SpreadMax = 6.5f,
                    EdgeBias = 0.3f,
                    PathWidth = 2.4f,
                    PathColor = new Color(0.80f, 0.76f, 0.68f, 0.34f)
                };

            // Santuario: altar, estatuas y faroles de rito. Sin sepulcros ni escalinatas.
            default:
                return new BiomeDecor
                {
                    Props = new[]
                    {
                        "altar", "statue", "rune pillar", "pillar", "stone lantern",
                        "stone bench", "stone cube"
                    },
                    Scatter = new[] { "stone 0" },
                    ScatterDivisor = 10f,
                    ScatterMinScale = 0.35f,
                    ScatterMaxScale = 0.7f,
                    Perimeter = new[] { "pillar", "statue", "stone lantern" },
                    PerimeterStep = 6f,
                    PerimeterSecondRing = false,
                    CanopyDivisor = 24f,
                    CanopyTint = new Color(0.02f, 0.08f, 0.07f),
                    FireflyDivisor = 18f,
                    FireflyTint = new Color(0.72f, 1f, 0.72f),
                    Shape = DecorShape.Ring,
                    MembersMin = 6,
                    MembersMax = 10,
                    SpreadMin = 1.8f,
                    SpreadMax = 3.2f,
                    EdgeBias = 0.25f,
                    PathWidth = 4.4f,
                    PathColor = new Color(0.88f, 0.97f, 0.90f, 0.50f)
                };
        }
    }

    /// <summary>Todos los props del pack Cainos cuyo nombre contenga alguno de los términos.</summary>
    static string[] DecorPrefabsByKeywords(string[] keywords)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Sprites Externos/Cainos/Pixel Art Top Down - Basic/Prefab" });
        List<string> paths = new List<string>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            for (int k = 0; k < keywords.Length; k++)
            {
                if (file.Contains(keywords[k]))
                {
                    paths.Add(path);
                    break;
                }
            }
        }
        return paths.ToArray();
    }

    static bool IsReserved(Vector2 point, List<Vector2> points, float radius)
    {
        for (int i = 0; i < points.Count; i++)
            if (Vector2.Distance(point, points[i]) < radius) return true;
        return false;
    }

    static bool IsBlocked(Vector2 point, Rect[] blocks, float margin)
    {
        if (blocks == null) return false;
        for (int i = 0; i < blocks.Length; i++)
        {
            Rect r = blocks[i];
            if (point.x > r.x - margin && point.x < r.x + r.width + margin &&
                point.y > r.y - margin && point.y < r.y + r.height + margin) return true;
        }
        return false;
    }

    /// <summary>Nombre legible de un portal (el nombre de escena interno no se muestra al jugador).</summary>
    static string PortalLabel(string target)
    {
        switch (target)
        {
            case "00_MainMenu": return "Menú principal";
            case "01_Bosque": return "Bosque de la Arboleda";
            case "02_Aldea": return "Aldea de Thal";
            case "03_Cuevas": return "Cuevas de Raíz Negra";
            case "04_Ruinas": return "Ruinas del Santuario";
            case "05_Santuario": return "Santuario del Sello";
            default: return target;
        }
    }

    static void CreateWall(Transform parent, string name, Vector2 center, Vector2 size, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(center.x, center.y, 0f);
        go.layer = LayerMask.NameToLayer("Walls");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.sortingOrder = -5;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    public static void BuildMainMenu()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.05f, 0.04f);
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        GameObject lightGo = new GameObject("Global Light 2D");
        Light2D light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        GameObject ui = new GameObject("MainMenuUI");
        ui.AddComponent<MainMenuUI>();

        // Fondo vivo del menú: arboleda en parallax, luna, niebla y luciérnagas.
        GameObject backdropGo = new GameObject("MenuBackdrop");
        MainMenuBackdrop backdrop = backdropGo.AddComponent<MainMenuBackdrop>();
        backdrop.treeSprites = DecorSpritesByKeywords(new[] { "tree" });
        backdrop.bushSprites = DecorSpritesByKeywords(new[] { "bush" });

        EditorSceneManager.SaveScene(scene, ScenePath + "/00_MainMenu.unity");
    }

    /// <summary>Extrae el sprite principal de cada prefab de decoración que case con los términos.</summary>
    static Sprite[] DecorSpritesByKeywords(string[] keywords)
    {
        string[] paths = DecorPrefabsByKeywords(keywords);
        List<Sprite> sprites = new List<Sprite>();

        for (int i = 0; i < paths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            if (prefab == null) continue;

            SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null && sr.sprite != null && !sprites.Contains(sr.sprite)) sprites.Add(sr.sprite);
        }

        return sprites.ToArray();
    }

    /// <summary>Escena de créditos finales (también se abre desde el menú principal).</summary>
    static void BuildCreditsScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.012f, 0.022f, 0.030f);
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        GameObject lightGo = new GameObject("Global Light 2D");
        Light2D light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        GameObject ui = new GameObject("CreditsUI");
        ui.AddComponent<CreditsUI>();

        EditorSceneManager.SaveScene(scene, ScenePath + "/06_Creditos.unity");
    }

    // ---------------- Prefabs ----------------

    static GameObject BuildPlayerPrefab(Sprite sprite, GameObject projectilePrefab)
    {
        GameObject go = new GameObject("Player");
        go.layer = LayerMask.NameToLayer("Default");
        go.tag = "Player";
        SpriteMetrics metrics = MeasureSprite(sprite);
        go.transform.localScale = Vector3.one * (1.15f / metrics.Height);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "player";
        sr.sortingOrder = 0;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(metrics.Width * 0.50f, metrics.Height * 0.40f);
        col.offset = new Vector2(0f, -metrics.Height * 0.28f);

        Animator animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/player.controller");

        // Sombra de contacto: el héroe se apoya en el suelo en lugar de flotar sobre él.
        AddContactShadow(go.transform, metrics, "player");

        PlayerController pc = go.AddComponent<PlayerController>();
        pc.speed = 6f;
        pc.attackRange = 1.5f;
        pc.attackDamage = 12;
        pc.enemyLayers = LayerMask.GetMask("Enemies");
        pc.meleeCooldown = 0.45f;
        pc.rangedCooldown = 0.7f;
        pc.rangedDamage = 10;
        pc.projectileSpeed = 11f;
        pc.projectilePrefab = projectilePrefab;

        Health health = go.AddComponent<Health>();
        health.maxHealth = 100;
        health.isPlayer = true;
        health.destroyOnDeath = false;
        health.invulnerabilityTime = 0.6f;

        return SavePrefab(go, "Player");
    }

    static GameObject BuildEnemyPrefab(string name, Sprite sprite, string controllerName, float targetHeight,
        int health, float speed, int damage, float cooldown,
        float aggro, float leash, int reward, bool isBoss)
    {
        GameObject go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("Enemies");
        SpriteMetrics metrics = MeasureSprite(sprite);
        go.transform.localScale = Vector3.one * (targetHeight / metrics.Height);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "enemy";
        // El jefe se distingue por color, no por tamaño desproporcionado.
        if (isBoss) sr.color = new Color(0.92f, 0.56f, 0.52f);

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(metrics.Width * 0.55f, metrics.Height * 0.42f);
        col.offset = new Vector2(0f, -metrics.Height * 0.28f);
        // Trigger: el enemigo no debe empujar físicamente al jugador (lo empujaba
        // contra los portales). El daño lo aplica EnemyAI y los impactos se
        // detectan por OverlapCircle/OnTriggerEnter2D.
        col.isTrigger = true;

        Animator animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/" + controllerName);

        go.AddComponent<AgentMover2D>();

        Health h = go.AddComponent<Health>();
        h.maxHealth = health;
        h.destroyOnDeath = true;
        h.invulnerabilityTime = 0.1f;

        EnemyAI ai = go.AddComponent<EnemyAI>();
        ai.speed = speed;
        ai.attackDamage = damage;
        ai.attackCooldown = cooldown;
        ai.aggroRange = aggro;
        ai.leashRange = leash;
        ai.scoreReward = reward;
        ai.isBoss = isBoss;
        ai.attackRange = isBoss ? 2.2f : 1.1f;

        return SavePrefab(go, name);
    }

    static GameObject BuildCollectiblePrefab(Sprite sprite)
    {
        GameObject go = new GameObject("Collectible");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one * 0.9f;

        go.AddComponent<Collectible>();
        return SavePrefab(go, "Collectible");
    }

    static GameObject BuildNpcPrefab(Sprite sprite, Sprite[] walkDown, Sprite[] walkUp, Sprite[] walkSide)
    {
        GameObject go = new GameObject("NPC");
        int walls = LayerMask.NameToLayer("Walls");
        go.layer = walls >= 0 ? walls : 0;
        SpriteMetrics metrics = MeasureSprite(sprite);
        go.transform.localScale = Vector3.one * (1.2f / metrics.Height);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FirstFrame(walkDown, sprite);
        sr.color = new Color(0.72f, 0.86f, 1f);
        sr.sortingLayerName = "npc";

        // Sombra de contacto: ancla al aldeano al suelo (es lo que hacía que "flotara").
        AddContactShadow(go.transform, metrics, "npc");

        // Cinemática: el aldeano camina, pero no empuja al jugador ni a los enemigos.
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(metrics.Width * 0.55f, metrics.Height * 0.45f);
        col.offset = new Vector2(0f, -metrics.Height * 0.25f);
        // Trigger: así el aldeano tampoco se rasteriza como obstáculo en la rejilla A*.
        col.isTrigger = true;

        go.AddComponent<NpcInteractable>();
        go.AddComponent<NpcWanderer>();

        // Ciclo de caminata propio: sin él el aldeano se deslizaba como una estampa fija.
        NpcAnimator anim = go.AddComponent<NpcAnimator>();
        anim.walkDown = walkDown;
        anim.walkUp = walkUp;
        anim.walkSide = walkSide;
        anim.framesPerSecond = 9f;

        return SavePrefab(go, "NPC");
    }

    /// <summary>Primer fotograma disponible (o el sprite suelto como respaldo).</summary>
    static Sprite FirstFrame(Sprite[] frames, Sprite fallback)
    {
        if (frames != null && frames.Length > 0 && frames[0] != null) return frames[0];
        return fallback;
    }

    /// <summary>Sombra achatada bajo la entidad: sin ella el sprite parece flotar.</summary>
    static void AddContactShadow(Transform owner, SpriteMetrics metrics, string sortingLayer)
    {
        Sprite shadow = ShadowSprite();
        if (shadow == null) return;

        GameObject go = new GameObject("Shadow");
        go.transform.SetParent(owner, false);
        go.transform.localPosition = new Vector3(0f, -metrics.Height * 0.48f, 0f);
        go.transform.localScale = new Vector3(1f, 0.62f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = shadow;
        sr.color = new Color(0f, 0f, 0f, 0.55f);
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = -1;
    }

    /// <summary>La espora se distingue por color y estalla al morir.</summary>
    static void TuneSporePrefab(GameObject prefab)
    {
        if (prefab == null) return;
        using (PrefabUtility.EditPrefabContentsScope scope =
               new PrefabUtility.EditPrefabContentsScope(PrefabPath + "/Enemy_Spore.prefab"))
        {
            GameObject root = scope.prefabContentsRoot;
            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.78f, 0.55f, 1f);
            if (root.GetComponent<SporeBurst>() == null) root.AddComponent<SporeBurst>();
        }
    }

    /// <summary>Conecta la habilidad de invocación del jefe final.</summary>
    static void AttachSummoner(GameObject bossPrefab, GameObject summonPrefab)
    {
        if (bossPrefab == null || summonPrefab == null) return;
        using (PrefabUtility.EditPrefabContentsScope scope =
               new PrefabUtility.EditPrefabContentsScope(PrefabPath + "/Boss_Guardian.prefab"))
        {
            GameObject root = scope.prefabContentsRoot;
            BossSummoner summoner = root.GetComponent<BossSummoner>();
            if (summoner == null) summoner = root.AddComponent<BossSummoner>();
            summoner.summonPrefab = summonPrefab;
            summoner.firstSummonDelay = 2f;
            summoner.interval = 7.5f;
            summoner.perWave = 3;
            summoner.maxAlive = 8;
        }
    }

    static GameObject BuildPortalPrefab(Sprite sprite)
    {
        GameObject go = new GameObject("Portal");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.5f, 1f, 0.85f, 0.85f);
        sr.sortingOrder = 4;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.4f, 2.6f);

        go.AddComponent<ScenePortal>();
        return SavePrefab(go, "Portal");
    }

    static GameObject BuildProjectilePrefab(Sprite sprite)
    {
        GameObject go = new GameObject("Projectile");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 20;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.32f;

        go.AddComponent<PlayerProjectile>();
        return SavePrefab(go, "Projectile");
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        string path = PrefabPath + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    // ---------------- Assets ----------------

    static void BuildSfxLibrary()
    {
        string path = ResourcesPath + "/SfxLibrary.asset";
        AssetDatabase.DeleteAsset(path);
        SfxLibrary lib = ScriptableObject.CreateInstance<SfxLibrary>();
        lib.slash = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/sound/slash.ogg");
        lib.hurt = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/sound/hurt.ogg");
        lib.item = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/sound/item.ogg");
        lib.enemyDeath = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/sound/enemy-death.ogg");
        lib.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/sound/ancient_path.ogg");
        AssetDatabase.CreateAsset(lib, path);
    }

    static void BuildDialogueDatabase()
    {
        string path = ResourcesPath + "/DialogueDatabase.asset";
        AssetDatabase.DeleteAsset(path);
        DialogueDatabase db = ScriptableObject.CreateInstance<DialogueDatabase>();
        db.stories = new[]
        {
            new SceneStory { sceneName = "01_Bosque", title = "Bosque de la Arboleda", lines = new[] { "El sello se ha roto. Recupera los cuatro fragmentos y salva la Arboleda." } },
            new SceneStory { sceneName = "02_Aldea", title = "Aldea de Thal", lines = new[] { "Los aldeanos resisten. El segundo fragmento está cerca del pozo." } },
            new SceneStory { sceneName = "03_Cuevas", title = "Cuevas de Raíz Negra", lines = new[] { "La corrupción nace bajo tierra. Encuentra el tercer fragmento." } },
            new SceneStory { sceneName = "04_Ruinas", title = "Ruinas del Santuario", lines = new[] { "El último fragmento abre la puerta del Santuario." } },
            new SceneStory { sceneName = "05_Santuario", title = "Santuario del Sello", lines = new[] { "Derrota al Guardián y restaura el sello." } }
        };
        AssetDatabase.CreateAsset(db, path);
    }

    static void ConfigureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        for (int i = 0; i < SceneNames.Length; i++)
        {
            string path = ScenePath + "/" + SceneNames[i] + ".unity";
            if (File.Exists(path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ---------------- Utilidades ----------------

    static void EnsureFolders()
    {
        string[] folders = { "Assets/_RPG", ArtPath, PrefabPath, ScenePath, ResourcesPath, Root + "/Data", Root + "/Scripts" };
        for (int i = 0; i < folders.Length; i++)
            if (!AssetDatabase.IsValidFolder(folders[i])) AssetDatabase.CreateFolder(Path.GetDirectoryName(folders[i]).Replace("\\", "/"), Path.GetFileName(folders[i]));
    }

    static void EnsureLayer(string layerName)
    {
        if (LayerMask.NameToLayer(layerName) >= 0) return;
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty sp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                return;
            }
        }
        Debug.LogWarning("[Aura] No hay huecos de layer libres para " + layerName);
    }

    static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < all.Length; i++)
        {
            Sprite candidate = all[i] as Sprite;
            if (candidate != null) return candidate;
        }
        return null;
    }

    /// <summary>Todos los sprites recortados de una hoja, ordenados por nombre (_0, _1, ...).</summary>
    static Sprite[] LoadSprites(string path)
    {
        List<Sprite> found = new List<Sprite>();
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < all.Length; i++)
        {
            Sprite s = all[i] as Sprite;
            if (s != null) found.Add(s);
        }
        found.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return found.ToArray();
    }

    static Sprite MakeSprite(string name, int w, int h, System.Func<float, float, Color> painter)
    {
        string filePath = ArtPath + "/" + name + ".png";
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, painter((x + 0.5f) / w, (y + 0.5f) / h));
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(filePath, png);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.spritePixelsPerUnit = Mathf.Max(w, h);
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
    }

    static Color FloorPainter(float u, float v)
    {
        int check = (Mathf.FloorToInt(u * 4f) + Mathf.FloorToInt(v * 4f)) % 2;
        float shade = check == 0 ? 1f : 0.93f;
        float grain = 0.96f + 0.06f * Mathf.PerlinNoise(u * 8f, v * 8f);
        return new Color(shade * grain, shade * grain, shade * grain, 1f);
    }

    static Color WallPainter(float u, float v)
    {
        float brick = (Mathf.Repeat(v * 4f, 1f) < 0.12f) ? 0.75f : 1f;
        float offset = Mathf.FloorToInt(v * 4f) % 2 == 0 ? 0f : 0.5f;
        float mortar = (Mathf.Repeat(u * 2f + offset, 1f) < 0.08f) ? 0.7f : 1f;
        float k = brick * mortar;
        return new Color(k, k, k, 1f);
    }

    static Color CoinPainter(float u, float v)
    {
        float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
        if (d > 0.46f) return new Color(1f, 1f, 1f, 0f);
        float k = d > 0.38f ? 0.65f : 1f;
        return new Color(k, k * 0.85f, k * 0.2f, 1f);
    }

    static Color GemPainter(float u, float v)
    {
        float d = Mathf.Abs(u - 0.5f) + Mathf.Abs(v - 0.5f) * 0.75f;
        if (d > 0.45f) return new Color(1f, 1f, 1f, 0f);
        float k = d > 0.34f ? 0.6f : 1f;
        return new Color(0.35f * k, 0.95f * k, 1f * k, 1f);
    }

    static Color HeartPainter(float u, float v)
    {
        Vector2 a = new Vector2(0.33f, 0.62f);
        Vector2 b = new Vector2(0.67f, 0.62f);
        float d = Mathf.Min(Vector2.Distance(new Vector2(u, v), a), Vector2.Distance(new Vector2(u, v), b));
        bool triangle = v < 0.62f && Mathf.Abs(u - 0.5f) < (v - 0.08f) * 0.62f;
        if (d > 0.2f && !triangle) return new Color(1f, 1f, 1f, 0f);
        return new Color(0.95f, 0.25f, 0.35f, 1f);
    }

    static Color FragmentPainter(float u, float v)
    {
        float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
        float ring = Mathf.Abs(d - 0.33f);
        if (ring > 0.1f) return new Color(1f, 1f, 1f, 0f);
        return new Color(0.55f, 1f, 0.65f, 1f);
    }

    static Color BoltPainter(float u, float v)
    {
        float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
        if (d > 0.48f) return new Color(1f, 1f, 1f, 0f);
        return new Color(1f, 0.95f, 0.4f, 1f);
    }

    static Color PortalPainter(float u, float v)
    {
        float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
        if (edge < 0.08f) return new Color(0.5f, 1f, 0.9f, 0f);
        float glow = 0.45f + 0.55f * Mathf.Sin(v * 12f) * 0.5f + 0.3f;
        return new Color(0.45f, 0.95f, 0.9f, Mathf.Clamp01(glow));
    }

    static Sprite fireflySpriteCache;

    /// <summary>Punto luminoso suave para las luciérnagas ambientales.</summary>
    static Sprite FireflySprite()
    {
        if (fireflySpriteCache == null) fireflySpriteCache = MakeSprite("firefly", 8, 8, FireflyPainter);
        return fireflySpriteCache;
    }

    static Color FireflyPainter(float u, float v)
    {
        float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
        if (d > 1f) return new Color(1f, 1f, 1f, 0f);
        float a = Mathf.Pow(1f - d, 0.85f);
        return new Color(1f, 0.96f, 0.68f, a);
    }

    static Sprite shadowSpriteCache;

    /// <summary>Elipse negra suave para las sombras de contacto.</summary>
    static Sprite ShadowSprite()
    {
        if (shadowSpriteCache == null) shadowSpriteCache = MakeSprite("shadow", 16, 8, ShadowPainter);
        return shadowSpriteCache;
    }

    static Color ShadowPainter(float u, float v)
    {
        float dx = (u - 0.5f) * 2f;
        float dy = (v - 0.5f) * 2f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 1f) return new Color(0f, 0f, 0f, 0f);
        return new Color(0f, 0f, 0f, Mathf.Pow(1f - d, 1.4f) * 0.9f);
    }
}
