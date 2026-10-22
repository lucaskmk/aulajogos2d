using UnityEngine;

// Os quadros de VENTO das plantas, desenhados por código (o mesmo truque do capim de Stardew Valley).
//
// Girar um desenho de pixel art fica feio (os pixels "serrilham"). Em vez disso, fazemos várias POSES:
// cada linha do desenho é empurrada alguns pixels para o lado, MAIS no topo e NADA na base (que está presa
// no chão). Do mesmo desenho saem 7 poses: toda inclinada para a esquerda, ..., reta, ..., toda para a direita.
// Quem escolhe a pose de cada planta, a cada momento, é o Vento.cs.
//
// Exemplo (topo empurrado 2 pixels para a direita, base parada):
//      reta:        inclinada:
//      .#.#.        ...#.#
//      .###.        ..###.
//      ..#..        ..#..
//      ..#..        ..#..
//
// Cada pose vira um sprite registrado na FabricaDeSprites ("flor_vento_0" a "flor_vento_6"), criado
// uma vez só e guardado no cache (nada é recriado quando a fase recomeça).
public static class ArteDoVento
{
    public const int Poses = 7;       // número ímpar: a do meio (3) é a planta reta
    public const int PoseReta = Poses / 2;

    // Como cada planta balança.
    struct Jeito
    {
        public float amplitude; // quantos pixels o topo anda na pose mais inclinada
        public float presa;     // fração da altura que fica parada (0,3 = os 30% de baixo não mexem)
        public bool daVolta;    // desenho que se repete lado a lado: o que sai por um lado entra pelo outro
        public bool deCabeca;   // preso EM CIMA (galho pendurado): quem balança é a parte de baixo
    }

    static readonly string[] Plantas =
    {
        "flor", "tufo", "arbusto", "arvore", "mapa_palmeira_morta", "hd2d_frente_capim", "hd2d_frente_galho",
        // as plantas do mapa do mundo (ArteDoMapaEnfeites)
        "mapa_palmeira_0", "mapa_palmeira_1", "mapa_palmeira_2", "mapa_flores_0", "mapa_flores_1", "mapa_flores_2", "mapa_flores_3",
    };

    static Jeito JeitoDe(string planta)
    {
        switch (planta)
        {
            case "flor": return new Jeito { amplitude = 2f, presa = 0.15f };
            case "tufo": return new Jeito { amplitude = 1.5f, presa = 0.2f };
            case "arbusto": return new Jeito { amplitude = 1f, presa = 0.35f };
            case "arvore": return new Jeito { amplitude = 1.5f, presa = 0.38f };        // o tronco não mexe, só a copa
            case "mapa_palmeira_morta": return new Jeito { amplitude = 1.5f, presa = 0.25f };
            case "mapa_palmeira_0":
            case "mapa_palmeira_1":
            case "mapa_palmeira_2": return new Jeito { amplitude = 1.5f, presa = 0.36f };
            case "mapa_flores_0":
            case "mapa_flores_1":
            case "mapa_flores_2":
            case "mapa_flores_3": return new Jeito { amplitude = 1.5f, presa = 0.2f };
            case "hd2d_frente_capim": return new Jeito { amplitude = 2f, presa = 0.3f, daVolta = true };
            case "hd2d_frente_galho": return new Jeito { amplitude = 1.5f, presa = 0.3f, daVolta = true, deCabeca = true };
            default: return new Jeito { amplitude = 1f, presa = 0.3f };
        }
    }

    public static bool TemVento(string planta) => System.Array.IndexOf(Plantas, planta) >= 0;
    public static string NomeDaPose(string planta, int pose) => planta + "_vento_" + pose;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        foreach (string planta in Plantas)
            for (int pose = 0; pose < Poses; pose++)
            {
                string nome = planta; int p = pose; // cópias para a função guardada lembrar destes valores
                FabricaDeSprites.Registrar(NomeDaPose(nome, p), () => Pose(nome, p));
            }
    }

    // Todas as poses de uma planta, da mais inclinada para a esquerda até a mais inclinada para a direita.
    public static Sprite[] TodasAsPoses(string planta)
    {
        var poses = new Sprite[Poses];
        for (int i = 0; i < Poses; i++) poses[i] = FabricaDeSprites.Pegar(NomeDaPose(planta, i));
        return poses;
    }

    // Quantos pixels a linha "y" anda na pose (-1 = toda para a esquerda, 1 = toda para a direita).
    // A curva (potência 1,4) faz o topo mexer bem mais que o meio, como um caule que verga.
    public static int Deslocamento(int y, int altura, float pose, float amplitude, float presa, bool deCabeca)
    {
        float subida = deCabeca ? (altura - 1 - y) : y;  // distância até a parte presa
        float parada = presa * (altura - 1);
        float peso = Mathf.Clamp01((subida - parada) / Mathf.Max(1f, altura - 1 - parada));
        return Mathf.RoundToInt(amplitude * pose * Mathf.Pow(peso, 1.4f));
    }

    static Sprite Pose(string planta, int indice)
    {
        Jeito jeito = JeitoDe(planta);
        Sprite original = FabricaDeSprites.Pegar(planta);
        Texture2D textura = original.texture;
        int largura = textura.width, altura = textura.height;
        Color32[] origem = textura.GetPixels32();
        float pose = (indice - PoseReta) / (float)PoseReta; // -1 ... 0 ... 1

        // Desenho que não dá a volta ganha uma folga dos lados (senão a ponta inclinada seria cortada).
        int folga = jeito.daVolta ? 0 : Mathf.CeilToInt(jeito.amplitude);
        int novaLargura = largura + 2 * folga;
        var pixels = new Color32[novaLargura * altura];
        for (int y = 0; y < altura; y++)
        {
            int d = Deslocamento(y, altura, pose, jeito.amplitude, jeito.presa, jeito.deCabeca);
            for (int x = 0; x < largura; x++)
            {
                Color32 c = origem[y * largura + x];
                if (c.a == 0) continue;
                int nx = x + d + folga;
                if (jeito.daVolta) nx = ((nx % largura) + largura) % largura;
                if (nx < 0 || nx >= novaLargura) continue;
                pixels[y * novaLargura + nx] = c;
            }
        }

        // O pivô continua no mesmo pixel (o pé da planta não sai do lugar), só somando a folga.
        Vector2 pivoEmPixels = original.pivot;
        var pivo = new Vector2((pivoEmPixels.x + folga) / novaLargura, pivoEmPixels.y / altura);
        Sprite sprite = FabricaDeSprites.Procedural(novaLargura, altura, pivo, (x, y) => pixels[y * novaLargura + x],
            Mathf.RoundToInt(original.pixelsPerUnit));
        // mesma filtragem do original (o capim desfocado do primeiro plano é "Bilinear"; o resto, "Point")
        sprite.texture.filterMode = textura.filterMode;
        sprite.texture.wrapModeU = textura.wrapModeU;
        sprite.texture.wrapModeV = textura.wrapModeV;
        return sprite;
    }
}
