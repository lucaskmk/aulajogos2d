using System.Collections.Generic;
using UnityEngine;

// O VENTO nas plantas: capim, flores, arbustos e árvores balançam trocando de POSE (os desenhos de
// ArteDoVento). O topo mexe mais, a base fica parada, e o balanço passa pela fase como uma ONDA:
// a fase da onda vem da posição x da planta, então as plantas vizinhas balançam quase juntas e as
// distantes não (parece uma rajada passando pelo capim, como em Stardew Valley).
//
// Um Vento só cuida de TODAS as plantas de uma fase (ou do mapa): um Update para centenas de plantas
// é bem mais leve que centenas de Updates. E ele só troca o desenho quando a pose muda.
// Ele fica preso no objeto "raiz" que recebe (a decoração da fase, o mapa...): quando a fase é
// destruída, o Vento vai junto.
public class Vento : MonoBehaviour
{
    struct Planta
    {
        public SpriteRenderer desenho, sombra; // a sombra (se tiver) troca de pose junto
        public Sprite[] poses;
        public float x;                        // onde ela está (a fase da onda)
        public float rigidez;                  // árvore grande mexe menos que capim
        public int pose;                       // a pose que está na tela agora
    }

    readonly List<Planta> plantas = new List<Planta>();

    // Faz o desenho balançar no vento. "planta" é o nome do sprite (precisa ter poses em ArteDoVento).
    // xFixo: usa este x em vez da posição (o primeiro plano anda com a câmera, mas balança todo junto).
    public static void Plantar(SpriteRenderer desenho, string planta, Transform raiz, float? xFixo = null, float rigidez = 1f)
    {
        if (desenho == null || raiz == null || !ArteDoVento.TemVento(planta)) return;
        if (!raiz.TryGetComponent(out Vento vento)) vento = raiz.gameObject.AddComponent<Vento>();

        // A sombra deixa de se copiar sozinha: o Vento troca o desenho dela junto com o da planta.
        Sombra sombra = desenho.GetComponentInChildren<Sombra>();
        if (sombra != null) sombra.Parar();

        vento.plantas.Add(new Planta
        {
            desenho = desenho,
            sombra = sombra != null ? sombra.Desenho : null,
            poses = ArteDoVento.TodasAsPoses(planta),
            x = xFixo ?? desenho.transform.position.x,
            rigidez = rigidez,
            pose = -1,
        });
    }

    // Quanto a planta em "x" está inclinada agora: -1 (toda para a esquerda) a 1 (toda para a direita).
    // É pública para o primeiro plano e quem mais quiser balançar no mesmo ritmo.
    public static float Balanco(float x, float tempo)
    {
        // Rajadas: um "ruído" bem lento diz onde está ventando mais (e ele também anda para a direita).
        float rajada = 0.35f + 0.65f * Mathf.PerlinNoise(tempo * 0.18f - x * 0.025f, 0.37f);
        // A onda: o vento sopra da esquerda para a direita (a crista anda para +x com o tempo).
        float onda = Mathf.Sin(tempo * 2.1f - x * 0.45f) + 0.3f * Mathf.Sin(tempo * 3.7f - x * 0.9f + 1.3f);
        // As plantas tombam mais a favor do vento (direita) do que contra ele.
        return Mathf.Clamp(rajada * (0.3f + 0.6f * onda), -1f, 1f);
    }

    void Update()
    {
        float forca = AjustesVisuais.vento ? AjustesVisuais.forcaDoVento : 0f;
        float tempo = Time.time;
        for (int i = 0; i < plantas.Count; i++)
        {
            Planta p = plantas[i];
            if (p.desenho == null) continue; // a planta foi destruída (ex.: junto com um bloco)

            float inclinacao = Mathf.Clamp(Balanco(p.x, tempo) * forca * p.rigidez, -1f, 1f);
            int pose = Mathf.RoundToInt((inclinacao + 1f) * 0.5f * (ArteDoVento.Poses - 1));
            if (pose == p.pose) continue; // a mesma pose de antes: não precisa mexer em nada

            p.pose = pose;
            p.desenho.sprite = p.poses[pose];
            if (p.sombra != null) p.sombra.sprite = p.poses[pose];
            plantas[i] = p; // (Planta é uma struct: precisa guardar a cópia alterada de volta)
        }
    }
}
