// =====================================================================
//  AS FASES DO JOGO
// =====================================================================
//  Cada fase é desenhada com texto! Cada caractere é um bloco de 1x1.
//  A linha de cima do texto é o topo da tela. Todas as fases têm 15 linhas.
//
//  LEGENDA
//   #  chão                       B  tijolo
//   P  início do jogador          G  bandeira (fim da fase)
//   ?  bloco de moeda             K  bloco "surpresa" (solta um inimigo!)
//   $  moeda                      E  inimigo
//   S  mola                       i  placa (texto vem da lista "placas")
//   ^  espinho                    h  espinho escondido (sai do chão)
//   v  espinho no teto que cai    T  bloco esmagador (despenca em você)
//   C  chão que despenca          F  chão falso (você atravessa)
//   I  bloco invisível (aparece quando você bate a cabeça)
//   <  serra que vem por trás     >  serra que vem pela frente
//   R  bandeira fujona            *  para onde a bandeira fujona foge
//   Z  bandeira falsa (mata!)     o  nuvem assassina
//   m  moeda assassina (igual à moeda normal)
//   e  inimigo disfarçado (igual ao normal, mas tem espinhos: NÃO pise!)
//   M  bloco que foge quando você pula perto dele
//   W  bandeira que te manda de volta pro começo
//   X  linha invisível que inverte os controles (cruzou de novo, desinverte)
//   U  cano (2 de largura, desce até o chão): de tempos em tempos sai uma MÃO dele. Encostou, morreu
//   r  coelho (Grande Coelho): pula atrás de você e se MULTIPLICA. Pise em cima para derrotar
//   w  Baleia Branca: quando você passa daqui, ela surge do fundo e atravessa a tela (desvie!).
//      Coloque no chão da arena, na linha em que o jogador anda
//   L  Emilia (esperando no fim da última fase)
//
//  As placas (quem fala é o Puck) recebem os textos na ordem da esquerda para a direita.
//
//  MUDANÇAS (opcional, nenhuma fase usa agora): a fase pode MUDAR depois que você morre (estilo Level Devil).
//  Cada item de "mudancas" é uma versão da fase usada nas tentativas seguintes,
//  escrita como uma lista de trocas "coluna,linha,caractere" ('.' = vazio).
//  Coluna e linha começam em 0 (linha 0 = topo). Na 1ª tentativa vale o mapa
//  original; na 2ª a 1ª mudança; na 3ª a 2ª; e assim por diante, em ciclo.
// =====================================================================

public class Fase
{
    public string nome;
    public string[] mapa;
    public string[] placas;
    public string[][] mudancas;
}

