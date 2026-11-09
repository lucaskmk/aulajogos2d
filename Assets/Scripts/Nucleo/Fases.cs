using UnityEngine;

// =====================================================================
//  AS FASES DO JOGO
// =====================================================================
//  Cada fase é desenhada com texto! Cada caractere é um bloco de 1x1.
//  A linha de cima do texto é o topo da tela. Todas as fases têm 15 linhas.
//
//  A VIAGEM (13 fases, cada uma com um lugar e um clima só dela): 3 fases e a Capitã, 2 fases e a Cozinheira,
//  2 fases e a Baleia Branca, 2 fases e o Kraken Vermelho (o chefe final, que mandou todo mundo atrás de você).
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
//   e  inimigo (igual ao 'E': todos os caranguejos morrem com um pisão)
//   M  bloco que foge quando você pula perto dele
//   W  bandeira que te manda de volta pro último checkpoint (sem checkpoint: pro começo)
//   X  linha invisível que inverte os controles quase na hora (0,4 s) (cruzou de novo, desinverte).
//      Só UMA fase usa isso, e só num trecho (é difícil demais para usar sempre)
//   U  cano (2 de largura, desce até o chão): de tempos em tempos sai uma MÃO dele. Encostou, morreu
//   r  rato (Rato do Porão): pula atrás de você e se MULTIPLICA. Pise em cima para derrotar
//   w  Baleia Branca (CHEFE): começo da arena de 24 blocos, na linha em que o jogador anda. O chão da arena precisa
//      ter buracos (vazios) nas colunas 7-8 e 15-16 contadas a partir do 'w': a Baleia tampa com blocos frágeis
//      que desmoronam na etapa 3. Entrou, a arena fecha e a luta começa (3 etapas; pule na cabeça dela quando ela cair)
//   u  CAPITÃ (CHEFE): começo da arena de 20 blocos, na linha em que o jogador anda (chão LISO, sem buracos).
//      Entrou, a arena fecha e a luta começa (3 etapas; quando a âncora dela ficar PRESA no chão, pule na cabeça dela).
//      Na etapa 3 ela fica FURIOSA. Veja ChefeCapita.cs
//   g  KRAKEN VERMELHO (CHEFE FINAL): começo da arena de 21 blocos, na linha em que o jogador anda. Embaixo da arena NÃO
//      tem chão no mapa: o convés é dela (tábuas que ela quebra), e logo depois vem o MAR (4 colunas vazias), de onde ela sai. Os tentáculos VERMELHOS matam; o AMARELO (o agarrão) não mata,
//      mas ROUBA suas moedas. Quando ela CANSA e encosta a cabeça no chão, pule nela. Veja ChefeKraken.cs
//   L  Sereia (esperando no fim da última fase)
//   c  baú do tesouro: o fim da última fase (funciona como a bandeira, e depois vem a escolha: devolver ou roubar)
//   0-9  porta da Dóris (2 de altura), com o número em cima: aperte S/seta para baixo na frente dela.
//      Para onde cada porta leva fica na lista "portas" da fase, em pares de ida e volta: "1-4" = a 1 leva
//      para a 4 e a 4 volta para a 1. É SEMPRE igual (dá para decorar o caminho)
//   Y  Dóris (fala como as placas do Louro; os textos entram na mesma lista "placas")
//   s  ponto de save: encostou, você renasce ali. Mas às vezes ele "muda de lugar" (volta pro anterior)... :)
//   j  plataforma mágica: vai para a direita e volta (até 6 blocos ou até encostar em algo) e leva você junto.
//      Blocos 'j' lado a lado na mesma linha = uma plataforma só. Todas começam juntas, indo para a direita
//   J  IGUAL à 'j'... mas 0,5 s depois que você sobe, o cristal fica VERMELHO, ela treme e DESPENCA
//      (reaparece no lugar 1,5 s depois, no mesmo ritmo das outras)
//   D  IGUAL à 'j'... mas o cristal fica AMARELO, ela treme e DISPARA até bater numa parede, freia de
//      repente e te joga para a frente. Quem pula quando ela treme cai no abismo
//   Q  mangual da Capitã: eixo de ferro (sólido, dá para subir) com uma bola de espinhos girando na corrente
//      (raio 2,75). Só a BOLA mata. A COLUNA do 'Q' escolhe onde a bola começa e o sentido do giro
//      (coluna % 8, tabela Jeitos em Ancora.cs). Com chão em cima ele fica ENTERRADO (parece chão)
//   q  IGUAL ao 'Q'... mas quando você chega perto a corrente range, a Capitã fica brava e a bola volta
//      girando para o OUTRO lado, 2x mais rápido
//   y  vento do mar: células 'y' encostadas = UMA zona (o retângulo em volta delas; pode ter moedas e blocos dentro).
//      Empurra para a ESQUERDA em rajadas, todas juntas: calmo 1,2 s, aviso 0,4 s (folhas e riscos começam a passar), rajada 1,4 s.
//      Segure para a frente, ou se esconda até 2 blocos à esquerda de um bloco sólido (o vento não atravessa paredes)
//   n  redemoinho: zona igual à 'y' que levanta o Grumete até o topo dela (ele fica boiando lá). Se a coluna da ESQUERDA
//      da zona for ÍMPAR, ele é TRAIÇOEIRO: quando você chega perto (4 blocos), engasga (fica cinza) e desliga por 1,5 s
//   a  bloco do ritmo AZUL (maré): sólido e apagado se revezam com o 'b' a cada 1,4 s (piscam e fazem tic-tic-TAC antes)
//   b  bloco do ritmo ROSA (coral): aceso quando o 'a' está apagado, e vice-versa. Blocos iguais lado a lado = um só
//   A  IGUAL ao 'a' (pisca e acende junto)... mas nunca fica sólido de verdade
//   t  esmagador do ritmo: despenca toda vez que os 'a' acendem (treme e fica de olho vermelho antes). Rosa aceso = pode passar
//   l  Cozinheira nas sombras (de longe, só dois olhos que brilham e somem). Chegou perto (14 blocos) e na mesma altura,
//      ela ARREMESSA uma faca em linha reta em você a cada 1,6 s. Olhos VERMELHOS = vai jogar (0,4 s).
//      A faca tem luz própria e crava no que for sólido (esconder atrás de um tijolo funciona). A Cozinheira não mata, a faca sim
//   k  IGUAL à 'l'... mas joga TRÊS facas em LEQUE. Colocada no alto, joga para baixo em qualquer ângulo: não pare embaixo dela
//   H  COZINHEIRA (CHEFE): começo da arena de 24 blocos, na linha em que o jogador anda (chão reto; o resto da arena
//      vazio: os dois degraus de dentro ela mesma cria). Luta no ESCURO (use com escura = true): só os olhos aparecem.
//      3 etapas; ache o LAMPIÃO, e no clarão pule na cabeça dela (veja ChefeCozinheira.cs)
//   N  TSUNAMI: a onda gigante que o Kraken levantou, que PERSEGUE você a partir desta coluna (acorda quando você se
//      afasta 4 blocos; anda 5,2 b/s e acelera se você abrir vantagem). Encostou, morreu. No checkpoint ela nasce 9 blocos atrás
//
//  As placas (quem fala é o Louro) recebem os textos na ordem da esquerda para a direita.
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
    public string[] portas; // pares de portas ligadas, ex.: "1-4" (só nas cabines do Navio Fantasma)
    public bool noite;      // fase de noite: céu escuro com lua e estrelas, luzinhas acesas, vaga-lumes
    public bool escura;     // fase no ESCURO: quase não se vê nada, só em volta do Grumete e das luzinhas
    public bool petalas;    // pétalas caindo (no lugar do pólen)
    public Ambiente.Tipo? clima; // as partículas da fase (folhas, chuva, bolhas...); null = pelo jeito da fase (veja PrepararClima)
    public Bioma bioma;          // o LUGAR: o chão, os enfeites, o bicho e o fundo pintado (veja Biomas.cs)
    public Color32? corDoFundo; // cor do céu da fase (tons pastel, de livro ilustrado)
    // Medalha do relógio: passar a fase em até tantos segundos (contando desde a entrada pelo mapa, com as mortes).
    // 0 = automático, pelo tamanho da fase (veja Medalhas.TempoAlvo).
    public float tempoAlvo;
    // Modo difícil: a velocidade do tsunami nesta fase (blocos por segundo). 0 = a normal (5,2). O elástico mantém a onda
    // perto de quem corre bem; as fases que obrigam a esperar (plataformas, ritmo, vento) precisam de uma onda mais lenta.
    public float velocidadeDaOnda;
    // O ponto de save desta fase NUNCA "muda de lugar" (a pegadinha da 3ª morte fica desligada).
    public bool saveFiel;
}

