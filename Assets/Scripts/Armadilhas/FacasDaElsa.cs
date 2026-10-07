using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 'l' - AS FACAS DA ELSA, a Caçadora de Entranhas (a assassina de preto que mata o Subaru logo no começo do anime).
//       Ela fica escondida nas sombras: de longe você só vê DOIS OLHOS que brilham e somem.
//       Quando o Subaru chega a menos de 14 blocos na horizontal e mais ou menos na mesma altura,
//       ela arremessa uma faca (uma kukri, a faca curvada dela) em linha reta, mirando nele, a cada 1,6 s.
//  - O AVISO: antes de cada arremesso os olhos ficam VERMELHOS e brilham forte, ela sorri e a faca aparece
//    na mão dela, já apontando para você (0,4 s). Ela continua mirando durante o aviso: para onde a faca
//    aponta no fim do aviso é para onde ela vai. E ela não mira muito para cima nem para baixo (no máximo 35 graus).
//  - A faca tem LUZ PRÓPRIA: no escuro dá para ver ela vindo de longe (e ela ilumina o caminho por onde passa).
//  - A faca crava (e some) quando bate em algo sólido: chão, parede, caixote... Esconder atrás de um bloco funciona!
//  - A Elsa em si não mata (é só uma sombra; dá para passar por ela). Quem mata é a faca.
//  - A faca mira no MEIO do Subaru: um pulo passa por cima dela.
//
// 'k' - IGUALZINHA... até arremessar. Essa joga TRÊS facas de uma vez, em LEQUE: uma em você,
//       uma mais para cima e uma mais para baixo. O pulão que salvava da 'l' te leva direto para a faca de cima. :)
//       (de longe, um pulinho curto passa entre elas; de perto, só um pulo bem alto passa por cima de todas)
//       O aviso dela é um pouco mais longo (0,55 s) e dá para ver as TRÊS facas na mão dela.
//       Ela também enxerga mais para cima e para baixo: colocada no alto, ela joga o leque para BAIXO,
//       em qualquer ângulo. Quem fica parado embaixo dela vira alfineteiro. Não pare!
//
// Como funciona: a Elsa é um objeto só com desenho (sem colisão). As facas que ela joga são objetos
// separados (filhos da fase, com um Perigo cada), mas quem move e quebra as facas é ela mesma, aqui no Update.
// A fase é recriada a cada morte, então as facas no ar somem junto e ela recomeça do zero.
public class FacasDaElsa : MonoBehaviour
{
    enum Estado { Espreitando, Cacando, Avisando }
    enum EstadoDaFaca { NaMao, Voando, Cravada }

    [Header("Quando ela ataca")]
    public float alcanceHorizontal = 14f;
    public float alcanceVertical = 2.5f;      // "mais ou menos na mesma altura" (diferença entre os centros)
    public float tempoEntreFacas = 1.6f;
    public float tempoDeAviso = 0.4f;
    public float demoraParaPerceber = 0.25f;  // entre ela te ver e começar o primeiro aviso

    [Header("A faca")]
    public float velocidadeDaFaca = 10f;
    public float anguloMaximo = 35f;          // o quanto ela mira para cima ou para baixo (em graus)
    public float distanciaMaxima = 40f;       // depois disso a faca some sozinha
    public float tempoCravada = 0.4f;         // a faca fica cravada na parede esse tempo, apagando

    [Header("Leque ('k')")]
    public bool leque;
    public int facasNoLeque = 3;
    public float aberturaDoLeque = 20f;       // graus entre uma faca e a próxima

    // Medidas do desenho (16 pixels = 1 bloco).
    const float AlturaDaSilhueta = 0.3f;      // a silhueta sobe um pouco: a "mão" (de onde sai a faca) fica no meio do bloco
    const float AlturaDosOlhos = AlturaDaSilhueta + 3.5f / 16f;
    const float AlturaDoSorriso = AlturaDaSilhueta + 1f / 16f;
    const float DistanciaDaMao = 0.55f;       // onde a faca aparece, a partir do meio do bloco
    const float PontaDaFaca = 0.5f;           // do meio da faca até a ponta (é a ponta que crava)
    const float PassoMaximo = 0.25f;          // a faca anda em passinhos (não atravessa parede fina nem com o jogo travando)
    static readonly Vector2 TamanhoDaAreaQueMata = new Vector2(0.7f, 0.24f); // um pouco menor que o desenho (é mais justo)
    static readonly Vector2 CentroDaAreaQueMata = new Vector2(0.2f, 0f);     // a lâmina fica na frente do cabo