public static class Fases
{
    public static readonly Fase[] Todas =
    {
        new Fase
        {
            nome = "Bem-vindo :)",
            placas = new[]
            {
                "Bem-vindo ao Re:CILADA!\nA/D ou SETAS para andar, ESPAÇO para pular.",
                "Dica: pule BEM alto aqui!",
                "A bandeira tá logo ali. Pode ir tranquilo!",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "                                        BBBBBBB",
                "                                           v",
                "        ?B?    II                   ?K",
                "                            $$",
                "                                        U",
                "  P i       i       $$h          E                      i   R               *",
                "###############  ##########CCCC################   ###############   ##############",
                "###############  ##########CCCC################   ###############   ##############",
            },
        },

        new Fase
        {
            nome = "Confia em mim",
            placas = new[]
            {
                "Essa fase é tranquila. Juro.",
                "Chão 100% seguro à frente.",
                "Última bandeira. Agora é sério.",
            },
            mapa = new[]
            {
                "",
                "",
                "                v",
                "",
                "",
                "                           BBBBBBBBB",
                "                             T   T",
                "                  ######",
                "                  ######",
                "          o       ######",
                "                  ######",
                "         B        ######                   $$",
                "  P i    B      S ######            i        i  E B Z   G",
                "######################################FFF#################",
                "######################################FFF#################",
            },
        },

        new Fase
        {
            nome = "Corre!",
            placas = new[]
            {
                "Não olhe para trás.",
                "Cuidado com o que vem pela frente!",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "                                                                    BBBBBBBB           *",
                "                                                                      v  v          BBBBBB",
                "",
                "                ??",
                "                     $$",
                "                                                  i         >                 R   S",
                " <P i                     E   ###############################CCCC###########################",
                "############  ######   ######################################CCCC###########################",
                "############  ######   ######################################CCCC###########################",
            },
        },

        new Fase
        {
            nome = "O Final (sem pegadinhas)",
            placas = new[]
            {
                "Última fase. Sem pegadinhas.\nPalavra de escoteiro.",
                "Pula com força! (ou não)",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "                              BBBBB",
                "                                T",
                "",
                "",
                "                    ##    ?K?                            IIII",
                "                                              o",
                "              ##  FF                U",
                "  P i h                        $$$                E   i           G",
                "###########            ##################CCC#############    #########",
                "###########            ##################CCC#############    #########",
            },
        },

        new Fase
        {
            nome = "Eu mudo de ideia",
            placas = new[]
            {
                "Pegue as moedas! Todas!",
                "Decorou a fase? Que pena.",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "                                      BBBBBB",
                "                                        v",
                "                                                    ?K",
                "",
                "                                                  U",
                "  P i  $$ m$            h             rr                 i       G",
                "###############   #############CCC############   #####################",
                "###############   #############CCC############   #####################",
            },
        },

        new Fase
        {
            nome = "Cadê a direita?",
            placas = new[]
            {
                "Tudo normal por aqui. Pode andar.",
                "Esquerda é direita. Ou era?",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "                          ?",
                "",
                "                                                                U",
                "  P i   X       e             X  e   E       i  X           X      G",
                "#####################   #################CCC########    ################",
                "#####################   #################CCC########    ################",
            },
        },

        new Fase
        {
            nome = "Foge, bloco!",
            placas = new[]
            {
                "Esses blocos são meio tímidos...",
                "Chegada logo ali!",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "                                                                    BBBBB",
                "                                                 $                    T",
                "                                                BBB",
                "",
                "                                                       U",
                "  P i                 i B  W                $m$               >        G",
                "########### MM     ################   ######################################",
                "###########        ################   ######################################",
            },
        },

        new Fase
        {
            nome = "O Verdadeiro Final (juro)",
            placas = new[]
            {
                "Agora sim, a última. Confia.",
                "A bandeira é logo ali. Corre!",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "                     BBBBB",
                "                       T",
                "",
                "",
                "                                                  ?K",
                "",
                "                      U                                U    i R    h              w                             *   L",
                "  P i $m$ X       X         e           <                 #############################################################",
                "#############   ############### MM     ########CC######################################################################",
                "#############   ###############        ########CC######################################################################",
            },
        },
    };

    // Frases que aparecem quando você morre (escolhidas aleatoriamente).
    public static readonly string[] MensagensDeMorte =
    {
        "KKKKKKKKKKK",
        "Achou que ia ser fácil?",
        "Isso foi de propósito.",
        "Confia no processo.",
        "O chão é uma mentira.",
        "Tenta de novo, campeão!",
        "Ninguém viu isso. Só eu.",
        "Eu avisei... ou não.",
        "Quase! (mentira)",
        "Skill issue.",
        "Nem tudo é o que parece.",
        "Já pensou em desistir?",
        "Essa doeu até em mim.",
        "Clássico.",
        "Você caiu numa CILADA!",
        "O jogo tá rindo de você.",
        "Mais uma pra conta!",
        "Respira e tenta de novo.",
        // --- Re:Zero ---
        "Recomeçando do zero...",
        "A Bruxa mandou um abraço.",
        "Só você lembra dessa morte.",
        "Emilia-tan ficaria decepcionada.",
        "O Puck viu tudo.",
        "Foi o Mabeast. Ou o chão. Ou os dois.",
        "O Subaru já passou por coisa pior.",
    };
}
