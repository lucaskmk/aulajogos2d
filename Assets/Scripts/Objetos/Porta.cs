using System.Collections;
using UnityEngine;

// 'D' - porta da Biblioteca Proibida (o "Door Crossing" da Beatrice).
// Aperte S (ou seta para baixo) na frente dela: você sai por OUTRA porta, que pode estar
// na sala da Beatrice... ou não. O ConstrutorDeFase sorteia para onde cada porta leva
// (de novo a cada morte), garantindo que sempre exista um caminho até a sala da bandeira.
public class Porta : MonoBehaviour
{
    public int sala;          // em qual sala ela fica (as salas são separadas por paredes)
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
    }

    void OnEnable()
    {
        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.portas.Add(this);
    }

    void OnDisable()
    {
        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.portas.Remove(this);
    }

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
        if (atravessando || destino == null || Time.time < podeEntrarDepoisDe) return;
        if (JogadorNaFrente() && Controles.BaixoApertou())
            StartCoroutine(Atravessar(GerenciadorDoJogo.JogadorAtual));
    }

    IEnumerator Atravessar(Jogador jogador)
    {
        atravessando = true;
        podeEntrarDepoisDe = Time.time + 0.8f;
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
        if (Random.value < 0.4f)
            GerenciadorDoJogo.Instancia.Avisar(Textos.PortaErrada[Random.Range(0, Textos.PortaErrada.Length)], 1.5f);

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