public static class Fases
{
    public static readonly Fase[] Todas =
    {
        // o PORTO do Cilada, de manhã
        new Fase
        {
            nome = "Bem-vindo :)",
            bioma = Bioma.Porto,
            corDoFundo = new Color32(150, 215, 235, 255),
            placas = new[]
            {
                "Bem-vindo a bordo do Cilada à Vista!\nA/D ou SETAS para andar, ESPAÇO para pular.",
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
                "  P i       i       $$h E        E  ^                   i ^ R            E  *",
                "###############  ##########CCCC################   ###############   ##############",
                "###############  ##########CCCC################   ###############   ##############",
            },
        },

        // a SELVA das palmeiras, folhas caindo (a serra mora no fim dela)
        new Fase
        {
            nome = "Confia em mim (e corre!)",
            bioma = Bioma.Selva,
            corDoFundo = new Color32(245, 190, 120, 255),
            clima = Ambiente.Tipo.Folhas,
            placas = new[]
            {
                "Essa fase é tranquila. Juro.",
                "Chão 100% seguro à frente.",
                "Última bandeira. Agora é sério.",
                "Sobreviveu à bandeira? A selva continua!\nE não olhe para trás.",
                "Cuidado com o que vem pela frente!",
            },
            mapa = new[]
            {
                "",
                "",
                "                v",
                "",
                "",
                "                           BBBBBBBBB",
                "                             T   T                                                                                                  BBBBBBBB           *",
                "                  ######                                                                                                              v  v          BBBBBB",
                "                  ######",
                "          o       ######   ?                                                    ??                    $$$",
                "                  ######                                                             $$               BBB                            $$$",
                "         B        ######     $$$           $$                                                     ^^         s    i  ^^     >              E  R   S",
                "  P i    B      S ######  ^      E  i        i  E B Z            <s i  ^                  E   ###############################CCCC###########################",
                "######################################FFF#################   ###############  ######   ######################################CCCC###########################",
                "######################################FFF#################   ###############  ######   ######################################CCCC###########################",
            },
        },

        // a LAGOA cor-de-rosa, com pétalas
        new Fase
        {
            nome = "O Final (mudei de ideia)",
            bioma = Bioma.Lagoa,
            corDoFundo = new Color32(240, 150, 205, 255),
            petalas = true,
            placas = new[]
            {
                "Última fase. Sem pegadinhas.\nPalavra de escoteiro.",
                "Pula com força! (ou não)",
                "Mudei de ideia: a fase continua! Pegue as moedas!\nTODAS! (as de trás também contam)",
                "Decorou a fase? Que pena.",
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
                "                                                                                                              BBBBBB",
                "                                                                                                                v",
                "                    ##    ?K?                            IIII                                                               ?K",
                "                                              o                                                                                    $$",
                "              ##  FF                U        $$                                                                           U",
                "  P i h                     ^  $$$     r          E  ^i             ^^ E  s i  $$ m$            h  E          rr              ^^ i    E  G",
                "###########            ##################CCC#############    ##########################   #############CCC############   #####################",
                "###########            ##################CCC#############    ##########################   #############CCC############   #####################",
            },
        },

        // o FORTE da Capitã, de tarde
        new Fase
        {
            nome = "A Capitã Âncora",
            bioma = Bioma.Forte,
            tempoAlvo = 170f, // chefe: a luta conta (estimativa; ajuste depois de testar)
            velocidadeDaOnda = 4f, // (os manguais pedem um tempinho)
            corDoFundo = new Color32(165, 185, 240, 255),
            placas = new[]
            {
                "O navio da Capitã Âncora! Ela limpa o convés.\nInclusive de visitas. Com uma ÂNCORA.",
                "Só a ÂNCORA mata. A corrente é enfeite.\nDica: em cima do eixo ela não te alcança.",
                "Ouviu esse barulho de corrente? É a Capitã.\nVai lá dar oi. Eu espero aqui. Bem aqui. Longe.",
                "O convés da Capitã. Só a ÂNCORA mata.\nÂncora PRESA no chão = pula na CABEÇA dela! Morreu? Volta na ETAPA.",
                "Você venceu a Capitã! Ela vai lembrar disso.\nPirata não esquece. Corre pra bandeira.",
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
                "      ?B?                    Q      Q                            Q",
                "            $$   $                                 $$                  $$$",
                "",
                "  P i      i     Q     E  ^     $$     ^ i     Q        ^^                  q   E     s i   u                      i     G",
                "####################################################  ########  #####   ######################################################",
                "####################################################  ########  #####   ######################################################",
            },
        },

        // a VILA de ponta-cabeça, poeira dourada no fim da tarde
        new Fase
        {
            nome = "Cadê a direita? (Foge, bloco!)",
            bioma = Bioma.Vila,
            velocidadeDaOnda = 4f, // (o trecho de controles invertidos)
            corDoFundo = new Color32(195, 165, 230, 255),
            clima = Ambiente.Tipo.Poeira,
            placas = new[]
            {
                "Tudo normal por aqui. Pode andar.",
                "Esquerda é direita. Ou era?",
                "Os controles voltaram ao normal. Acho.\nEsses blocos daqui são meio tímidos...",
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
                "                                                                                                                                                                    BBBBB",
                "                                                                                                                                                 $                    T",
                "                    ?     ?                                                                                                                     BBB",
                "                                                                            $$",
                "          $$$                                                                                                                 $$$                      U",
                "  P i     ^^    e             ^  e   E       is X    ^    E         ^         ^^  E X    h        s i                 i B  W   ^         E  $m$     h     rr  >        G",
                "#####################   #################CCC##################   ######   ####################   ########## MM     ################   ######################################",
                "#####################   #################CCC##################   ######   ####################   ##########        ################   ######################################",
            },
        },

        // a ABORDAGEM: os navios piratas lado a lado, no pôr do sol, e as plataformas mágicas da Marina nos vãos
        new Fase
        {
            nome = "Abordagem!",
            bioma = Bioma.Navios,
            velocidadeDaOnda = 3f, // (a plataforma leva 4,8 s para dar a volta)
            corDoFundo = new Color32(250, 165, 120, 255),
            placas = new[]
            {
                "ABORDAGEM! Os navios estão lado a lado. Pula de convés em convés!\nNos vãos, a Marina mandou plataformas mágicas. Sobe que ela te leva.",
                "Essa aqui é IGUALZINHA à outra.\nPode subir sem medo!",
                "Relaxa e curte o passeio.\nNenhuma serra à vista!",
                "EXPRESSA PARA O ÚLTIMO NAVIO!\nSe ela tremer, NÃO PULE. (dessa vez é sério)",
                "Chegou! Viu só? Era só confiar no Louro. :)",
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
                "                           ?                  T            o                                                                                                                ?",
                "      ?K?                                                    $ $ $     ?K                  ?                  $$        ?",
                "                                    $ $  $$               jj                                               JJ                                                    $$             $m$",
                "          $$                $$     jj                                        $$                          $m$           $                         $ $                                         $",
                "  P i  ^         $$$    i   ^     $        ^      s  jjj               E   i  h    $$$         >B s  jjj                ^   i      $ $ $ $       h     s      jjj        h    >          E    i   G",
                "###############jj      #########JJ      #############               #############jj      ############                #########DDD             #############          ##########  ####   ############",
                "###############        #########        #############               #############        ############                #########                #############          ##########  ####   ############",
            },
        },

        // a GRUTA escura (a cozinha que ela montou depois de ser expulsa do navio fantasma)
        new Fase
        {
            nome = "A Gruta da Cozinheira",
            bioma = Bioma.Gruta,
            tempoAlvo = 190f, // chefe
            velocidadeDaOnda = 4f, // (no escuro, e com facas)
            escura = true,
            corDoFundo = new Color32(55, 35, 80, 255),
            placas = new[]
            {
                "A gruta da Cozinheira. Expulsaram ela do navio fantasma...\nNo escuro, viu OLHOS VERMELHOS? PULA. Ou corre. Ou reza.",
                "Esse caixote é à prova de faca. Se esconde atrás dele!\nE a moeda ali em cima também é de confiança. :)",
                "A cozinha dela. Tá um BREU. Ache o LAMPIÃO,\ndepois pule na CABEÇA dela. Morreu? Volta na mesma ETAPA.",
                "Você venceu a Cozinheira! Ela fugiu rindo, mas conta.\nA bandeira é logo ali. Essa não tem faca. Eu acho.",
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
                "                                                   k",
                "       $$     $$    m                $$                  $$    $$",
                "                         l",
                "                         B",
                "  P i           l ^i  B  B       l        B   h    B   ^ E B  l  s i    H                          i      G",
                "##########  ################  #####   ##########################################################################",
                "##########  ################  #####   ##########################################################################",
            },
        },

        // o NAVIO FANTASMA: as cabines da Dóris e, no convés, o baile das marés (almas flutuando)
        new Fase
        {
            nome = "O Navio Fantasma",
            bioma = Bioma.NavioFantasma,
            // As cabines: a 1 ou a 3 levam à sala das armadilhas, a 6 à sala das moedas, a 8 à Dóris (a 2 é o beco
            // dos caranguejos). Da sala da Dóris, a parede da direita está aberta: o convés do baile.
            portas = new[] { "1-4", "3-5", "2-9", "6-7", "8-0" },
            tempoAlvo = 170f, // as portas tomam tempo
            velocidadeDaOnda = 2.5f, // (a escalada dos blocos do ritmo pede uns 4 s para andar 12 colunas)
            noite = true,
            corDoFundo = new Color32(120, 150, 150, 255),
            clima = Ambiente.Tipo.Almas,
            placas = new[]
            {
                "Cada porta tem um número e SEMPRE leva para o mesmo lugar.\nDecore o caminho! (S para entrar)",
                "Beco sem saída! Mas tem caranguejo.\nA porta 9 sempre volta para a 2.",
                "Achou a porta certa. Buuu.\nAgora sai pela direita e vai DANÇAR no convés.",
                "Blocos da maré (azul) e do coral (rosa) se revezam.\nTic, tic, TAC: trocou! Pise no que tá aceso.",
                "Pule no TIC, caia no TAC.\nA maré não espera ninguém.",
                "Esses aí caem quando o AZUL acende.\nRosa aceso = caminho livre. (dessa vez é sério)",
                "Agora é só ritmo puro, sem pegadinha.\nPalavra de papagaio! :3",
                "Ufa, chegou! A bandeira tá logo ali.\nPode ir andando, sem pressa. :)",
            },
            mapa = new[]
            {
                "#############################################################",
                "#############################################################",
                "#           #         v #           #           #                                                                     ?",
                "#           #           #           #           #",
                "#           #           #           #           #                                                               $",
                "#           #           #           #           #                                                                bb ####",
                "#           #           #           #           #                       $                              v     $      ####                                                                 $$",
                "#           #           #           #           #  BBBBBB                                                     aa    ####                                                             $       m",
                "#  BBBBBB   #           #           #           #                                                                   ####     BBBBBBBBBBBBBBBBBBBBBBBBBB                      $                   $",
                "#           #           #  BBBBBB   #           #                      ?B?                ?                bb       ####      tt      tt      ttt                   ?K?                aa                       $$",
                "#           #           #           #      $    #                                   $m$         $$                  ####                                      $                aa              bb",
                "#           #           #   $$$m    #           #                                                       aa          ####                                                           bb      bb",
                "# Pi 1  2 3 # 4 h 5   6 # 7   E   8 # 9 i e  E  # 0   Y     ^^    s i   h         h      i                ^^^^^^^^^^#### s i           $       $        i            s     bb                      aaa  i   b Z       G",
                "############################################################################aaaa####bbbb#####aaa  bbb############################bbb####bbbb#######aaa#####aaaAaaa########                             ########aaaa#######",
                "############################################################################^^^^####^^^^#####^^^^^^^^############################^^^####^^^^#######^^^#####^^^^^^^########^^^^^^^^^^^^^^^^^^^^^^^^^^^^^########^^^^#######",
            },
        },

        // o PENHASCO DOS VENTOS, rosado
        new Fase
        {
            nome = "Vento em Popa!",
            saveFiel = true, // (muitos checkpoints de propósito: morrer aqui não pode custar o caminho todo)
            bioma = Bioma.Penhasco,
            velocidadeDaOnda = 3.5f, // (e a rajada segura a onda também: veja Tsunami.Perseguir)
            corDoFundo = new Color32(245, 195, 215, 255),
            clima = Ambiente.Tipo.Poeira, // o vento do deserto levanta areia
            placas = new[]
            {
                "O vento do mar tá de mau humor hoje.\nQuando as FOLHAS vierem, segure pra frente ou se esconda atrás de um bloco!",
                "Redemoinho! Entra que ele te leva lá pra cima.\nDepois é só segurar pra direita.",
                "Outro redemoinho, igualzinho ao primeiro!\nPode ir direto, sem medo. :)",
                "Dica de graça: espere o redemoinho te levantar antes de sair.\n(essa é de verdade. A outra... nem tanto)",
                "Pronto! O vento cansou e foi tirar um cochilo.\nAcabou a ventania. A bandeira é logo ali!",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "                                                                                                                                         nnnnn$$$$nnnnn           yyyy                   yyyyyyyyyyyyyyyyyyyy",
                "                                                                                                                  yyyyyyyyyyyyyyyyyyyy   nnnnnnnnnnnnnn         $nyyyynnn  n$n           yyyyyyyyy$$$yyyyyyyy",
                "                                                    $$                                            nnnn$$$$nn      yyyyyyyyyyyyyyyyyyyy   nnnnnnnnnnnnnn         nnyyyynnn  nnn           yyyyyyyyyyyyyyy?yyyy",
                "                                                                                                  nnnnnnnnnn      yyyyyyy$yyy$m$yyyyyy   nnnnnnnnnnnnnn         nnyy$ynnn  nnn           yyyyyyyyyyyyyyyyyyyy",
                "           yyyyyyyyyyyyyyyyyyyyyyyyyyyy                                                           nnnnnnnnnn      yyyyyyyyyyyyyyyyyyyy   nnnnnnnnnnnnnn         nnyyyynnn  nnn           yyyyyyyyyyyyyyyyyyyy",
                "           yyyyyyyy$$yyyyyyyyyyyy?yyyyy               yyyyyyyyyyyyyyyyyy yyyyyyyyyyyyyyyyyyyyyyy  nnnnnnnnnn   s  yyyByyyyyyyyyyEyByyyis nnnnnnnnnnnnnn   s i   nnyyyynnn  nnn   s  i    yyyyBByyZyyyyByyEyBy  G",
                "           yyyyyyyyyyyyyyyyyyyyyyyyyyyy         I ?K  yyyyyyyyy?yyyyyyyy yyyyyyyy?yyyy$yyyyyy?yy  nnnnn$$nnn ###########   ##############nnnnnnnnnnnnnn ####### nnyyyynnn  nnn  ######CC#####################",
                "           yyyyyyyyyyyyyyyyyyyyyyyyyyyy        $$     yyy$$yyyyy$$yyyyyy yyyyyyyyyyyyyyyyyyyyyyy  nnnnnnnnnn ###########   ##############nnnnnnnnnnnnnn ####### nnyyyynnn  nnn  ######  #####################",
                "           yyyyyyyyyyyyyyyyyyyyyyyyyyyy               yyyyyyyyyyyyyyyyyy yyyyyyyyyyyyyyyyyyyyyyy  nnnnnnnnnn ###########   ##############nnnnnnnnnnnnnn ####### nnyyyynnn  nnn  ######  #####################",
                "  P i      yyyyyyByy^^yyEyyyyyyyByyyEyy  ^^   s     S yyyyyyBy^yyyyByyEysyy^^yyEyyByyyyyyyyhyEyysinnnnnnnnnn ###########   ##############nnnnnnnnnnnnnn ####### nnyyyynnn  nnn  ######  #####################",
                "###########################   #######################################################   #############nnnnnnn ###########   ##############nnnnnnnnnnnnnn ####### nnyyyynnn  nnn  ######  #####################",
                "###########################   #######################################################   #############nnnnnnn ###########   ##############nnnnnnnnnnnnnn ####### nnyyyynnn  nnn  ######  #####################",
            },
        },

        // o MAR ABERTO de noite
        new Fase
        {
            nome = "A Baleia Branca",
            saveFiel = true, // (muitos checkpoints de propósito: morrer aqui não pode custar o caminho todo)
            bioma = Bioma.Recife,
            tempoAlvo = 250f, // chefe
            noite = true,
            corDoFundo = new Color32(70, 60, 120, 255),
            placas = new[]
            {
                "O mar da Baleia Branca.\nDizem que ela já afundou cem navios. Não afunda você, tá?",
                "É agora. Respira.\nMorreu? Volta na mesma ETAPA. Decora os padrões!",
                "Você venceu a Baleia Branca!\nMas quem mandou ela atrás de você? Algo VERMELHO lá no fundo...",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "                                                          BBBBBBB",
                "                                                           v  v",
                "",
                "           o      o          BBBBBBBBB            o                      $$$",
                "                               v   v                                     BBB    o",
                "",
                "  P i              ^^                  h     s       E       $      h        E       E  h  s i  w                             i        G",
                "###########   ##########CCC##############  #####   #######CC##CC##   ######   #########################  ######  ###########################",
                "###########   ##########CCC##############  #####   #######CC##CC##   ######   #########################  ######  ###########################",
            },
        },

        // a PRAIA das ilhas do Kraken, debaixo de tempestade (o tsunami vem do mar)
        new Fase
        {
            nome = "O Tsunami (CORRE!)",
            bioma = Bioma.Praia,
            corDoFundo = new Color32(120, 140, 150, 255), // tempestade de dia: cinza-esverdeado
            clima = Ambiente.Tipo.Chuva,
            placas = new[]
            {
                "Que praia bonita... ouviu isso? O Kraken levantou um TSUNAMI!\nNão olha para trás. Só CORRE e PULA!",
                "Buraquinho de nada!\nPula bem na beiradinha. :)",
                "Mola para o alto! Lá em cima tem moedas...\ne nuvens fofinhas. MUITO fofinhas.",
                "Reta final! A bandeira tá logo ali.\nDessa vez ela não foge. Juro pela Marina.",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "                                                                                               o",
                "",
                "                                                                                       $$$",
                "",
                "                                                    ?                                   BBBBBB    $m$                                                                                        ?",
                "                                                                                                                                                                    $",
                "                 ?         $                 $$                  $       $$                      BBBBBB                   $$$             ?                                                     $$",
                "         $$           $$              $$                    $$      $$                                      $$                                                                 $$$",
                "                                                  ######                                                                                                            h                        ##                *",
                " N  P i   ^         ######     i h           ^^   ######                       s i  S                         h            ^^       h        h           ^     > #######  s i           ##   ##    ##  R $  ######",
                "##############  ##########   ########CC  ###################CC##CCC#CC##CCCC#########                    ##########   ##########  ##CCCC####   ###### #########################FFF###   ##   ##    ######C  ######",
                "##############  ##########   ########CC  ###################CC##CCC#CC##CCCC#########                    ##########   ##########  ##CCCC####   ###### #########################FFF###   ##   ##    ######C  ######",
            },
        },

        // o ROCHEDO do pôr do sol
        new Fase
        {
            nome = "O Verdadeiro Final (juro)",
            bioma = Bioma.Rochedo,
            corDoFundo = new Color32(240, 160, 130, 255),
            placas = new[]
            {
                "Agora sim, a última. Confia.",
                "A bandeira é logo ali. Corre!",
                "Pronto, chegou! Pode encostar na bandeira.\nEssa é de verdade. Palavra de Louro.",
            },
            mapa = new[]
            {
                "",
                "",
                "",
                "",
                "",
                "                                       BBBBBBB                                               BBBBB",
                "                                         T  T                                                  T                                                          BBBB",
                "                                                                                                                                                            v",
                "",
                "                 $$                                     ?K?                                                               ?K                  $$$          $$",
                "                                                                                         $$$               $$                                                                        $m$",
                "                             $$$                                                              U                                U    i R    h       s   h        E        iB Z               *",
                "  P i  ^ e      $    ^^^            ^        E     h  E  B Z          >   E   $m$                r  e           <                 ##################################    #########CCC############",
                "###############CCC###########    ##############MM##############CCCC##################   ############### MM     ########CC###########################################    #########CCC############",
                "###############CCC###########    ##############  ##############CCCC##################   ###############        ########CC###########################################    #########CCC############",
            },
        },

        // o ALTO-MAR, debaixo de tempestade: de navio em navio até o convés do navio onde o Kraken ataca
        new Fase
        {
            nome = "O Kraken Vermelho",
            bioma = Bioma.AltoMar,
            tempoAlvo = 260f, // chefe final
            noite = true,
            corDoFundo = new Color32(95, 30, 45, 255),
            clima = Ambiente.Tipo.Chuva,
            placas = new[]
            {
                "Alto-mar! A lula gigante VERMELHA que mandou a Baleia e o tsunami\nprendeu a Marina naquele navio. Pula de convés em convés!",
                "Ela ROUBA moedas: tentáculo AMARELO não mata, mas leva as suas!\nQuando ela CANSAR e encostar a cabeça no chão, PULA NELA!",
                "Você venceu o Kraken! Ela devolveu suas moedas e fugiu\npro fundo do mar. A Marina tá logo ali. Agora é de verdade!",
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
                "                        BBBBB",
                "     ?K?                  T                $$$",
                "",
                "                U",
                "  P i              $m$      E           r  ^    s i   g                         i         L    c",
                "##########   ############CC######    #################                         #####################",
                "##########   ############CC######    #################                         #####################",
            },
        },

    };

