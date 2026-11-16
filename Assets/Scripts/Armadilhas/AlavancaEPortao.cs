using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// COOP: a ALAVANCA e o PORTÃO (e o BAÚ DA TRIPULAÇÃO atrás dele). As letras:
//  /  ALAVANCA: uma placa no chão. Enquanto um Grumete está em cima dela, TODOS os portões da fase ficam abertos.
//  +  PORTÃO: blocos '+' encostados = um portão (grade de ferro). Fecha 0,4 s depois que a placa fica vazia,
//     e fecha com força: quem estiver no meio dele morre. A cilada: o da alavanca "sem querer" sai de cima.
//  ;  BAÚ DA TRIPULAÇÃO: encostou, abre e dá 10 moedas direto no bolso (uma vez por tentativa).
// Sozinho não dá para segurar a alavanca e passar: por isso o portão nunca fica no caminho da bandeira, só
// na frente de salas de bônus (moedas que não contam para a medalha da fase).
public class Alavanca : MonoBehaviour
{
    static readonly List<Alavanca> todas = new List<Alavanca>();
    public static bool AlgumaApertada
    {
        get
        {
            foreach (Alavanca a in todas) if (a != null && a.apertada) return true;
            return false;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => todas.Clear();

    SpriteRenderer desenho;
    bool apertada;

    void Awake()
    {
        desenho = gameObject.AddComponent<SpriteRenderer>();
        desenho.sprite = FabricaDeSprites.Pegar("alavanca");
        desenho.sortingOrder = 2;
        todas.Add(this);
    }

    void OnDestroy() => todas.Remove(this);

    void Update()
    {
        bool antes = apertada;
        apertada = false;
        foreach (Jogador j in GerenciadorDoJogo.Jogadores)
        {
            if (j == null || j.Morto || j.NaBolha) continue;
            Vector2 p = j.transform.position;
            if (Mathf.Abs(p.x - transform.position.x) < 0.6f && p.y > transform.position.y - 0.2f && p.y < transform.position.y + 0.9f) apertada = true;
        }
        if (apertada != antes) GerenciadorDoJogo.Som(apertada ? "tic" : "tac", 0.6f);
        desenho.sprite = FabricaDeSprites.Pegar(apertada ? "alavanca_apertada" : "alavanca");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("alavanca", () => FabricaDeSprites.DeArte(new[]
        {
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            "..kkkkkkkkkkkk..",
            ".kyyyyyyyyyyyyk.",
            ".kooooooooooook.",
            "kSSSSSSSSSSSSSSk",
            "kkkkkkkkkkkkkkkk",
        }, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("alavanca_apertada", () => FabricaDeSprites.DeArte(new[]
        {
            "................", "................", "................", "................", "................",
            "................", "................", "................", "................", "................",
            "................", "................", "................",
            ".kkkkkkkkkkkkkk.",
            "kSgggggggggggSSk",
            "kkkkkkkkkkkkkkkk",
        }, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("portao", () => FabricaDeSprites.DeArte(new[]
        {
            "kSkkSkkSkkSkkSkk",
            "kSkkSkkSkkSkkSkk",
            "kSSSSSSSSSSSSSSk",
            "kSkkSkkSkkSkkSkk",
            "kSkkSkkSkkSkkSkk",
            "kSkkSkkSkkSkkSkk",
            "kSkkSkkSkkSkkSkk",
            "kSSSSSSSSSSSSSSk",
            "kSkkSkkSkkSkkSkk",
            "kSkkSkkSkkSkkSkk",
            "kSkkSkkSkkSkkSkk",
            "kSkkSkkSkkSkkSkk",
            "kSSSSSSSSSSSSSSk",
            "kSkkSkkSkkSkkSkk",
            "kSkkSkkSkkSkkSkk",
            "kSkkSkkSkkSkkSkk",
        }, FabricaDeSprites.Centro));
    }
}

public class Portao : MonoBehaviour
{
    readonly List<Transform> grades = new List<Transform>();
    readonly List<Collider2D> colisores = new List<Collider2D>();
    readonly List<Vector3> fechado = new List<Vector3>();
    float abertura;      // 0 = fechado, 1 = aberto
    float timerFechar;
    float baixo = float.MaxValue, cima = float.MinValue, esquerda = float.MaxValue, direita = float.MinValue;

    public void AdicionarGrade(Vector3 posicao)
    {
        var grade = ConstrutorDeFase.Visual("Grade", transform, posicao, "portao", 3);
        var colisor = grade.AddComponent<BoxCollider2D>();
        colisor.size = Vector2.one;
        grades.Add(grade.transform);
        colisores.Add(colisor);
        fechado.Add(posicao);
        baixo = Mathf.Min(baixo, posicao.y - 0.5f);
        cima = Mathf.Max(cima, posicao.y + 0.5f);
        esquerda = Mathf.Min(esquerda, posicao.x - 0.5f);
        direita = Mathf.Max(direita, posicao.x + 0.5f);
    }

    void Update()
    {
        bool abrir = Alavanca.AlgumaApertada;
        if (abrir) timerFechar = 0.4f;
        else timerFechar -= Time.deltaTime;
        float alvo = abrir || timerFechar > 0f ? 1f : 0f;
        float antes = abertura;
        abertura = Mathf.MoveTowards(abertura, alvo, (alvo > abertura ? 3f : 9f) * Time.deltaTime);
        float subida = abertura * (cima - baixo + 0.1f);
        for (int i = 0; i < grades.Count; i++)
        {
            grades[i].position = fechado[i] + Vector3.up * subida;
            colisores[i].enabled = abertura < 0.5f;
        }
        // fechou de vez agora: quem estava no meio do portão é esmagado
        if (antes > 0.05f && abertura <= 0.05f)
        {
            GerenciadorDoJogo.Som("pancada", 0.8f);
            CameraSeguir.Tremer(0.1f, 0.15f);
            foreach (Jogador j in GerenciadorDoJogo.Jogadores)
            {
                if (j == null || j.Morto) continue;
                Vector2 p = j.transform.position;
                if (p.x > esquerda - 0.25f && p.x < direita + 0.25f && p.y > baixo && p.y < cima) j.Morrer();
            }
        }
    }
}

public class BauDaTripulacao : MonoBehaviour
{
    bool aberto;

    void Awake()
    {
        var desenho = gameObject.AddComponent<SpriteRenderer>();
        desenho.sprite = FabricaDeSprites.Pegar("mapa_bau");
        desenho.sortingOrder = 2;
        // (do tamanho de um bloco: cabe na salinha do portão)
        Luzes.Ponto(transform, Luzes.Dourada, 2f, 0.5f, Vector3.up * 0.5f);
    }

    void Update()
    {
        if (aberto) return;
        for (int i = 0; GerenciadorDoJogo.ProximoVivo(ref i, out Vector2 p);)
            if (Mathf.Abs(p.x - transform.position.x) < 0.9f && Mathf.Abs(p.y - (transform.position.y + 0.5f)) < 0.9f)
            {
                aberto = true;
                GerenciadorDoJogo.Som("conquista", 0.6f);
                for (int k = 0; k < 5; k++) StartCoroutine(Efeitos.MoedaSaltando(transform.parent, transform.position + new Vector3(-0.8f + k * 0.4f, 1f, 0f)));
                GerenciadorDoJogo.Instancia?.GanharMoedasDoBau(10);
                GerenciadorDoJogo.Instancia?.Avisar(Idioma.T("O BAÚ DA TRIPULAÇÃO! +10 moedas", "THE CREW CHEST! +10 coins"), 2f);
                GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.6f, 0.6f);
                return;
            }
    }
}
