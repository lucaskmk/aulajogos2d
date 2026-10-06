using System.Collections;
using UnityEngine;

// '0' a '9' - porta da Biblioteca Proibida (o "Door Crossing" da Beatrice), com o número em cima.
// Aperte S (ou seta para baixo) na frente dela: você sai pela porta par dela (lista "portas" da fase).
// É sempre o mesmo caminho, então dá para decorar. Depois de atravessar, espera um pouquinho
// antes de poder entrar de novo (senão você atravessava sem querer, ida e volta).
public class Porta : MonoBehaviour
{
    public const float Espera = 1f; // segundos depois de atravessar até poder entrar de novo

    public char numero;
    public Porta destino;

    SpriteRenderer desenho;
    Sprite fechada, aberta;
    bool atravessando;
    static float podeEntrarDepoisDe; // não deixa entrar de novo logo que saiu

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => podeEntrarDepoisDe = 0f;

    void Awake()
    {
        fechada = FabricaDeSprites.Pegar("porta");
        aberta = FabricaDeSprites.Pegar("porta_aberta");
        desenho = ConstrutorDeFase.Visual("Visual", transform, transform.position, "porta", -1).GetComponent<SpriteRenderer>();
        Luzes.Ponto(transform, Luzes.Lilas, 2.2f, 0.55f, Vector3.up * 1.2f);
    }

    void OnEnable()
    {
        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.portas.Add(this);
    }

    void OnDisable()
    {
        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.portas.Remove(this);
    }

    // Escreve o número em cima da porta.
    public void Numerar(char numeroDaPorta)
    {
        numero = numeroDaPorta;
        var placa = new GameObject("Numero");
        placa.transform.SetParent(transform, false);
        placa.transform.position = transform.position + Vector3.up * 2.35f;
        var desenhoDoNumero = placa.AddComponent<SpriteRenderer>();
        desenhoDoNumero.sprite = FabricaDeSprites.SpriteDeTexto(numero.ToString(), new Color32(255, 222, 90, 255), new Color32(20, 20, 28, 255));
        desenhoDoNumero.sortingOrder = 1;
        placa.transform.localScale = Vector3.one * 1.3f;
    }

    // Já dá para entrar de novo? (a Interface só mostra a dica quando pode)
    public static bool PodeEntrar => Time.time >= podeEntrarDepoisDe;

    // O jogador está parado na frente desta porta?
    public bool JogadorNaFrente()
    {
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (jogador == null || jogador.Morto || !GerenciadorDoJogo.JogadorVivo(out Vector2 posicao)) return false;
        Vector2 d = posicao - (Vector2)transform.position; // a posição da porta é o chão, embaixo dela
        return Mathf.Abs(d.x) < 0.6f && d.y > 0f && d.y < 1.5f && jogador.NoChao;
    }

    void Update()
    {
        if (atravessando || destino == null || !PodeEntrar) return;
        if (JogadorNaFrente() && Controles.BaixoApertou())
            StartCoroutine(Atravessar(GerenciadorDoJogo.JogadorAtual));
    }

    IEnumerator Atravessar(Jogador jogador)
    {
        atravessando = true;
        podeEntrarDepoisDe = Time.time + 0.3f + Espera; // 0,3 s atravessando + a espera
        jogador.Congelar(); // ninguém te mata dentro da porta
        desenho.sprite = aberta;
        GerenciadorDoJogo.Som("porta");
        yield return new WaitForSeconds(0.3f);

        destino.Abrir();
        jogador.Liberar();
        jogador.Teletransportar(destino.transform.position + Vector3.up * 0.45f);
        CameraSeguir.Centralizar();
        Efeitos.Poeira(destino.transform.parent, destino.transform.position + Vector3.up * 0.5f, 6, 2f);
        Conquistas.Contar("portas", 20);
        GerenciadorDoJogo.Instancia.Avisar($"Porta {numero} -> porta {destino.numero}", 1.8f); // para ajudar a decorar

        yield return new WaitForSeconds(0.2f);
        desenho.sprite = fechada;
        atravessando = false;
    }

    // A porta de chegada abre um pouquinho (você "sai" dela).
    public void Abrir() => StartCoroutine(AbrirUmPouco());

    IEnumerator AbrirUmPouco()
    {
        desenho.sprite = aberta;
        yield return new WaitForSeconds(0.4f);
        desenho.sprite = fechada;
    }
}
