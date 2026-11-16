using UnityEngine;

// O GRUMETE NA ÁGUA (o Mar de Baixo, e a maré que sobe e desce nas ilhas). Veja AguaDoMar em MarDeBaixo.cs.
//  - dentro d'água a gravidade cai para 20% e a queda é lenta (no máximo 3,5 blocos/s);
//  - PULAR dá uma BRAÇADA (sobe 7 blocos/s), com um tempinho entre uma e outra (0,18 s); segurar não faz nada;
//  - com a cabeça para fora d'água (na superfície), PULAR é um pulo de verdade: sai da água;
//  - anda mais devagar (5 blocos/s) e para mais devagar (a água segura);
//  - O AR: 8 segundos com a cabeça debaixo d'água. As bolhinhas em cima da cabeça mostram quanto falta
//    (uma por segundo). Acabou, morreu afogado. Respira na superfície (enche rápido) ou numa bolha de ar ('@').
// Também está aqui o "lançado" do canhão (')'): uns instantes sem controle do lado, para o arco não parar no ar.
public partial class Jogador
{
    [Header("Na água")]
    public float gravidadeNaAgua = 0.2f;     // multiplica a gravidade normal
    public float quedaNaAgua = 3.5f;
    public float velocidadeNaAgua = 5f;
    public float aceleracaoNaAgua = 40f;
    public float forcaDaBracada = 7f;
    public float esperaDaBracada = 0.18f;
    public float arMaximo = 8f;

    public bool NaAgua { get; private set; }
    public float Ar { get; private set; } = 8f;
    public bool Submerso { get; private set; }

    float timerBracada, timerLancado;
    float superficie;
    SpriteRenderer[] bolhinhas;

    // Chamado no FixedUpdate, depois de calcular a velocidade do lado. Devolve true se está nadando
    // (aí o pulo, o pulo variável e a gravidade normal não valem neste passo).
    bool Nadar(ref Vector2 v, float dt, float vento)
    {
        bool estava = NaAgua;
        NaAgua = AguaDoMar.Dentro(Corpo.position, out superficie);
        if (NaAgua != estava)
        {
            Corpo.gravityScale = NaAgua ? gravidade * gravidadeNaAgua : gravidade;
            if (NaAgua)
            {
                v.y *= 0.35f; // o "tchibum" freia
                GerenciadorDoJogo.Som("pulo", 0.25f);
                Efeitos.Poeira(transform.parent, new Vector3(Corpo.position.x, superficie, 0f), 6, 2f);
            }
        }
        Submerso = NaAgua && Corpo.position.y + 0.3f < superficie;
        AtualizarAr(dt);
        if (!NaAgua) return false;

        timerBracada -= dt;
        if (timerLancado <= 0f)
            v.x = Mathf.MoveTowards(v.x, entradaX * velocidadeNaAgua + vento, aceleracaoNaAgua * dt);
        bool naSuperficie = Corpo.position.y + 0.45f > superficie;
        if (timerBufferPulo > 0f && timerBracada <= 0f)
        {
            timerBufferPulo = 0f;
            timerBracada = esperaDaBracada;
            if (naSuperficie)
            {
                v.y = forcaDoPulo * 0.95f; // sai da água com um pulo de verdade
                pulando = true;
                GerenciadorDoJogo.Som("pulo");
            }
            else
            {
                v.y = Mathf.Max(v.y, forcaDaBracada);
                GerenciadorDoJogo.Som("pulo", 0.35f);
            }
            escala = new Vector2(0.8f, 1.2f);
            Efeitos.Poeira(transform.parent, transform.position + Vector3.down * 0.4f, 3, 1.2f);
        }
        if (v.y < -quedaNaAgua) v.y = -quedaNaAgua;
        // (perto da superfície ele boia: não afunda sozinho de volta)
        if (naSuperficie && v.y < 0f && !Submerso && entradaX == 0f) v.y = Mathf.Max(v.y, -0.5f);
        return true;
    }

    void AtualizarAr(float dt)
    {
        if (Submerso) Ar -= dt;
        else Ar = Mathf.Min(arMaximo, Ar + dt * 4f);
        if (Ar > 0f) return;
        Ar = arMaximo;
        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.Avisar(Idioma.T("Glub glub.", "Glub glub."), 1.5f);
        Conquistas.Desbloquear("afogou");
        Morrer(ignorarEscudo: true); // (o escudo não traz ar)
    }

    // Pegou uma bolha de ar: o ar enche todo.
    public void Respirar()
    {
        Ar = arMaximo;
        GerenciadorDoJogo.Som("moeda", 0.4f);
    }

    // As bolhinhas do ar em cima da cabeça (só aparecem quando está faltando ar).
    void DesenharAr()
    {
        if (bolhinhas == null)
        {
            bolhinhas = new SpriteRenderer[Mathf.CeilToInt(arMaximo)];
            for (int i = 0; i < bolhinhas.Length; i++)
            {
                var b = ConstrutorDeFase.Visual("Ar", transform, transform.position, "bolhinha_ar", 30, sombra: false).GetComponent<SpriteRenderer>();
                b.enabled = false;
                bolhinhas[i] = b;
            }
        }
        bool mostrar = !Morto && Ar < arMaximo - 0.05f;
        int cheias = Mathf.CeilToInt(Ar);
        for (int i = 0; i < bolhinhas.Length; i++)
        {
            bolhinhas[i].enabled = mostrar && i < cheias;
            if (!bolhinhas[i].enabled) continue;
            float x = (i - (bolhinhas.Length - 1) / 2f) * 0.22f;
            bolhinhas[i].transform.localPosition = new Vector3(x, 0.85f + 0.04f * Mathf.Sin(Time.time * 5f + i), 0f);
            // a última pisca quando falta pouco
            bolhinhas[i].color = Ar < 2.5f && i == cheias - 1 && (int)(Time.time * 8f) % 2 == 0 ? new Color(1f, 0.5f, 0.5f) : Color.white;
        }
    }

    // O canhão (')') joga o Grumete: a velocidade vira esta e, por um instante, o lado não é controlado.
    public void Lancar(Vector2 velocidadeDoTiro, float semControle = 0.55f)
    {
        Corpo.linearVelocity = velocidadeDoTiro;
        timerLancado = semControle;
        pulando = false;
        timerBufferPulo = 0f;
        escala = new Vector2(0.7f, 1.3f);
    }

    // Recomeçou (a fase foi montada de novo): o ar volta cheio e a física volta ao normal.
    void ZerarAgua()
    {
        NaAgua = false;
        Submerso = false;
        Ar = arMaximo;
        timerLancado = 0f;
        if (Corpo != null) Corpo.gravityScale = gravidade;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarBolhinha()
    {
        FabricaDeSprites.Registrar("bolhinha_ar", () => FabricaDeSprites.DeArte(new[]
        {
            ".ll.",
            "lwEl",
            "lEEl",
            ".ll.",
        }, FabricaDeSprites.Centro));
    }
}
