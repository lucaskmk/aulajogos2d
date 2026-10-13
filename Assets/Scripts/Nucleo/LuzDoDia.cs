using UnityEngine;

// A "hora do dia" e o céu, divididos entre os efeitos (raios de sol, cores da tela, sombras de nuvem).
//
//  - Entardecer: nas fases de DIA, o começo da fase é de manhã e o fim é um entardecer dourado.
//    Quem manda é a posição do Subaru na fase (0 = manhã, 1 = fim de tarde). É bem suave: só a parte
//    final da fase fica realmente alaranjada. No painel F10 dá para forçar uma hora (AjustesVisuais.horaManual).
//  - Cobertura: quanto o sol está tapado por uma nuvem agora (0 = céu limpo, 1 = nuvem cheia).
//    Quem calcula são as SombrasDeNuvens; os raios de sol (FeixesDeLuz) somem quando ela sobe.
public static class LuzDoDia
{
    public static float Entardecer { get; private set; }
    public static float Cobertura { get; set; }

    static int faseAnterior = int.MinValue;

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar()
    {
        Entardecer = 0f;
        Cobertura = 0f;
        faseAnterior = int.MinValue;
    }

    // Chamado uma vez por quadro (pelos EfeitosDeTela).
    public static void Atualizar(GerenciadorDoJogo jogo, Clima clima)
    {
        bool dia = jogo.Mapa == null && clima == Clima.Dia;
        float alvo = 0f;
        if (dia && AjustesVisuais.horaManual >= 0f) alvo = Mathf.Clamp01(AjustesVisuais.horaManual);
        else if (dia && AjustesVisuais.CorPorHoraLigada)
        {
            Jogador jogador = GerenciadorDoJogo.JogadorAtual;
            int largura = Paralaxe.LarguraDaFase;
            if (jogador != null && largura > 0)
            {
                // o primeiro quinto da fase é manhã; depois a tarde vai chegando devagar
                float caminho = Mathf.InverseLerp(largura * 0.2f, largura, jogador.transform.position.x);
                alvo = Mathf.SmoothStep(0f, 1f, caminho);
            }
        }

        // Trocou de fase (ou foi para o mapa)? Pula direto para a hora certa: a tela está coberta pela transição.
        int id = jogo.Mapa != null ? -1 : jogo.FaseAtual;
        if (id != faseAnterior)
        {
            faseAnterior = id;
            Entardecer = alvo;
        }
        else Entardecer = Mathf.MoveTowards(Entardecer, alvo, 0.5f * Time.unscaledDeltaTime);
    }
}
