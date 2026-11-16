using System.Collections.Generic;
using UnityEngine;

// O REPLAY DAS MORTES: no fim da fase, todas as tentativas correm JUNTAS, como fantasmas, e cada uma
// morre no mesmo lugar em que você morreu (com um X e um "puf"). A última, a que passou, vai até a bandeira.
// É a tela que mais vira vídeo de streamer: 40 Grumetes pulando no mesmo espinho ao mesmo tempo.
//
// Duas partes:
//  - GravadorDeTentativas (estático): enquanto você joga, guarda o caminho de cada Grumete, 30 vezes por segundo
//    (o lugar do desenho, o sprite, para que lado olha e o "estica e achata"). Uma tentativa vai do renascer à morte.
//    No coop, quem vira balão fecha o rastro dele; quem sai do balão começa outro, na mesma tentativa.
//  - ReplayDasMortes (o componente): no fim da fase cria um fantasma por rastro e toca tudo junto, acelerado
//    para caber em uns 9 segundos. A câmera segue o fantasma da tentativa que passou.
public static class GravadorDeTentativas
{
    public const float Intervalo = 1f / 30f;
    const int MaximoDeTentativas = 40;           // as mais velhas saem (40 fantasmas já lotam a tela)
    const int MaximoDeQuadros = 30 * 60 * 4;     // 4 minutos por rastro

    public struct Quadro
    {
        public Vector3 lugar;
        public Vector2 escala;
        public Sprite sprite;
        public bool olhaEsquerda;
    }

    public class Rastro
    {
        public int jogador, tentativa;
        public float inicio;                 // em que segundo da tentativa ele começou
        public readonly List<Quadro> quadros = new List<Quadro>();
        public bool aberto = true, morreu, passou;
        public float Duracao => quadros.Count * Intervalo;
    }

    public static readonly List<Rastro> Rastros = new List<Rastro>();
    public static int Tentativas { get; private set; }
    static readonly Rastro[] abertos = new Rastro[Tripulacao.Maximo];
    static float tempo, acumulado;

    // Entrou na fase pelo mapa: apaga tudo.
    public static void ComecarFase()
    {
        Rastros.Clear();
        System.Array.Clear(abertos, 0, abertos.Length);
        Tentativas = 0;
        NovaTentativa();
    }

    // Renasceu: o que estava aberto morreu ali.
    public static void NovaTentativa()
    {
        FecharTodos(morreu: true);
        Tentativas++;
        tempo = 0f;
        acumulado = Intervalo; // grava o primeiro quadro já no começo
        // só as últimas tentativas ficam
        int velha = Tentativas - MaximoDeTentativas;
        if (velha > 0) Rastros.RemoveAll(r => r.tentativa <= velha);
    }

    // Passou da fase: quem estava vivo passou.
    public static void Concluir()
    {
        for (int i = 0; i < abertos.Length; i++)
        {
            if (abertos[i] == null) continue;
            abertos[i].aberto = false;
            abertos[i].passou = true;
            abertos[i] = null;
        }
    }

    static void FecharTodos(bool morreu)
    {
        for (int i = 0; i < abertos.Length; i++)
        {
            if (abertos[i] == null) continue;
            abertos[i].aberto = false;
            abertos[i].morreu = morreu;
            abertos[i] = null;
        }
    }

    // Chamado a cada quadro enquanto o estado é "Jogando".
    public static void Gravar(List<Jogador> jogadores, float dt)
    {
        tempo += dt;
        acumulado += dt;
        if (acumulado < Intervalo) return;
        acumulado -= Intervalo;
        if (acumulado > Intervalo) acumulado = 0f; // (quadro muito lento: não tenta "alcançar")

        foreach (Jogador j in jogadores)
        {
            if (j == null) continue;
            int i = Mathf.Clamp(j.Indice, 0, abertos.Length - 1);
            if (j.Morto)
            {
                // virou balão (ou morreu): o rastro dele acaba aqui
                if (abertos[i] != null)
                {
                    abertos[i].aberto = false;
                    abertos[i].morreu = true;
                    abertos[i] = null;
                }
                continue;
            }
            if (abertos[i] == null)
            {
                abertos[i] = new Rastro { jogador = j.Indice, tentativa = Tentativas, inicio = tempo };
                Rastros.Add(abertos[i]);
            }
            Rastro r = abertos[i];
            if (r.quadros.Count >= MaximoDeQuadros) continue;
            SpriteRenderer desenho = j.Desenho;
            r.quadros.Add(new Quadro
            {
                lugar = desenho != null ? desenho.transform.position : j.transform.position,
                escala = desenho != null ? (Vector2)desenho.transform.localScale : Vector2.one,
                sprite = desenho != null ? desenho.sprite : null,
                olhaEsquerda = desenho != null && desenho.flipX,
            });
        }
    }

    // Tem o que mostrar? (pelo menos uma morte gravada)
    public static bool TemMortes
    {
        get
        {
            foreach (Rastro r in Rastros)
                if (r.morreu && r.quadros.Count > 3) return true;
            return false;
        }
    }
}