    // (públicas: a luta contra a Elsa, em ElsaCacadora.cs, usa as mesmas cores)
    public static readonly Color CorDosOlhos = new Color(0.8f, 0.5f, 1f);       // lilás (os olhos dela de longe)
    public static readonly Color CorDoAviso = new Color(1f, 0.25f, 0.35f);      // vermelho: vai jogar!
    public static readonly Color CorDaFaca = new Color(1f, 0.45f, 0.5f);        // a luz da lâmina (diferente dos vaga-lumes e das moedas)
    public static readonly Color CorDoRastro = new Color(1f, 0.65f, 0.7f, 0.6f);
    public static readonly Color CorDaFaisca = new Color(1f, 0.95f, 0.7f);

    // Uma faca: na mão da Elsa (durante o aviso), voando, ou cravada em algo (apagando).
    class Faca
    {
        public Transform objeto;
        public SpriteRenderer desenho;
        public Perigo perigo;
        public Light2D luz;          // null nas fases claras
        public float intensidadeDaLuz;
        public float desvio;         // ângulo dela dentro do leque (0 = a do meio)
        public Vector2 direcao;
        public float percorrido;
        public float timer;          // cravada: quanto falta para sumir
        public float timerRastro;    // voando: quanto falta para soltar o próximo pontinho do rastro
        public EstadoDaFaca estado;
    }

    Estado estado = Estado.Espreitando;
    float recarga;                   // tempo até o próximo arremesso
    Vector2 alvo;                    // onde ela viu o Subaru pela última vez
    bool jaRiu;                      // a 'k' ri na primeira vez que joga o leque
    float abertura;                  // 0 = olhos fechados (sumiu no escuro), 1 = olhos abertos

    Transform silhueta;
    SpriteRenderer desenhoDaSilhueta, desenhoDosOlhos, desenhoDoSorriso;
    Sprite olhosNormais, olhosBravos;
    Light2D luzDosOlhos;             // null nas fases claras
    float intensidadeDosOlhos;
    readonly List<Faca> facas = new List<Faca>();
    ContactFilter2D filtroSolido;
    readonly List<Collider2D> encostados = new List<Collider2D>();

    void Awake()
    {
        // A silhueta: a Elsa de capa preta, sumindo nas sombras (atrás dos blocos e na frente do cenário).
        var objetoDaSilhueta = ConstrutorDeFase.Visual("Elsa", transform, transform.position + Vector3.up * AlturaDaSilhueta, "elsa_sombra", -6, false);
        silhueta = objetoDaSilhueta.transform;
        desenhoDaSilhueta = objetoDaSilhueta.GetComponent<SpriteRenderer>();

        // Os olhos e o sorriso ficam na frente de tudo: no escuro, é só isso que você vê.
        olhosNormais = FabricaDeSprites.Pegar("olhos_elsa");
        olhosBravos = FabricaDeSprites.Pegar("olhos_elsa_bravos");
        var objetoDosOlhos = ConstrutorDeFase.Visual("Olhos", transform, transform.position + Vector3.up * AlturaDosOlhos, "olhos_elsa", 6, false);
        desenhoDosOlhos = objetoDosOlhos.GetComponent<SpriteRenderer>();
        desenhoDoSorriso = ConstrutorDeFase.Visual("Sorriso", transform, transform.position + Vector3.up * AlturaDoSorriso, "sorriso_elsa", 6, false).GetComponent<SpriteRenderer>();
        desenhoDoSorriso.enabled = false;

        // A luz dos olhos (só existe no escuro). Guardamos a intensidade "cheia" para poder aumentar e diminuir.
        luzDosOlhos = Luzes.Ponto(objetoDosOlhos.transform, CorDosOlhos, 1.2f, 1f);
        if (luzDosOlhos != null) intensidadeDosOlhos = luzDosOlhos.intensity;

        filtroSolido = new ContactFilter2D { useTriggers = false };
        AnimarOlhos(0f);
    }

