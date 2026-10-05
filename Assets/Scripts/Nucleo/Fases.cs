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
//
//  As placas recebem os textos na ordem da esquerda para a direita.
//
//  MUDANÇAS: a fase pode MUDAR depois que você morre (estilo Level Devil).
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
                "Bem-vindo ao CILADA!\nA/D ou SETAS para andar, ESPAÇO para pular.",
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
                "",
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
                "              ##  FF",
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
                "",
                "  P i  $$ m$            h                                i       G",
                "###############   #############CCC############   #####################",
                "###############   #############CCC############   #####################",
            },
            mudancas = new[]
            {
                new[] { "10,12,$", "8,12,m", "24,12,.", "21,12,h", "31,13,#", "32,13,#", "33,13,#", "31,14,#", "32,14,#", "33,14,#", "36,13,C", "37,13,C", "38,13,C", "36,14,C", "37,14,C", "38,14,C", "40,8,.", "42,8,v", "52,9,K", "53,9,?", "60,12,B", "63,12,Z" },
                new[] { "24,12,.", "27,12,h", "36,13,C", "37,13,C", "36,14,C", "37,14,C", "42,8,v", "11,12,m", "10,12,$" },
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
                "",
                "  P i   X       e             X  e   E       i  X           X      G",
                "#####################   #################CCC########    ################",
                "#####################   #################CCC########    ################",
            },
            mudancas = new[]
            {
                new[] { "12,12,X", "16,12,E", "37,12,e" },
                new[] { "30,12,.", "28,12,X", "33,12,E", "26,9,K" },
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
                "",
                "                                                 $",
                "                                                BBB",
                "",
                "",
                "  P i                 i B  W                $m$               >        G",
                "########### MM     ################   ######################################",
                "###########        ################   ######################################",
            },
            mudancas = new[]
            {
                new[] { "27,12,G", "71,12,W" },
                new[] { "31,12,h", "45,12,$", "46,12,m" },
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
                "                                                             i R    h     *",
                "  P i $m$ X       X         e           <                 ####################",
                "#############   ############### MM     ########CC#############################",
                "#############   ###############        ########CC#############################",
            },
            mudancas = new[]
            {
                new[] { "10,12,.", "7,12,$", "8,12,m" },
                new[] { "68,11,.", "66,11,h", "28,12,E", "50,9,K", "51,9,?" },
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
    };
}