public class ReplayDasMortes : MonoBehaviour
{
    const float DuracaoAlvo = 9f;   // o replay inteiro cabe em uns 9 s
    const float VelocidadeMinima = 1.5f;
    const float Folga = 0.8f;       // depois do último fantasma, um respiro antes do painel

    class Fantasma
    {
        public GravadorDeTentativas.Rastro rastro;
        public SpriteRenderer desenho;
        public bool acabou;
    }

    readonly List<Fantasma> fantasmas = new List<Fantasma>();
    Transform alvoDaCamera;
    Fantasma seguido;
    float tempo, velocidade, fim;
    int sonsNesteQuadro;

    public static int MortesNoReplay { get; private set; }
    public float Duracao => fim / velocidade + Folga;
    public bool Acabou => tempo >= fim + Folga * velocidade;

    // Cria o replay das tentativas gravadas. Devolve null se não tem morte nenhuma para mostrar.
    public static ReplayDasMortes Criar(Transform pai)
    {
        if (!GravadorDeTentativas.TemMortes) return null;
        var objeto = new GameObject("ReplayDasMortes");
        objeto.transform.SetParent(pai, false);
        var replay = objeto.AddComponent<ReplayDasMortes>();
        replay.Montar();
        return replay;
    }

    void Montar()
    {
        MortesNoReplay = 0;
        fim = 0f;
        foreach (GravadorDeTentativas.Rastro r in GravadorDeTentativas.Rastros)
        {
            if (r.quadros.Count < 2) continue;
            var objeto = new GameObject("Fantasma");
            objeto.transform.SetParent(transform, false);
            var desenho = objeto.AddComponent<SpriteRenderer>();
            desenho.sortingOrder = r.passou ? 12 : 9;
            desenho.color = r.passou ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.45f);
            desenho.enabled = false;
            var fantasma = new Fantasma { rastro = r, desenho = desenho };
            fantasmas.Add(fantasma);
            if (r.morreu) MortesNoReplay++;
            fim = Mathf.Max(fim, r.inicio + r.Duracao);
            // a câmera segue quem passou (o P1, se for coop); sem ninguém que passou, o mais longo
            if (seguido == null || (r.passou && (!seguido.rastro.passou || r.jogador < seguido.rastro.jogador))
                || (!seguido.rastro.passou && !r.passou && r.Duracao > seguido.rastro.Duracao))
                seguido = fantasma;
        }
        velocidade = Mathf.Max(VelocidadeMinima, fim / DuracaoAlvo);
        alvoDaCamera = new GameObject("AlvoDoReplay").transform;
        alvoDaCamera.SetParent(transform, false);
        CameraSeguir.SeguirNoReplay(alvoDaCamera);
        Atualizar();
    }

    // Pula para o fim (o jogador apertou ENTER).
    public void Pular()
    {
        tempo = fim + Folga * velocidade;
        Atualizar();
    }

    void Update()
    {
        tempo += Time.deltaTime * velocidade;
        Atualizar();
    }

    void Atualizar()
    {
        sonsNesteQuadro = 0;
        foreach (Fantasma f in fantasmas)
        {
            GravadorDeTentativas.Rastro r = f.rastro;
            float local = tempo - r.inicio;
            if (local < 0f) { f.desenho.enabled = false; continue; }
            int i = Mathf.FloorToInt(local / GravadorDeTentativas.Intervalo);
            if (i >= r.quadros.Count)
            {
                if (!f.acabou)
                {
                    f.acabou = true;
                    GravadorDeTentativas.Quadro ultimo = r.quadros[r.quadros.Count - 1];
                    if (r.morreu) Morreu(ultimo.lugar);
                    // quem passou fica parado no fim (na bandeira); quem morreu some
                    f.desenho.enabled = r.passou;
                }
                if (f == seguido) alvoDaCamera.position = r.quadros[r.quadros.Count - 1].lugar;
                continue;
            }
            // entre dois quadros: desliza (fica liso mesmo acelerado)
            GravadorDeTentativas.Quadro a = r.quadros[i];
            GravadorDeTentativas.Quadro b = r.quadros[Mathf.Min(i + 1, r.quadros.Count - 1)];
            float t = local / GravadorDeTentativas.Intervalo - i;
            f.desenho.enabled = a.sprite != null;
            f.desenho.sprite = a.sprite;
            f.desenho.flipX = a.olhaEsquerda;
            f.desenho.transform.position = Vector3.Lerp(a.lugar, b.lugar, t);
            f.desenho.transform.localScale = new Vector3(a.escala.x, a.escala.y, 1f);
            if (f == seguido) alvoDaCamera.position = f.desenho.transform.position;
        }
    }

    void Morreu(Vector3 lugar)
    {
        Efeitos.Poeira(transform, lugar, 6, 2.2f);
        var x = ConstrutorDeFase.Visual("XDoReplay", transform, lugar, "marca_x", 13, sombra: false);
        x.transform.localScale = Vector3.one * 0.55f;
        x.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));
        // (40 mortes no mesmo quadro fariam um barulhão: no máximo 2 sons por quadro, bem baixinhos)
        if (sonsNesteQuadro++ < 2) GerenciadorDoJogo.Som("pancada", 0.18f);
    }

    void OnDestroy()
    {
        CameraSeguir.SeguirNoReplay(null);
    }
}
