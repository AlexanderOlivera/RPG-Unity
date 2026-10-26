using UnityEngine;

/// <summary>
/// Punto de entrada para relanzar la generación de contenido desde herramientas externas.
/// Equivale al menú Aura / 1. Generar contenido completo.
/// </summary>
public static class AuraBuildRunner
{
    public static void Execute()
    {
        Debug.Log("[Aura] Lanzando GameSceneBuilder.BuildAll()");
        GameSceneBuilder.BuildAll();
        Debug.Log("[Aura] GameSceneBuilder.BuildAll() terminado");
    }
}
