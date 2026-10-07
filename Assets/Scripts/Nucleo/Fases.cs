using UnityEngine;

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
//   X  linha invisível que inverte os controles depois de uma contagem curta (1,2 s) (cruzou de novo, desinverte).
//      Só UMA fase usa isso, e só num trecho (é difícil demais para usar sempre)
//   U  cano (2 de largura, desce até o chão): de tempos em tempos sai uma MÃO dele. Encostou, morreu
//   r  coelho (Grande Coelho): pula atrás de você e se MULTIPLICA. Pise em cima para derrotar
//   w  Baleia Branca (CHEFE): começo da arena de 26 blocos, na linha em que o jogador anda.
//      Entrou, a arena fecha e a luta começa (3 de vida: pule na cabeça dela quando ela cair)
//   L  Emilia (esperando no fim da última fase)
//   0-9  porta da Beatrice (2 de altura), com o número em cima: aperte S/seta para baixo na frente dela.
//      Para onde cada porta leva fica na lista "portas" da fase, em pares de ida e volta: "1-4" = a 1 leva
//      para a 4 e a 4 volta para a 1. É SEMPRE igual (dá para decorar o caminho)
//   Y  Beatrice (fala como as placas do Puck; os textos entram na mesma lista "placas")
//   s  ponto de save: encostou, você renasce ali. Mas às vezes ele "muda de lugar"... :)
//   j  plataforma mágica: vai para a direita e volta (até 6 blocos ou até encostar em algo) e leva você junto.
//      Blocos 'j' lado a lado na mesma linha = uma plataforma só. Todas começam juntas, indo para a direita
//   J  IGUAL à 'j'... mas 0,5 s depois que você sobe, o cristal fica VERMELHO, ela treme e DESPENCA
//      (reaparece no lugar 1,5 s depois, no mesmo ritmo das outras)
//   D  IGUAL à 'j'... mas o cristal fica AMARELO, ela treme e DISPARA até bater numa parede, freia de
//      repente e te joga para a frente. Quem pula quando ela treme cai no abismo
//   Q  mangual da Rem: eixo de ferro (sólido, dá para subir) com uma bola de espinhos girando na corrente
//      (raio 2,75). Só a BOLA mata. A COLUNA do 'Q' escolhe onde a bola começa e o sentido do giro
//      (coluna % 8, tabela Jeitos em MangualDaRem.cs). Com chão em cima ele fica ENTERRADO (parece chão)
//   q  IGUAL ao 'Q'... mas quando você chega perto a corrente range, a Rem fica brava e a bola volta
//      girando para o OUTRO lado, 2x mais rápido
//   y  vento da Ram: células 'y' encostadas = UMA zona (o retângulo em volta delas; pode ter moedas e blocos dentro).
//      Empurra para a ESQUERDA em rajadas, todas juntas: calmo 1 s, aviso 0,5 s (folhas e riscos começam a passar), rajada 2 s.
//      Segure para a frente, ou se esconda até 2 blocos à esquerda de um bloco sólido (o vento não atravessa paredes)
//   n  redemoinho: zona igual à 'y' que levanta o Subaru até o topo dela (ele fica boiando lá). Se a coluna da ESQUERDA
//      da zona for ÍMPAR, ele é TRAIÇOEIRO: quando você chega perto (4 blocos), engasga (fica cinza) e desliga por 1,5 s
//   a  bloco do ritmo AZUL (Rem): sólido e apagado se revezam com o 'b' a cada 1,4 s (piscam e fazem tic-tic-TAC antes)
//   b  bloco do ritmo ROSA (Ram): aceso quando o 'a' está apagado, e vice-versa. Blocos iguais lado a lado = um só
//   A  IGUAL ao 'a' (pisca e acende junto)... mas nunca fica sólido de verdade
//   t  esmagador do ritmo: despenca toda vez que os 'a' acendem (treme e fica de olho vermelho antes). Rosa aceso = pode passar
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
    public string[] portas; // pares de portas ligadas, ex.: "1-4" (só na Biblioteca Proibida)
    public bool noite;      // fase de noite: céu escuro com lua e estrelas, luzinhas acesas, vaga-lumes
    public bool escura;     // fase no ESCURO: quase não se vê nada, só em volta do Subaru e das luzinhas
    public bool petalas;    // pétalas caindo (no lugar do pólen)
    public Color32? corDoFundo; // cor do céu da fase (tons pastel, como os ímãs de Re:Zero)
}