    // Chamado pelo ConstrutorDeFase: ehLeque = é uma 'k' (três facas em leque).
    public void Montar(bool ehLeque)
    {
        leque = ehLeque;
        if (!leque) return;
        tempoEntreFacas = 2.4f;
        tempoDeAviso = 0.55f;
        alcanceHorizontal = 12f;
        alcanceVertical = 7f;   // do alto ela também te vê
        anguloMaximo = 90f;     // e joga em qualquer ângulo (até reto para baixo)
    }

    void Update()
    {
        float dt = Time.deltaTime; // na pausa é 0: ela e as facas congelam sozinhas
        bool viu = JogadorNoAlcance(out Vector2 jogador);
        if (viu) alvo = jogador;

        switch (estado)
        {
            case Estado.Espreitando:
                recarga = Mathf.Max(0f, recarga - dt);
                if (viu)
                {
                    // te viu! Os olhos abrem e, um tiquinho depois, começa o aviso
                    estado = Estado.Cacando;
                    recarga = Mathf.Max(recarga, tempoDeAviso + demoraParaPerceber);
                }
                break;

            case Estado.Cacando:
                recarga -= dt;
                if (!viu) estado = Estado.Espreitando;
                else if (recarga <= tempoDeAviso) ComecarAviso();
                break;

            case Estado.Avisando:
                // mirando (as facas na mão acompanham o Subaru) até a hora de soltar
                recarga -= dt;
                SegurarFacas();
                if (recarga <= 0f) Arremessar(viu);
                break;
        }

        AnimarOlhos(dt);
        MoverFacas(dt);
    }

    // O Subaru está perto o bastante (e na altura certa) para ela atacar?
    bool JogadorNoAlcance(out Vector2 jogador)
    {
        if (!GerenciadorDoJogo.JogadorVivo(out jogador)) return false;
        Vector2 d = jogador - (Vector2)transform.position;
        return Mathf.Abs(d.x) < alcanceHorizontal && Mathf.Abs(d.y) < alcanceVertical;
    }

    // ------------------------------------------------------------------ o arremesso

    // Olhos vermelhos, sorriso e as facas aparecendo na mão dela, já mirando. É o aviso!
    void ComecarAviso()
    {
        estado = Estado.Avisando;
        int quantas = leque ? Mathf.Max(1, facasNoLeque) : 1;
        for (int i = 0; i < quantas; i++)
            facas.Add(CriarFaca((i - (quantas - 1) / 2f) * aberturaDoLeque));
        SegurarFacas();

        GerenciadorDoJogo.Som("pop", 0.6f); // o "plim" do aviso: ouviu, pula!
        if (leque && !jaRiu)
        {
            jaRiu = true;
            GerenciadorDoJogo.Som("risada", 0.5f); // a primeira vez que você vê as três facas...
        }
    }

    Faca CriarFaca(float desvio)
    {
        SpriteRenderer desenho = DesenharFaca(transform.parent, transform.position, 5, out Light2D luz);
        GameObject objeto = desenho.gameObject;
        var faca = new Faca
        {
            objeto = objeto.transform,
            desenho = desenho,
            desvio = desvio,
            estado = EstadoDaFaca.NaMao,
        };
        // A área que mata gira junto com a faca. Na mão ela ainda não mata (só depois de solta).
        faca.perigo = Perigo.Adicionar(objeto, TamanhoDaAreaQueMata, CentroDaAreaQueMata);
        faca.perigo.ativo = false;
        faca.perigo.conquista = "elsa";
        Perigo.TornarMovel(objeto);
        faca.luz = luz;
        if (faca.luz != null) faca.intensidadeDaLuz = faca.luz.intensity;
        return faca;
    }

