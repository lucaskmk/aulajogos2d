using UnityEngine;

// Ícones do menu: as bandeiras da escolha de idioma, no título (Brasil = Português, Estados Unidos = English),
// e a lixeira da tela dos slots.
// Desenhadas por código, pixel a pixel (como o resto da arte): Pegar("bandeira_brasil") / Pegar("bandeira_eua").
// As duas têm uma borda escura de 1 pixel. (no Texture2D, y = 0 é a linha de BAIXO)
public static class ArteDasBandeiras
{
    public const int LarguraDoBrasil = 30, AlturaDoBrasil = 21; // 10:7, como a bandeira de verdade
    public const int LarguraDosEUA = 30, AlturaDosEUA = 15;     // 13 listras de 1 pixel + a borda

    static readonly Color32 Borda = new Color32(20, 20, 28, 255);
    static readonly Color32 Verde = new Color32(0, 156, 59, 255);
    static readonly Color32 Amarelo = new Color32(255, 223, 0, 255);
    static readonly Color32 AzulDoBrasil = new Color32(0, 39, 118, 255);
    static readonly Color32 Branco = new Color32(255, 255, 255, 255);
    static readonly Color32 Vermelho = new Color32(191, 10, 48, 255);
    static readonly Color32 AzulDosEUA = new Color32(10, 49, 97, 255);

    // A LIXEIRA da tela dos slots (o botão de apagar um slot): tampa com a alça, o balde de metal com os frisos.
    static readonly string[] ArteDaLixeira =
    {
        "....kkkkkk....",
        "....kzzzzk....",
        "kkkkkkkkkkkkkk",
        "kZZZZZZZZZZZZk",
        "kmmmmmmmmmmmMk",
        "kkkkkkkkkkkkkk",
        ".kmZmzmZmzmMk.",
        ".kmZmzmZmzmMk.",
        ".kmZmzmZmzmMk.",
        ".kmZmzmZmzmMk.",
        ".kmZmzmZmzmMk.",
        ".kmZmzmZmzmMk.",
        ".kmZmzmZmzmMk.",
        ".kmmmmmmmmmMk.",
        ".kkkkkkkkkkkk.",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("lixeira", () => FabricaDeSprites.DeArte(ArteDaLixeira, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("bandeira_brasil", () => FabricaDeSprites.Procedural(LarguraDoBrasil, AlturaDoBrasil, FabricaDeSprites.Centro, Brasil));
        FabricaDeSprites.Registrar("bandeira_eua", () => FabricaDeSprites.Procedural(LarguraDosEUA, AlturaDosEUA, FabricaDeSprites.Centro, EUA));
    }

    // Verde, o losango amarelo, o círculo azul e a faixa branca atravessando o círculo.
    static Color32 Brasil(int x, int y)
    {
        if (x == 0 || y == 0 || x == LarguraDoBrasil - 1 || y == AlturaDoBrasil - 1) return Borda;
        float cx = (LarguraDoBrasil - 1) / 2f, cy = (AlturaDoBrasil - 1) / 2f;
        float dx = x - cx, dy = y - cy;
        if (dx * dx + dy * dy <= 5.2f * 5.2f)
        {
            // a faixa: um pedaço de um círculo grande, com o centro lá embaixo (ela "sorri" para cima)
            float faixa = Mathf.Sqrt(dx * dx + (y - (cy - 14f)) * (y - (cy - 14f)));
            return faixa >= 14.6f && faixa <= 15.7f ? Branco : AzulDoBrasil;
        }
        if (Mathf.Abs(dx) / 12.5f + Mathf.Abs(dy) / 8f <= 1f) return Amarelo;
        return Verde;
    }

    // 13 listras (vermelho em cima e embaixo) e o cantão azul com as estrelinhas.
    static Color32 EUA(int x, int y)
    {
        if (x == 0 || y == 0 || x == LarguraDosEUA - 1 || y == AlturaDosEUA - 1) return Borda;
        int listra = AlturaDosEUA - 2 - y; // 0 = a listra de cima
        if (x <= 12 && listra <= 6)
        {
            // o cantão: as estrelas em xadrez (um pixel branco sim, um não)
            bool estrela = x >= 2 && x <= 11 && listra >= 1 && listra <= 5 && (x + listra) % 2 == 1;
            return estrela ? Branco : AzulDosEUA;
        }
        return listra % 2 == 0 ? Vermelho : Branco;
    }
}
