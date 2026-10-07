using System.Collections.Generic;
using UnityEngine;

// 't' - ESMAGADOR DO RITMO: um primo do esmagador 'T' que não liga para onde você está:
//   ele segue o MESMO relógio dos blocos do ritmo (veja BlocoDoRitmo.cs).
//   Toda vez que os blocos azuis ('a') acendem, ele despenca. Fica um tempinho no chão e volta a subir,
//   e passa a vez dos blocos rosa ('b') inteira lá em cima.
//   Aviso: nos 0,35 s antes de cair (enquanto os blocos piscam), ele treme e os olhos ficam VERMELHOS.
//   Regra para decorar: "rosa aceso = esmagador em cima = pode passar".
// Encostou nele (subindo, descendo ou parado), morreu.
public class EsmagadorDoRitmo : MonoBehaviour
{
    enum Estado { EmCima, Caindo, NoChao, Subindo }

    public float gravidade = 90f;       // cai MAIS rápido que o Subaru (34): não dá para "correr junto"
    public float tempoNoChao = 0.45f;
    public float velocidadeSubida = 9f;
    public float quedaMaxima = 12f;     // se não tiver chão embaixo, para de cair depois disso (e volta)

    Estado estado = Estado.EmCima;
    Vector3 origem;
    float velocidade;
    float timer;
    int ultimaBatida;
    SpriteRenderer desenho;
    Transform visual;
    Sprite spriteCalmo, spriteBravo;
    ContactFilter2D filtroSolido;
    readonly List<RaycastHit2D> acertos = new List<RaycastHit2D>();

    void Awake()
    {
        BlocoDoRitmo.ComecarSeFaseNova();

        // O desenho fica num filho, para tremer sem mexer na área que mata.
        var filho = ConstrutorDeFase.Visual("Visual", transform, transform.position, "esmagador_ritmo", 3);
        visual = filho.transform;
        desenho = filho.GetComponent<SpriteRenderer>();
        spriteCalmo = FabricaDeSprites.Pegar("esmagador_ritmo");
        spriteBravo = FabricaDeSprites.Pegar("esmagador_ritmo_bravo");

        Perigo.Adicionar(gameObject, new Vector2(0.95f, 0.95f), Vector2.zero);
        Perigo.TornarMovel(gameObject);
        filtroSolido = new ContactFilter2D { useTriggers = false };
        origem = transform.position;
        ultimaBatida = BlocoDoRitmo.Batida;
    }

    void Update()
    {
        BlocoDoRitmo.AvancarRelogio();
        bool perto = BlocoDoRitmo.PertoDoJogador(transform.position);
        if (perto) BlocoDoRitmo.AvisarQueEstaPerto();

        // Mudou a batida e agora é a vez dos azuis? DESPENCA.
        int batida = BlocoDoRitmo.Batida;
        bool batidaNova = batida != ultimaBatida;
        ultimaBatida = batida;
        if (batidaNova && BlocoDoRitmo.VezDoA && estado == Estado.EmCima)
        {
            estado = Estado.Caindo;
            velocidade = 0f;
        }

        switch (estado)
        {
            case Estado.Caindo:
                if (Cair())
                {
                    estado = Estado.NoChao;
                    timer = tempoNoChao;
                    if (perto)
                    {
                        GerenciadorDoJogo.Som("pancada", 0.7f);
                        CameraSeguir.Tremer(0.12f, 0.15f);
                        Efeitos.Poeira(transform.parent, transform.position + Vector3.down * 0.5f, 6, 3f);
                    }
                }
                break;

            case Estado.NoChao:
                timer -= Time.deltaTime;
                if (timer <= 0f) estado = Estado.Subindo;
                break;

            case Estado.Subindo:
                transform.position = Vector3.MoveTowards(transform.position, origem, velocidadeSubida * Time.deltaTime);
                if (transform.position == origem) estado = Estado.EmCima;
                break;
        }

        if (GerenciadorDoJogo.Pausado) return; // na pausa o desenho congela (nada de tremer)

        // Aviso: lá em cima, na vez dos rosa, quando falta pouco para os azuis acenderem.
        bool avisando = estado == Estado.EmCima && !BlocoDoRitmo.VezDoA && BlocoDoRitmo.TempoParaTrocar < BlocoDoRitmo.TempoDoAviso;
        bool bravo = avisando || estado == Estado.Caindo || estado == Estado.NoChao;
        desenho.sprite = bravo ? spriteBravo : spriteCalmo;
        visual.localPosition = avisando ? (Vector3)(Random.insideUnitCircle * 0.06f) : Vector3.zero;
    }

    // Um passo de queda com gravidade. Devolve true quando encostou no chão (ou caiu o máximo).
    bool Cair()
    {
        velocidade += gravidade * Time.deltaTime;
        float passo = velocidade * Time.deltaTime;
        float distancia = Mathf.Min(DistanciaAteOChao(), transform.position.y - (origem.y - quedaMaxima));
        if (distancia <= passo)
        {
            transform.position += Vector3.down * Mathf.Max(0f, distancia);
            velocidade = 0f;
            return true;
        }
        transform.position += Vector3.down * passo;
        return false;
    }

    // Igual ao do esmagador normal: o Subaru e os bichos não contam como chão.
    // (os blocos do ritmo apagados também não: o colisor deles fica desligado)
    float DistanciaAteOChao()
    {
        Vector2 baseDoBloco = (Vector2)transform.position + Vector2.down * 0.5f;
        int quantidade = Physics2D.Raycast(baseDoBloco, Vector2.down, filtroSolido, acertos, 30f);
        float menor = 100f;
        for (int i = 0; i < quantidade; i++)
        {
            Collider2D outro = acertos[i].collider;
            if (outro.GetComponent<Jogador>() != null || Inimigo.EhBicho(outro)) continue;
            menor = Mathf.Min(menor, acertos[i].distance);
        }
        return menor;
    }

    // ------------------------------------------------------------------ desenhos

    // Bloco de pedra roxa com uma nota na testa e dentes embaixo. Bravo: olhos vermelhos e a nota acesa.
    static readonly string[] ArteCalmo =
    {
        "kkkkkkkkkkkkkkkk",
        "kPPPPPPPPPPPPPVk",
        "kPVVVVVwwVVVVVvk",
        "kPVVVVVwVwVVVVvk",
        "kPVVVVwwVVVVVVvk",
        "kPVVVVwwVVVVVVvk",
        "kPVkkkkVVVkkkkvk",
        "kPVVkwkVVVkwkVvk",
        "kPVVkkkVVVkkkVvk",
        "kPVVVVVVVVVVVVvk",
        "kPVVkkkkkkkkVVvk",
        "kPVVkwkwkwkwkVvk",
        "kPVVVkkkkkkkVVvk",
        "kvvvvvvvvvvvvvvk",
        "kSkkSkkSkkSkkSkk",
        ".k..k..k..k..k..",
    };

    static string[] Bravo()
    {
        // olhos vermelhos (o 'w' dos olhos vira 'r') e a nota da testa fica amarela
        var arte = (string[])ArteCalmo.Clone();
        arte[7] = "kPVVkrkVVVkrkVvk";
        for (int linha = 2; linha <= 5; linha++) arte[linha] = arte[linha].Replace('w', 'y');
        return arte;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("esmagador_ritmo", () => FabricaDeSprites.DeArte(ArteCalmo, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("esmagador_ritmo_bravo", () => FabricaDeSprites.DeArte(Bravo(), FabricaDeSprites.Centro));
    }
}