    // Só o DESENHO de uma faca, com a luz própria dela (luz = null nas fases claras).
    // Público porque a luta contra a Elsa (ElsaCacadora.cs) usa as mesmas facas: lá quem decide
    // se a faca mata é a própria luta (com as "zonas"), então aqui não vai nenhum Perigo.
    public static SpriteRenderer DesenharFaca(Transform pai, Vector3 lugar, int ordem, out Light2D luz)
    {
        var objeto = ConstrutorDeFase.Visual("FacaDaElsa", pai, lugar, "faca", ordem);
        luz = Luzes.Ponto(objeto.transform, CorDaFaca, 2f, 1f);
        return objeto.GetComponent<SpriteRenderer>();
    }

    // Gira o desenho da faca para a direção. Indo para a esquerda o desenho também é espelhado:
    // assim o fio da lâmina fica sempre para baixo.
    public static void ApontarDesenho(SpriteRenderer desenho, Vector2 direcao)
    {
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        desenho.transform.localRotation = Quaternion.Euler(0f, 0f, angulo);
        desenho.flipY = direcao.x < 0f;
    }

    // Põe as facas da mão no lugar: um pouco à frente dela, apontando para o alvo (cada uma no seu ângulo do leque).
    void SegurarFacas()
    {
        Vector2 tremida = Random.insideUnitCircle * 0.03f; // a mão tremendo de vontade
        foreach (Faca faca in facas)
        {
            if (faca.estado != EstadoDaFaca.NaMao || faca.objeto == null) continue;
            faca.direcao = Mirar(faca.desvio);
            faca.objeto.position = transform.position + (Vector3)(faca.direcao * DistanciaDaMao + tremida);
            Apontar(faca);
        }
    }

