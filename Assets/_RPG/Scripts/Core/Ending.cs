using System.Text;

/// <summary>
/// Final del juego y textos de créditos. El epílogo se adapta a cómo jugaste
/// (comercio con los aldeanos, enemigos derrotados, exploración), de modo que
/// el "mejor final" se siente ganado y no solo mostrado.
/// </summary>
public static class Ending
{
    public const string Title = "EL SELLO RESPIRA DE NUEVO";

    // ---------- Rango ----------

    public static string Rank()
    {
        int merit = GameSession.Score + GameSession.Trades * 90 + GameSession.NpcsTalked * 40;
        if (GameSession.BossDefeated && merit >= 4200) return "GUARDIÁN DEL SELLO";
        if (GameSession.BossDefeated && merit >= 3000) return "HERALDO DE LA ARBOLEDA";
        if (GameSession.BossDefeated) return "VIAJERO DEL SELLO";
        return "APRENDIZ DEL BOSQUE";
    }

    // ---------- Victoria ----------

    public static string Epilogue()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("El Guardián cae de rodillas y la corrupción se deshace como ceniza al viento. ");
        sb.Append("Los cuatro fragmentos buscan su sitio en el sello y la grieta se cierra con un susurro de hojas.\n\n");

        sb.Append("El bosque vuelve primero: la corteza gris se llena de musgo, las luciérnagas regresan a los claros ");
        sb.Append("y el sendero que abriste a golpes de valor se cubre de flores que nadie había visto en cien inviernos.\n\n");

        if (GameSession.Trades > 0)
            sb.Append("En Thal encienden faroles en cada esquina. Los aldeanos recuerdan tus monedas y, sobre todo, ");
        else
            sb.Append("En Thal encienden faroles en cada esquina. Los aldeanos recuerdan al viajero que llegó sin pedir nada y ");

        sb.Append("recuerdan que elegiste escuchar antes de golpear. ");
        sb.Append("La herrera, el minero, el erudito y el espíritu del sello dan tu nombre a la primera guardia de la Arboleda.\n\n");

        sb.Append("El Guardián, ya libre, te entrega su espada rota: «no custodies el sello con miedo, ");
        sb.Append("custódialo con quienes caminan contigo». Y el sello se cierra, esta vez, sin dueño.\n\n");

        sb.Append("<color=#F7D98C>Final alcanzado: ").Append(Rank()).Append("</color>");
        return sb.ToString();
    }

    public static string Stats()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Puntaje ").Append(GameSession.Score);
        sb.Append("   ·   Enemigos ").Append(GameSession.Kills);
        sb.Append("   ·   Monedas ").Append(GameSession.Coins);
        sb.Append("   ·   Compras ").Append(GameSession.Trades);
        sb.Append("\nFragmentos ").Append(GameSession.Fragments).Append("/4");
        sb.Append("   ·   Aldeanos escuchados ").Append(GameSession.NpcsTalked);
        sb.Append("   ·   Gemas ").Append(GameSession.Gems);
        return sb.ToString();
    }

    // ---------- Créditos ----------

    /// <summary>Reflexión sobre lo aprendido jugando (se muestra en los créditos).</summary>
    public static string Reflection()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("REFLEXIÓN\n\n");
        sb.Append("Jugar este juego nos enseñó algo que ningún apunte explica igual: un mundo se vuelve real ");
        sb.Append("cuando alguien decide recorrerlo. El bosque no es un decorado, es un sistema: árboles que ");
        sb.Append("componen claros, aldeanos que huyen cuando el peligro se acerca, monedas que solo valen algo ");
        sb.Append("cuando sirven para ayudar a otro.\n\n");
        sb.Append("Aprendimos que la dificultad no está en el enemigo más grande, sino en la paciencia de volver a ");
        sb.Append("intentarlo; que un jefe final se vuelve inolvidable cuando cambia las reglas e invoca aliados, y ");
        sb.Append("que el miedo de un aldeano corriendo dice más del peligro que cualquier barra de vida.\n\n");
        sb.Append("Aprendimos, sobre todo, que programar un videojuego es contar una historia con reglas: cada ");
        sb.Append("variable es una decisión, cada error es una pista y cada detalle amable del camino —una ");
        sb.Append("luciérnaga, un farol encendido, un gracias— es la parte que el jugador recuerda al final.\n");

        return sb.ToString();
    }

    /// <summary>Créditos completos: equipo, institución y reconocimientos.</summary>
    public static string CreditsBody()
    {
        StringBuilder sb = new StringBuilder();

        // Los tamaños van en <size> absolutos (Best Fit no los escala), así que
        // se ajustan para que el bloque entero quepa sobre el botón VOLVER.
        sb.Append("<size=22><color=#C9A85B>DISEÑO Y PROGRAMACIÓN</color></size>\n");
        sb.Append("<size=28>RESENDIZ OLIVERA ALEXANDER JOSUE</size>\n");
        sb.Append("<size=28>ROSALES SANCHEZ ERIC LEONARDO</size>\n\n");

        sb.Append("<size=22><color=#C9A85B>INSTITUCIÓN</color></size>\n");
        sb.Append("<size=24>FACULTAD DE ESTUDIOS SUPERIORES ARAGÓN</size>\n");
        sb.Append("<size=21>UNIVERSIDAD NACIONAL AUTÓNOMA DE MÉXICO</size>\n\n");

        sb.Append("<size=22><color=#C9A85B>MATERIA</color></size>\n");
        sb.Append("<size=24>PROGRAMACIÓN DE VIDEOJUEGOS</size>\n\n");

        sb.Append("<size=22><color=#C9A85B>ARTE Y SONIDO</color></size>\n");
        sb.Append("<size=20>Cainos (Pixel Art Top Down), Tiny RPG Adventure\n");
        sb.Append("Efectos de sonido del propio proyecto</size>\n\n");

        sb.Append("<size=22><color=#C9A85B>PROYECTO</color></size>\n");
        sb.Append("<size=24>LA CORRUPCIÓN DE LA ARBOLEDA</size>\n\n");
        sb.Append("<size=26><color=#F7D98C>Gracias por jugar.</color></size>");

        return sb.ToString();
    }
}