public static class Fases
{
    public static readonly Fase[] Todas =
    {
        new Fase
        {
            nome = "Bem-vindo :)",
            corDoFundo = new Color32(150, 215, 235, 255),
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
                "                                           v          ?",
                "        ?B?    II                   ?K",
                "               $$           $$                                        $$",
                "                                        U            BBB",
                "  P i       i       $$h          E                      i   R            E  *",
                "###############  ##########CCCC################   ###############   ##############",
                "###############  ##########CCCC################   ###############   ##############",
            },
        },

        new Fase
        {
            nome = "Confia em mim",
            corDoFundo = new Color32(245, 190, 120, 255),
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
                "          o       ######   ?",
                "                  ######",
                "         B        ######     $$$           $$",
                "  P i    B      S ######         E  i        i  E B Z   G",
                "######################################FFF#################",
                "######################################FFF#################",
            },
        },

        new Fase
        {
            nome = "Corre!",
            corDoFundo = new Color32(130, 210, 200, 255),
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
                "                ??                    $$$",
                "                     $$               BBB                            $$$",
                "                                             s    i         >                 R   S",
                " <P i                     E   ###############################CCCC###########################",
                "############  ######   ######################################CCCC###########################",
                "############  ######   ######################################CCCC###########################",
            },
        },

        new Fase
        {
            nome = "O Final (sem pegadinhas)",
            corDoFundo = new Color32(240, 150, 205, 255),
            petalas = true,
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
                "              ##  FF                U        $$",
                "  P i h                        $$$     r          E   i           G",
                "###########            ##################CCC#############    #########",
                "###########            ##################CCC#############    #########",
            },
        },

        new Fase
        {
            nome = "Eu mudo de ideia",
            corDoFundo = new Color32(245, 222, 110, 255),
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
                "                                                           $$",
                "                                                  U",
                "  P i  $$ m$            h             rr                 i    E  G",
                "###############   #############CCC############   #####################",
                "###############   #############CCC############   #####################",
            },
        },

        new Fase
        {
            nome = "Quem é Rem?",
            corDoFundo = new Color32(165, 185, 240, 255),
            placas = new[]
            {
                "Mansão Roswaal! A empregada Rem limpa tudo.\nInclusive visitas. Com um MANGUAL.",
                "Só a BOLA mata. A corrente é enfeite.\nDica: em cima do eixo ela não te alcança.",
                "Se ouvir a corrente RANGER... corre.\nNão me pergunte por quê. :)",
                "Save! Agora é pular de eixo em eixo.\nO chão do outro lado? Confia.",
                "A bandeira! Acabou!\n...Você ainda confia em bandeira?",
                "O corredor da faxina.\nEla varre o chão. Com você junto.",
                "Daqui pra frente é só chão.\nChão normal, de terra. Juro pelo Puck.",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "                                                                                                  BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
                "                                                                                                  BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
                "                                                                    m                    $        BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
                "      ?B?                    Q      Q          q                $       $                         BBBBQBBBBBQBBBBBQBBBBBQBBBBBQBBBBBQBBB    ? K",
                "            $$   $                                                                                 v",
                "                                                                  Q       Q                                          $                             $",
                "  P i      i     Q     E        $$       i    $$$   $ s i     Q       q             i  B Z   s i         $     $     h     $     $        i                G",
                "###########################################################                   CC#################################################################################",
                "###########################################################                   CC###################################################################Q#######q#####",
            },
        },

        new Fase
        {
            nome = "Cadê a direita?",
            corDoFundo = new Color32(195, 165, 230, 255),
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
                "                    ?     ?",
                "                                                                            $$",
                "          $$$",
                "  P i           e                e   E       is X                   ^               X   G",
                "#####################   #################CCC##################   ######   ##################",
                "#####################   #################CCC##################   ######   ##################",
            },
        },

        new Fase
        {
            nome = "Foge, bloco!",
            corDoFundo = new Color32(245, 185, 195, 255),
            petalas = true,
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
                "                              $$$                      U",
                "  P i                 i B  W             E  $m$           rr  >        G",
                "########### MM     ################   ######################################",
                "###########        ################   ######################################",
            },
        },

        new Fase
        {
            nome = "Ponte para a Capital",
            corDoFundo = new Color32(120, 110, 185, 255),
            noite = true,
            placas = new[]
            {
                "A ponte para a capital caiu...\nMas o Roswaal mandou plataformas mágicas! Sobe que ela te leva.",
                "Essa aqui é IGUALZINHA à outra.\nPode subir sem medo!",
                "Relaxa e curte o passeio.\nNenhuma serra à vista!",
                "EXPRESSA PARA A CAPITAL!\nSe ela tremer, NÃO PULE. (dessa vez é sério)",
                "Chegou! Viu só? Era só confiar no Puck. :)",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "                                                              v",
                "                                            BBBBB",
                "                           ?                  T            o",
                "      ?K?                                                    $ $ $     ?K                  ?                  $$        ?",
                "                                    $ $  $$               jj                                               JJ",
                "          $$                $$     jj                                        $$                          $m$           $                         $ $ $",
                "  P i            $$$    i         $               s  jjj                   i  h    $$$         >B s  jjj                    i      $ $ $ $            i   G",
                "###############jj      #########JJ      #############               #############jj      ############                #########DDD             ##############",
                "###############        #########        #############               #############        ############                #########                ##############",
            },
        },

        new Fase
        {
            nome = "Voa, Barusu!",
            corDoFundo = new Color32(245, 195, 215, 255),
            placas = new[]
            {
                "A Ram tá de mau humor hoje.\nQuando as FOLHAS vierem, segure pra frente ou se esconda atrás de um bloco!",
                "Redemoinho da Ram! Entra que ele te leva lá pra cima.\nDepois é só segurar pra direita.",
                "Outro redemoinho, igualzinho ao primeiro!\nPode ir direto, sem medo. :)",
                "Dica de graça: espere o redemoinho te levantar antes de sair.\n(essa é de verdade. A outra... nem tanto)",
                "Pronto! A Ram cansou e foi tirar um cochilo.\nAcabou o vento. A bandeira é logo ali!",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "                                                                                                 nnnnn$$$$nnnnn           yyyy                   yyyyyyyyyyyyyyyyyyyy",
                "                                                                          yyyyyyyyyyyyyyyyyyyy   nnnnnnnnnnnnnn         $nyyyynnn  n$n           yyyyyyyyy$$$yyyyyyyy",
                "            $$                                            nnnn$$$$nn      yyyyyyyyyyyyyyyyyyyy   nnnnnnnnnnnnnn         nnyyyynnn  nnn           yyyyyyyyyyyyyyy?yyyy",
                "                                                          nnnnnnnnnn      yyyyyyy$yyy$m$yyyyyy   nnnnnnnnnnnnnn         nnyy$ynnn  nnn           yyyyyyyyyyyyyyyyyyyy",
                "                                                          nnnnnnnnnn      yyyyyyyyyyyyyyyyyyyy   nnnnnnnnnnnnnn         nnyyyynnn  nnn           yyyyyyyyyyyyyyyyyyyy",
                "              yyyyyyyyyyyyyyyyyy yyyyyyyyyyyyyyyyyyyyyyy  nnnnnnnnnn   s  yyyByyyyyyyyyyEyByyyi  nnnnnnnnnnnnnn   s i   nnyyyynnn  nnn      i    yyyyBByyZyyyyByyyyBy  G",
                "        I ?K  yyyyyyyyy?yyyyyyyy yyyyyyyy?yyyy$yyyyyy?yy  nnnnn$$nnn ###########   ##############nnnnnnnnnnnnnn ####### nnyyyynnn  nnn  ######CC#####################",
                "       $$     yyy$$yyyyy$$yyyyyy yyyyyyyyyyyyyyyyyyyyyyy  nnnnnnnnnn ###########   ##############nnnnnnnnnnnnnn ####### nnyyyynnn  nnn  ######  #####################",
                "              yyyyyyyyyyyyyyyyyy yyyyyyyyyyyyyyyyyyyyyyy  nnnnnnnnnn ###########   ##############nnnnnnnnnnnnnn ####### nnyyyynnn  nnn  ######  #####################",
                "  P i       S yyyyyyByyyyyyByyyy yy^^yyEyyByyyyyyyyhyyyy innnnnnnnnn ###########   ##############nnnnnnnnnnnnnn ####### nnyyyynnn  nnn  ######  #####################",
                "#############################################   #############nnnnnnn ###########   ##############nnnnnnnnnnnnnn ####### nnyyyynnn  nnn  ######  #####################",
                "#############################################   #############nnnnnnn ###########   ##############nnnnnnnnnnnnnn ####### nnyyyynnn  nnn  ######  #####################",
            },
        },

        new Fase
        {
            nome = "Biblioteca Proibida",
            corDoFundo = new Color32(185, 150, 120, 255),
            placas = new[]
            {
                "Cada porta tem um número e SEMPRE leva para o mesmo lugar.\nDecore o caminho! (S para entrar)",
                "Beco sem saída! Mas tem Mabeast.\nA porta 9 sempre volta para a 2.",
                "Achou a porta certa, de fato.\nNão que eu estivesse esperando, kashira.",
            },
            // A sai pela 1 ou 3 (sala das armadilhas), a 6 vai para a sala das moedas, a 8 vai para a Beatrice.
            // A 2 é o beco sem saída com os Mabeasts.
            portas = new[] { "1-4", "3-5", "2-9", "6-7", "8-0" },
            mapa = new[]
            {
                "#############################################################",
                "#############################################################",
                "#           #         v #           #           #           #",
                "#           #           #           #           #           #",
                "#           #           #           #           #           #",
                "#           #           #           #           #           #",
                "#           #           #           #           #           #",
                "#           #           #           #           #  BBBBBB   #",
                "#  BBBBBB   #           #           #           #           #",
                "#           #           #  BBBBBB   #           #           #",
                "#           #           #           #      $    #           #",
                "#           #           #   $$$m    #           #           #",
                "# Pi 1  2 3 # 4 h 5 h 6 # 7   E   8 # 9 i e  E  # 0   Y  G  #",
                "#############################################################",
                "#############################################################",
            },
        },

        new Fase
        {
            nome = "Dança das Gêmeas",
            noite = true,
            corDoFundo = new Color32(150, 120, 200, 255),
            placas = new[]
            {
                "Blocos da Rem (azul) e da Ram (rosa) se revezam.\nTic, tic, TAC: trocou! Pise no que tá aceso.",
                "Pule no TIC, caia no TAC.\nAs gêmeas não esperam ninguém.",
                "Esses aí caem quando o AZUL acende.\nRosa aceso = caminho livre. (dessa vez é sério)",
                "Agora é só ritmo puro, sem pegadinha.\nPalavra de espírito! :3",
                "Ufa, chegou! A bandeira tá logo ali.\nPode ir andando, sem pressa. :)",
            },
            mapa = new[]
            {
                "",
                "",
                "                                                      ?",
                "",
                "                                                $",
                "                                                 bb ####",
                "        $                              v     $      ####                                                                 $$",
                "                                              aa    ####                                                             $       m",
                "                                                    ####     BBBBBBBBBBBBBBBBBBBBBBBBBB                      $                   $",
                "       ?B?                ?                bb       ####      tt      tt      ttt                   ?K?                aa                       $$",
                "                    $m$         $$                  ####                                      $                aa              bb",
                "                                        aa          ####                                                           bb      bb",
                "  P i             h      i                ^^^^^^^^^^#### s i           $       $        i            s     bb                      aaa  i   b Z       G",
                "############aaaa####bbbb#####aaa  bbb############################bbb####bbbb#######aaa#####aaaAaaa########                             ########aaaa#######",
                "############^^^^####^^^^#####^^^^^^^^############################^^^####^^^^#######^^^#####^^^^^^^########^^^^^^^^^^^^^^^^^^^^^^^^^^^^^########^^^^#######",
            },
        },

        new Fase
        {
            nome = "O Verdadeiro Final (juro)",
            corDoFundo = new Color32(240, 160, 130, 255),
            placas = new[]
            {
                "Agora sim, a última. Confia.",
                "A bandeira é logo ali. Corre!",
                "Pronto, chegou! Pode encostar na bandeira.\nEssa é de verdade. Palavra de Puck.",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "                     BBBBB",
                "                       T                                                          BBBB",
                "                                                                                    v",
                "",
                "                                                  ?K                  $$$          $$",
                "                 $$$               $$                                                                        $m$",
                "                      U                                U    i R    h       s   h        E        iB Z               *",
                "  P i $m$                r  e           <                 ##################################    #########CCC############",
                "#############   ############### MM     ########CC###########################################    #########CCC############",
                "#############   ###############        ########CC###########################################    #########CCC############",
            },
        },
    };

    // A fase secreta: aparece no mapa (numa ilha do lago) se você pegar TODAS as moedas da fase do segredo.
    public const int IndiceSecreto = 99;
    public const int FaseDoSegredo = 4; // "Eu mudo de ideia" (o Puck avisa: "Pegue as moedas! Todas!")

    public static readonly Fase Secreta = new Fase
    {
        nome = "Santuário do Puck (secreta)",
        corDoFundo = new Color32(250, 230, 150, 255), // dourado
        placas = new[]
        {
            "Você achou a fase secreta!\nAqui não tem pegadinha. Juro de verdade.",
            "Viu? Só moedas. Pode confiar.",
            "...tá, só uma.",
        },
        mapa = new[]
        {
            "",
            "",
            "",
            "",
            "",
            "        $$$                $$$                  $$$",
            "       $   $              $   $                $   $",
            "",
            "",
            "              ?  ?  ?            $$$",
            "                                BBBBB",
            "                                                       $$$",
            "  P i    S  $$$             S         i          S          i   R     *",
            "######################   ##################   ##########################",
            "######################   ##################   ##########################",
        },
    };

    // Dados de uma fase pelo índice (inclui a secreta).
    public static Fase Dados(int indice) => indice == IndiceSecreto ? Secreta : Todas[indice];

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
        "Porta errada, kashira.",
        "Quem é Rem? Agora você sabe.",
        "A corrente rangeu. Você não ouviu.",
        "Faxina concluída. O lixo era você.",
        "Era só pular. Era SÓ pular.",
        "A Ram soprou. Você voou. Ela nem olhou.",
        "Barusu, até as folhas sabem para onde ir.",
        "O redemoinho desligou. Que coincidência, né?",
        "Segurar pra frente era opcional. Morrer, não.",
        "Atravessou o compasso. E o bloco.",
        "A Ram não espera ninguém. Muito menos você.",
        "Tic, tic, TAC... e tchau.",
        "Fora do ritmo, fora da vida.",
        "Cristal vermelho = pula. Anotou?",
        "O Roswaal não dá garantia das plataformas.",
        "Pulou da expressa? Clássico.",
        "A plataforma tremeu. Você também.",
    };
}