    // A fase secreta: aparece no mapa (numa ilha do lago) se você pegar TODAS as moedas da fase do segredo.
    public const int IndiceSecreto = 99;
    public const int FaseDoSegredo = 2; // "O Final (mudei de ideia)" (o Louro avisa: "Pegue as moedas! TODAS!")

    public static readonly Fase Secreta = new Fase
    {
        nome = "Ilha do Louro (secreta)",
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

    // A fase tem a coisa da etiqueta de uma mensagem de morte? (pelas letras do mapa)
    public static bool TemNaFase(Fase fase, string etiqueta)
    {
        string letras;
        switch (etiqueta)
        {
            case "kraken": letras = "g"; break;
            case "baleia": letras = "w"; break;
            case "capita": letras = "u"; break;
            case "ancora": letras = "uQq"; break;
            case "cozinheira": letras = "Hlk"; break;
            case "tsunami":
                if (GerenciadorDoJogo.Tempestade) return true; // no modo difícil, toda fase tem
                letras = "N";
                break;
            case "vento": letras = "yn"; break;
            case "ritmo": letras = "abAt"; break;
            case "plataforma": letras = "jJD"; break;
            case "chefe": letras = "uHwg"; break;
            case "porta": return fase.portas != null;
            default: return true;
        }
        foreach (string linha in fase.mapa)
            if (linha.IndexOfAny(letras.ToCharArray()) >= 0) return true;
        return false;
    }

    // A fase tem alguma destas letras no mapa? (as poções da Loja só são gastas em fase que tenha o que mostrar)
    public static bool TemLetras(Fase fase, string letras)
    {
        foreach (string linha in fase.mapa)
            if (linha.IndexOfAny(letras.ToCharArray()) >= 0) return true;
        return false;
    }

    // Dados de uma fase pelo índice (inclui a secreta).
    public static Fase Dados(int indice) => indice == IndiceSecreto ? Secreta : Todas[indice];

    // Frases que aparecem quando você morre (escolhidas aleatoriamente).
    // Uma frase que fala de uma coisa específica começa com a ETIQUETA dela ("cozinheira:..."): ela só aparece numa fase
    // que tem aquilo (veja Fases.TemNaFase). Sem etiqueta, vale em qualquer fase.
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
        // --- personagens ---
        "A maré te trouxe de volta...",
        "O Kraken mandou lembranças.",
        "Afundou. Mas a maré devolve.",
        "A Marina ficaria decepcionada.",
        "O Louro viu tudo. E vai contar pra todo mundo.",
        "Foi o caranguejo. Ou o chão. Ou os dois.",
        "Grumete que é grumete já passou por coisa pior.",
        "porta:Porta errada. Buuu.",
        "cozinheira:A Cozinheira já ia te pôr na panela.",
        "cozinheira:Olho vermelho era pra PULAR, grumete.",
        "cozinheira:Três facas. Você pulou direto na de cima. Clássico.",
        "cozinheira:Quem apagou a luz? Spoiler: foi a Cozinheira. Em você.",
        "kraken:O Kraken só queria um abraço de oito braços.",
        "tsunami:Glub. Glub. Glub. Glub.",
        "tsunami:Parou para respirar? O tsunami agradece.",
        "Volta da Maré, patrocinada pelo Kraken.",
        "tsunami:Tsunami: 1. Grumete: 0.",
        "tsunami:O Kraken levantou uma onda só pra você. Que honra.",
        "kraken:O tentáculo era VERMELHO. Vermelho mata, lembra?",
        "kraken:O Kraken te deu um abraço. Oito, na verdade.",
        "kraken:Ela cansou e você não pulou. Agora quem cansou foi você.",
        "kraken:Pelo menos ela não levou suas moedas dessa vez. Ou levou?",
        "baleia:A Baleia Branca te engoliu. E cuspiu. Eca.",
        "baleia:Pula na CABEÇA dela, não na boca.",
        "baleia:O capitão Ahab levou anos caçando ela. Calma.",
        "chefe:Decorou o padrão? Ela também decorou você.",
        "capita:Quem é a Capitã Âncora? Agora você sabe.",
        "ancora:A corrente rangeu. Você não ouviu.",
        "capita:Convés limpo. O lixo era você.",
        "capita:A âncora ficou presa no chão. Você também. Embaixo dela.",
        "chefe:Linha baixa é pra PULAR. Linha alta é pra NÃO pular. Anota.",
        "capita:Dentro do anel, colado nela, era seguro. Era. Até você pular.",
        "capita:A Capitã ficou furiosa. Você virou isca de peixe.",
        "capita:A âncora cravou na parede... e voltou. Óbvio.",
        "Era só pular. Era SÓ pular.",
        "vento:O vento soprou. Você voou. Ele nem olhou.",
        "vento:Grumete, até as folhas sabem para onde ir.",
        "vento:O redemoinho desligou. Que coincidência, né?",
        "vento:Segurar pra frente era opcional. Morrer, não.",
        "ritmo:Atravessou o compasso. E o bloco.",
        "ritmo:A maré não espera ninguém. Muito menos você.",
        "ritmo:Tic, tic, TAC... e tchau.",
        "ritmo:Fora do ritmo, fora da vida.",
        "plataforma:Cristal vermelho = pula. Anotou?",
        "plataforma:A Marina não dá garantia das plataformas.",
        "plataforma:Pulou da expressa? Clássico.",
        "plataforma:A plataforma tremeu. Você também.",
        "cozinheira:Era só pegar o lampião. Brilhando. No escuro. Era SÓ isso.",
        "cozinheira:A Cozinheira riu. Ela sempre ri. Você não.",
        "cozinheira:Olho roxo: tudo bem. Olho vermelho: tchau.",
        "cozinheira:A faca voltou. Bumerangue, sabe? Ela avisou.",
    };
}
