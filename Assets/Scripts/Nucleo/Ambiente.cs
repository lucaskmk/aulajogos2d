using UnityEngine;

// Partículas de clima flutuando na frente do cenário, sempre em volta da câmera:
//  - Polen:     pontinhos claros subindo devagar, balançando (dia normal);
//  - Petalas:   pétalas cor-de-rosa caindo (fases rosadas);
//  - VagaLumes: luzinhas amarelas que piscam e iluminam em volta (fase noturna);
//  - Poeira:    poeira dourada parada no ar (as cabines);
//  - Brilhos:   estrelinhas (fase secreta);
//  - Folhas:    folhinhas verdes caindo e girando (a selva, o penhasco dos ventos);
//  - Chuva:     riscos de chuva caindo inclinados e rápidos (a tempestade do tsunami);
//  - Bolhas:    bolhinhas subindo, cada vez mais rápido (o covil do Kraken, no fundo do mar);
//  - Almas:     luzinhas verde-claras subindo devagar e apagando (o navio fantasma).
// Cada fase escolhe as suas (Fase.clima, em Fases.cs).
[DefaultExecutionOrder(110)] // depois da câmera e da paralaxe
public class Ambiente : MonoBehaviour
{
    public enum Tipo { Polen, Petalas, VagaLumes, Poeira, Brilhos, Folhas, Chuva, Bolhas, Almas }

    Tipo tipo;
    Transform[] particulas;
    SpriteRenderer[] desenhos;
    Vector2[] velocidades;
    float[] fases;

    public static void Criar(Transform raiz, Tipo tipo, int quantidade)
    {
        var ambiente = new GameObject("Ambiente " + tipo).AddComponent<Ambiente>();
        ambiente.transform.SetParent(raiz, false);
        ambiente.tipo = tipo;
        ambiente.particulas = new Transform[quantidade];
        ambiente.desenhos = new SpriteRenderer[quantidade];
        ambiente.velocidades = new Vector2[quantidade];
        ambiente.fases = new float[quantidade];

        Camera cam = Camera.main;
        float cx = cam != null ? cam.transform.position.x : 0f;
        for (int i = 0; i < quantidade; i++)
        {
            // Pólen, poeira e vaga-lumes são 1 ou 2 pixels de verdade ("hd2d_pixel"): com a tela em baixa
            // resolução (TelaPixelada), um pontinho menor que 1 pixel pisca e some.
            string sprite = tipo == Tipo.Brilhos ? "brilho" : tipo == Tipo.Petalas ? "poeira" : tipo == Tipo.Folhas ? "folhinha"
                : tipo == Tipo.Chuva ? "pingo_de_chuva" : tipo == Tipo.Bolhas ? "bolhinha" : "hd2d_pixel";
            var p = ConstrutorDeFase.Visual("Particula", ambiente.transform, new Vector3(cx + Random.Range(-15f, 15f), Random.Range(0f, 15f), 0f), sprite, 15, false);
            ambiente.particulas[i] = p.transform;
            ambiente.desenhos[i] = p.GetComponent<SpriteRenderer>();
            ambiente.fases[i] = Random.Range(0f, 10f);
            ambiente.Preparar(i);
            if (tipo == Tipo.VagaLumes && i % 2 == 0)
                Luzes.Ponto(p.transform, new Color(1f, 0.95f, 0.5f), 1.6f, 0.6f);
            if (tipo == Tipo.Almas && i % 3 == 0)
                Luzes.Ponto(p.transform, new Color(0.6f, 1f, 0.85f), 1.4f, 0.5f);
        }
    }

