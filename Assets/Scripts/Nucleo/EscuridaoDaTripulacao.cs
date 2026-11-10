using System.Collections.Generic;
using UnityEngine;

// A ESCURIDÃO dos chefes (a etapa 3 da Cozinheira e o ataque final da Baleia): tudo preto, menos um círculo de
// luz em volta de CADA Grumete (os vivos e os que estão pendurados no balão).
//
// Antes era um desenho fixo com UM buraco no meio, que andava junto com o líder: no coop, a luz ficava pulando de
// um jogador para o outro e os demais ficavam no breu. Agora o desenho é montado a cada quadro, numa textura
// pequena presa na câmera: preto em tudo e um buraco pontilhado (matriz de Bayer, como antes) em cada Grumete.
// A transparência geral continua vindo do SpriteRenderer.color (os chefes fazem a escuridão aparecer e sumir).
public class EscuridaoDaTripulacao : MonoBehaviour
{
    // O tamanho do que a escuridão cobre, em blocos (bem maior que a tela, até em monitor ultralargo).
    const float LarguraEmBlocos = 56f, AlturaEmBlocos = 30f;
    static readonly int[] Bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

    float blocosPorPixel, claro, escuro; // (claro e escuro: os raios do buraco, em pixels da textura)
    Color32 cor;
    byte alfaDaBorda;                    // 0 = borda só pontilhada; mais = os pontinhos claros da borda ficam meio escuros
    int largura, altura;
    Texture2D textura;
    Color32[] cheio, pixels;

    // Troca o desenho de "escuridao" pela textura viva. raioClaro e raioEscuro em blocos: até o claro não tem
    // escuridão nenhuma; do claro ao escuro ela vai fechando em pontinhos.
    public static EscuridaoDaTripulacao Ligar(SpriteRenderer escuridao, float blocosPorPixel, float raioClaro, float raioEscuro,
        Color32 cor, byte alfaDaBorda = 0)
    {
        var viva = escuridao.gameObject.AddComponent<EscuridaoDaTripulacao>();
        viva.blocosPorPixel = blocosPorPixel;
        viva.claro = raioClaro / blocosPorPixel;
        viva.escuro = raioEscuro / blocosPorPixel;
        viva.cor = cor;
        viva.alfaDaBorda = alfaDaBorda;
        viva.largura = Mathf.CeilToInt(LarguraEmBlocos / blocosPorPixel);
        viva.altura = Mathf.CeilToInt(AlturaEmBlocos / blocosPorPixel);
        viva.textura = new Texture2D(viva.largura, viva.altura, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        viva.cheio = new Color32[viva.largura * viva.altura];
        for (int i = 0; i < viva.cheio.Length; i++) viva.cheio[i] = cor;
        viva.pixels = new Color32[viva.cheio.Length];
        escuridao.sprite = Sprite.Create(viva.textura, new Rect(0, 0, viva.largura, viva.altura), new Vector2(0.5f, 0.5f),
            1f / blocosPorPixel, 0, SpriteMeshType.FullRect);
        escuridao.transform.localScale = Vector3.one;
        viva.Desenhar();
        return viva;
    }

    void OnDestroy()
    {
        if (textura != null) Destroy(textura);
    }

    // Depois de todo mundo andar neste quadro (e de a câmera se posicionar).
    void LateUpdate() => Desenhar();

    void Desenhar()
    {
        // presa na câmera, mas andando de pixel em pixel da textura (senão os pontinhos da borda "ferveriam")
        Camera cam = Camera.main;
        Vector2 centro = cam != null ? (Vector2)cam.transform.position : (Vector2)transform.position;
        centro.x = Mathf.Round(centro.x / blocosPorPixel) * blocosPorPixel;
        centro.y = Mathf.Round(centro.y / blocosPorPixel) * blocosPorPixel;
        transform.position = new Vector3(centro.x, centro.y, transform.position.z);

        System.Array.Copy(cheio, pixels, cheio.Length);
        List<Jogador> jogadores = GerenciadorDoJogo.Jogadores;
        foreach (Jogador jogador in jogadores)
        {
            // os vivos e os do balão têm luz; quem acabou de morrer (caindo da tela) não
            if (jogador == null || (jogador.Morto && !jogador.NaBolha)) continue;
            Vector2 p = jogador.transform.position;
            Furar((p.x - centro.x) / blocosPorPixel + largura / 2f, (p.y - centro.y) / blocosPorPixel + altura / 2f);
        }
        textura.SetPixels32(pixels);
        textura.Apply(false);
    }

    // Quanto de escuridão tem num lugar do mundo (0 = luz, 255 = breu). Para os testes.
    public byte AlfaEm(Vector2 lugar)
    {
        Vector2 centro = transform.position;
        int x = Mathf.FloorToInt((lugar.x - centro.x) / blocosPorPixel + largura / 2f);
        int y = Mathf.FloorToInt((lugar.y - centro.y) / blocosPorPixel + altura / 2f);
        return x < 0 || y < 0 || x >= largura || y >= altura ? (byte)255 : pixels[y * largura + x].a;
    }

    // Abre o buraco de luz em volta de (cx, cy), em pixels da textura.
    void Furar(float cx, float cy)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - escuro)), x1 = Mathf.Min(largura - 1, Mathf.CeilToInt(cx + escuro));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - escuro)), y1 = Mathf.Min(altura - 1, Mathf.CeilToInt(cy + escuro));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d >= escuro) continue;
                int i = y * largura + x;
                if (d <= claro)
                {
                    pixels[i] = new Color32(cor.r, cor.g, cor.b, 0);
                    continue;
                }
                // na borda: cada vez mais pontinhos escuros (quem já foi clareado por outro Grumete continua claro)
                float p = (d - claro) / (escuro - claro);
                if (p > (Bayer[(y % 4) * 4 + (x % 4)] + 0.5f) / 16f) continue;
                byte alfa = (byte)(alfaDaBorda * p);
                if (alfa < pixels[i].a) pixels[i] = new Color32(cor.r, cor.g, cor.b, alfa);
            }
    }
}