    // Direção da faca: para o alvo, com no máximo "anguloMaximo" para cima ou para baixo,
    // mais o desvio dela no leque (positivo = mais para cima, dos dois lados).
    Vector2 Mirar(float desvio)
    {
        Vector2 d = alvo - (Vector2)transform.position;
        float lado = d.x >= 0f ? 1f : -1f;
        float elevacao = Mathf.Atan2(d.y, Mathf.Abs(d.x)) * Mathf.Rad2Deg; // 0 = reto para o lado, 90 = para cima
        elevacao = Mathf.Clamp(elevacao, -anguloMaximo, anguloMaximo) + desvio;
        float radianos = elevacao * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radianos) * lado, Mathf.Sin(radianos));
    }

    // Gira o desenho (e a área que mata, que está no mesmo objeto) para a direção da faca.
    static void Apontar(Faca faca) => ApontarDesenho(faca.desenho, faca.direcao);

    // Solta as facas da mão. Agora elas matam!
    void Arremessar(bool aindaVendo)
    {
        foreach (Faca faca in facas)
        {
            if (faca.estado != EstadoDaFaca.NaMao || faca.objeto == null) continue;
            faca.estado = EstadoDaFaca.Voando;
            faca.perigo.ativo = true;
        }
        GerenciadorDoJogo.Som("serra", 0.45f); // "ziiing"
        estado = aindaVendo ? Estado.Cacando : Estado.Espreitando;
        recarga = tempoEntreFacas;
    }

    // ------------------------------------------------------------------ as facas no ar

    void MoverFacas(float dt)
    {
        for (int i = facas.Count - 1; i >= 0; i--)
        {
            Faca faca = facas[i];
            if (faca.objeto == null)
            {
                facas.RemoveAt(i);
                continue;
            }

            if (faca.estado == EstadoDaFaca.Voando)
            {
                // anda em passinhos e para no primeiro que encostar em algo sólido
                float falta = velocidadeDaFaca * dt;
                while (falta > 0f && faca.estado == EstadoDaFaca.Voando)
                {
                    float passo = Mathf.Min(falta, PassoMaximo);
                    falta -= passo;
                    faca.objeto.position += (Vector3)(faca.direcao * passo);
                    faca.percorrido += passo;
                    if (PontaEncostouEmAlgo(faca)) Cravar(faca);
                }
                DeixarRastro(faca, dt);
                if (faca.estado == EstadoDaFaca.Voando && faca.percorrido > distanciaMaxima)
                {
                    Destroy(faca.objeto.gameObject);
                    facas.RemoveAt(i);
                }
            }
            else if (faca.estado == EstadoDaFaca.Cravada)
            {
                // cravada: treme um pouquinho e vai apagando até sumir
                faca.timer -= dt;
                float resta = Mathf.Clamp01(faca.timer / tempoCravada);
                faca.desenho.color = new Color(1f, 1f, 1f, resta);
                if (faca.luz != null) faca.luz.intensity = faca.intensidadeDaLuz * resta;
                if (faca.timer <= 0f)
                {
                    Destroy(faca.objeto.gameObject);
                    facas.RemoveAt(i);
                }
            }
        }
    }

    // A ponta da faca está dentro de algo sólido? (o Subaru e os bichos não contam: a faca atravessa os bichos)
    bool PontaEncostouEmAlgo(Faca faca)
    {
        Vector2 ponta = (Vector2)faca.objeto.position + faca.direcao * PontaDaFaca;
        int quantidade = Physics2D.OverlapCircle(ponta, 0.08f, filtroSolido, encostados);
        for (int i = 0; i < quantidade; i++)
        {
            Collider2D outro = encostados[i];
            if (outro.GetComponent<Jogador>() == null && !Inimigo.EhBicho(outro)) return true;
        }
        return false;
    }

    // TUNK! A faca crava, solta faíscas e para de matar.
    void Cravar(Faca faca)
    {
        faca.estado = EstadoDaFaca.Cravada;
        faca.timer = tempoCravada;
        faca.perigo.ativo = false;

        Vector3 ponta = faca.objeto.position + (Vector3)(faca.direcao * PontaDaFaca);
        for (int i = 0; i < 6; i++)
        {
            // as faíscas voltam para trás, abrindo em leque
            Vector2 volta = -faca.direcao * Random.Range(1.5f, 4f) + Random.insideUnitCircle * 2.5f;
            Particula.Criar(transform.parent, ponta, volta, Random.Range(0.15f, 0.3f), Random.Range(0.35f, 0.6f), CorDaFaisca);
        }

        if (GerenciadorDoJogo.JogadorVivo(out Vector2 jogador))
        {
            float distancia = Vector2.Distance(jogador, ponta);
            if (distancia < 12f) GerenciadorDoJogo.Som("bloco", 0.5f);
            if (distancia < 4f) CameraSeguir.Tremer(0.05f, 0.08f);
        }
    }

    // Um rastrinho rosado atrás da faca (no escuro, ajuda a ver de onde ela veio e para onde vai).
    // Cada faca tem o seu timer: no leque, as três deixam rastro.
    void DeixarRastro(Faca faca, float dt)
    {
        faca.timerRastro -= dt;
        if (faca.timerRastro > 0f) return;
        faca.timerRastro = 0.03f;
        Particula.Criar(transform.parent, faca.objeto.position, Vector2.zero, 0.18f, 0.55f, CorDoRastro);
    }

    // ------------------------------------------------------------------ olhos e silhueta

    // Espreitando, os olhos abrem e fecham de vez em quando ("brilham e somem").
    // Caçando, ficam abertos. Avisando, ficam vermelhos, brilham forte e ela sorri.
    void AnimarOlhos(float dt)
    {
        bool avisando = estado == Estado.Avisando;
        float alvoDaAbertura = 1f;
        if (estado == Estado.Espreitando)
        {
            // pisca num ritmo diferente para cada Elsa (pela posição): abertos 0,9 s a cada 3,2 s
            float ciclo = Mathf.Repeat(Time.time + transform.position.x * 0.73f, 3.2f);
            alvoDaAbertura = ciclo < 0.9f ? 1f : 0f;
        }
        abertura = dt <= 0f ? alvoDaAbertura : Mathf.MoveTowards(abertura, alvoDaAbertura, 5f * dt);

        desenhoDosOlhos.sprite = avisando ? olhosBravos : olhosNormais;
        desenhoDosOlhos.color = new Color(1f, 1f, 1f, abertura);
        desenhoDoSorriso.enabled = avisando;
        desenhoDaSilhueta.color = new Color(1f, 1f, 1f, 0.35f + 0.65f * abertura); // some no escuro quando fecha os olhos
        // respirando (a silhueta sobe e desce meio pixel)
        silhueta.position = transform.position + Vector3.up * (AlturaDaSilhueta + Mathf.Sin(Time.time * 2f) * 0.03f);

        if (luzDosOlhos == null) return;
        if (avisando)
        {
            // pisca forte e vermelho
            luzDosOlhos.color = CorDoAviso;
            luzDosOlhos.pointLightOuterRadius = 2.6f;
            luzDosOlhos.intensity = intensidadeDosOlhos * (1.3f + 0.4f * Mathf.Sin(Time.time * 40f));
        }
        else
        {
            luzDosOlhos.color = CorDosOlhos;
            luzDosOlhos.pointLightOuterRadius = 1.2f;
            luzDosOlhos.intensity = intensidadeDosOlhos * 0.55f * abertura;
        }
    }

    // ------------------------------------------------------------------ desenhos

    // Registra os desenhos na FabricaDeSprites (funcionam com Pegar("faca") em qualquer lugar,
    // inclusive no mapa do mundo, ao lado do ponto desta fase).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("faca", () => FabricaDeSprites.DeArte(ArteFaca, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("elsa_sombra", () => FabricaDeSprites.DeArte(ArteElsa, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("olhos_elsa", () => FabricaDeSprites.DeArte(ArteOlhos, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("olhos_elsa_bravos", () => FabricaDeSprites.DeArte(ArteOlhosBravos, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("sorriso_elsa", () => FabricaDeSprites.DeArte(ArteSorriso, FabricaDeSprites.Centro));
    }

    // A kukri, apontando para a direita: cabo de madeira com a ponta de metal, a guarda,
    // e a lâmina que "dobra" para baixo e engorda perto da ponta (com o entalhe perto do cabo).
    static readonly string[] ArteFaca =
    {
        ".......k.kkkkk......",
        ".kkkkkkMkwwwwwkk....",
        "ksnnnnnSwssssswsk...",
        "kSbbbbbSssSssssssk..",
        "kSdddddSSSkSMMssssk.",
        ".kkkkkkMkk.kSSMMMMsk",
        ".......k....kkSSSSSk",
        "..............kkkkk.",
    };

    // A Elsa nas sombras: cabelo preto comprido caindo dos lados, capa preta com a beirada roxa,
    // e a parte de baixo se desfazendo no escuro. O rosto fica na sombra (os olhos são outro desenho, por cima).
    static readonly string[] ArteElsa =
    {
        ".....kkkkkk.....",
        "...kkxxxxxxkk...",
        "..kxxhxxxxxxxk..",
        ".kxxhxxxxhxxxxk.",
        ".kxxxxkxxxkxxxk.",
        ".kxxkffxxxffkxk.",
        "kxxkffffffffkxxk",
        "kxxkffffffffkxxk",
        "kxhkfffffffffxhk",
        "kxhxkffffffkxxhk",
        "kxhxxkkffkkxxhxk",
        "kvhvkxxkkxxkvhvk",
        "kvhvvxvxxvxvvhvk",
        "kxhxxxxvvxxxxhxk",
        "kxhxxxxvvxxxxkxk",
        ".xhxxxxvvxxxxkx.",
        ".x.h.x.vx.x.x.x.",
        "..x.x.x.x.x.x...",
        ".x...x...x...x..",
        "....x.......x...",
    };

    // Os olhos de sempre: lilases, meio fechados, com o brilho branco no canto.
    static readonly string[] ArteOlhos =
    {
        ".kk....kk.",
        "kwPV..VPwk",
        ".kVk..kVk.",
    };

    // Os olhos do aviso: arregalados, vermelhos e com as sobrancelhas de "vou te pegar".
    static readonly string[] ArteOlhosBravos =
    {
        "k........k",
        ".kkk..kkk.",
        "kwpr..rpwk",
        "kprk..kprk",
        ".kk....kk.",
    };

    // O sorriso (só aparece no aviso).
    static readonly string[] ArteSorriso =
    {
        "k....k",
        ".kwwk.",
    };
}