    void Preparar(int i)
    {
        float tamanho;
        Color cor;
        switch (tipo)
        {
            case Tipo.Petalas:
                velocidades[i] = new Vector2(Random.Range(0.4f, 1.2f), Random.Range(-1.2f, -0.6f));
                tamanho = Random.Range(0.6f, 0.8f); // (pelo menos uns 3 pixels: menor que isso some na tela pixelada)
                cor = new Color(1f, 0.7f, 0.8f, 0.9f);
                break;
            case Tipo.VagaLumes:
                velocidades[i] = Random.insideUnitCircle * 0.4f;
                tamanho = 2f; // 2 x 2 pixels
                cor = new Color(1f, 0.95f, 0.5f);
                break;
            case Tipo.Poeira:
                velocidades[i] = new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.05f, 0.1f));
                tamanho = Random.value < 0.75f ? 1f : 2f; // 1 ou 2 pixels
                cor = new Color(1f, 0.9f, 0.6f, 0.7f);
                break;
            case Tipo.Brilhos:
                velocidades[i] = new Vector2(0f, Random.Range(0.2f, 0.5f));
                tamanho = Random.Range(0.3f, 0.6f);
                cor = new Color(1f, 1f, 0.8f, 0.9f);
                break;
            case Tipo.Folhas:
                velocidades[i] = new Vector2(Random.Range(-1.4f, -0.5f), Random.Range(-1.3f, -0.7f));
                tamanho = Random.Range(0.8f, 1f);
                cor = Color.Lerp(new Color(0.55f, 0.85f, 0.35f), new Color(0.95f, 0.8f, 0.35f), Random.value * 0.4f);
                break;
            case Tipo.Chuva:
                velocidades[i] = new Vector2(-3.5f, Random.Range(-17f, -13f));
                tamanho = Random.Range(0.8f, 1.1f);
                cor = new Color(0.75f, 0.85f, 1f, 0.55f);
                break;
            case Tipo.Bolhas:
                velocidades[i] = new Vector2(0f, Random.Range(0.6f, 1.4f));
                tamanho = Random.Range(0.5f, 1f);
                cor = new Color(1f, 0.85f, 0.85f, 0.6f);
                break;
            case Tipo.Almas:
                velocidades[i] = new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(0.25f, 0.6f));
                tamanho = 2f; // 2 x 2 pixels
                cor = new Color(0.65f, 1f, 0.85f);
                break;
            default: // Polen
                velocidades[i] = new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(0.15f, 0.4f));
                tamanho = Random.value < 0.75f ? 1f : 2f; // 1 ou 2 pixels
                cor = new Color(1f, 1f, 0.85f, 0.65f);
                break;
        }
        particulas[i].localScale = Vector3.one * tamanho;
        desenhos[i].color = cor;
    }

    // Os desenhos pequenos das partículas novas (a folha, o pingo e a bolha).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("folhinha", () => FabricaDeSprites.Procedural(4, 3, FabricaDeSprites.Centro, (x, y) =>
            (x == 0 && y != 1) || (x == 3 && y != 1) ? FabricaDeSprites.Transparente : new Color32(255, 255, 255, 255)));
        FabricaDeSprites.Registrar("pingo_de_chuva", () => FabricaDeSprites.Procedural(1, 5, FabricaDeSprites.Centro, (x, y) =>
            new Color32(255, 255, 255, (byte)(90 + y * 40))));
        FabricaDeSprites.Registrar("bolhinha", () => FabricaDeSprites.Procedural(4, 4, FabricaDeSprites.Centro, (x, y) =>
        {
            bool canto = (x == 0 || x == 3) && (y == 0 || y == 3);
            if (canto) return FabricaDeSprites.Transparente;
            bool borda = x == 0 || x == 3 || y == 0 || y == 3;
            if (x == 1 && y == 2) return new Color32(255, 255, 255, 255); // o brilhinho
            return borda ? new Color32(255, 255, 255, 220) : new Color32(255, 255, 255, 40);
        }));
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        float meia = cam.orthographicSize * cam.aspect + 1f;
        float cx = cam.transform.position.x, dt = Time.deltaTime, t = Time.time;

        for (int i = 0; i < particulas.Length; i++)
        {
            Vector3 p = particulas[i].position;
            float balanco = Mathf.Sin(t * 1.5f + fases[i]);
            float subida = tipo == Tipo.Bolhas ? 0.6f + 0.1f * p.y : 1f; // as bolhas sobem cada vez mais rápido
            p.x += (velocidades[i].x + balanco * (tipo == Tipo.Chuva ? 0f : 0.3f)) * dt;
            p.y += velocidades[i].y * subida * dt;

            // saiu da tela? volta pelo outro lado
            if (p.x < cx - meia) p.x += meia * 2f;
            if (p.x > cx + meia) p.x -= meia * 2f;
            if (p.y < -0.5f) p.y += 15.5f;
            if (p.y > 15f) p.y -= 15.5f;
            particulas[i].position = p;

            if (tipo == Tipo.Petalas) particulas[i].rotation = Quaternion.Euler(0f, 0f, balanco * 60f);
            if (tipo == Tipo.Folhas) particulas[i].rotation = Quaternion.Euler(0f, 0f, t * 90f + fases[i] * 40f);
            if (tipo == Tipo.Chuva) particulas[i].rotation = Quaternion.Euler(0f, 0f, -12f); // inclinada pelo vento
            if (tipo == Tipo.VagaLumes || tipo == Tipo.Brilhos || tipo == Tipo.Almas)
            {
                Color c = desenhos[i].color;
                c.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 2f + fases[i]));
                desenhos[i].color = c;
            }
        }
    }
}
