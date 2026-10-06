# Re:CILADA! — um jogo de plataforma nada confiável

Jogo 2D de plataforma "troll" (no estilo Cat Mario / Level Devil): tudo parece normal,
mas o chão cai, espinhos brotam do nada, a bandeira foge e as nuvens têm dentes.

Tema **Re:Zero**: você é o Subaru, os inimigos são Mabeasts e Grandes Coelhos, quem dá as "dicas" é o Puck
e cada morte é um *Retorno pela Morte*: a tela escurece, as mãos da Bruxa avançam e você renasce no pico do áudio
`Assets/Resources/Sons/retorno_pela_morte.mp3` (o momento do pico é detectado sozinho; para trocar o som,
é só substituir o arquivo mantendo o nome). No lugar das suas últimas mortes fica a marca da mão da Bruxa.
Na Biblioteca Proibida a Beatrice embaralha as portas, na última fase a Baleia Branca aparece,
e a Emilia espera o Subaru no final.

## Como jogar
1. Abra o projeto no Unity 6 (6000.6.2f1).
2. Abra `Assets/Scenes/SampleScene` e aperte **Play**. O jogo se monta sozinho.

| Tecla | Ação |
|---|---|
| A / D ou setas | andar |
| Espaço / W / ↑ | pular (segure para pular mais alto) |
| R | reiniciar a fase (conta como morte 😈) |
| Esc | pausar / continuar (no mapa: voltar ao título) |
| Q (na pausa) | voltar ao mapa (o progresso fica salvo) |
| Espaço / Enter (morto) | pular a animação da morte |
| ↑ / ↓ e Enter (título) | escolher no menu |
| Setas e Enter (mapa) | andar pelo caminho e entrar na fase |
| S / seta para baixo | entrar numa porta da Beatrice |
| Setas (pausa) | volume da música e dos efeitos |

Também funciona com controle (gamepad).

## Mapa do mundo
Depois do título vem um **mapa no estilo Super Mario World**, maior que a tela (ele rola para o lado):
a capital, o rio com a ponte, a floresta, os morros, o lago, o campo de flores, as montanhas e a mansão.
Cada ponto é uma fase e o Subaru anda pelos caminhos entre elas. Em cada ponto fica o "personagem"
daquela fase (Puck, Mabeast, serra, nuvem malvada, coelho...) e a Emilia espera na mansão.
- **O mapa começa coberto pela névoa** (o miasma da Bruxa): você só vê em volta das fases liberadas e não
  sabe onde fica a última. Ao passar de fase, a névoa some ao longo do caminho novo, o ponto da próxima fase
  aparece e o Subaru anda sozinho até ele. Quando a última fase é liberada, a névoa do mapa inteiro vai embora.
- Verde = já passou nesta partida, amarelo = liberada.
- Dá para voltar e jogar de novo qualquer fase liberada. Fases liberadas em partidas anteriores continuam
  abertas (bom para apresentar), mas pular fases pelo mapa tira a partida do recorde.
- Igual às fases, o mapa tem horizonte com paralaxe (céu, floresta e árvores ao longe, brilhinhos, nuvens e a
  Baleia Branca nadando no céu), sombras projetadas em tudo, sombras de nuvem passando pelo chão, árvores
  balançando, brilhos na água e a névoa "respirando".
- O mapa é desenhado em texto em `MapaDoMundo.cs` (legenda no próprio arquivo), igual às fases.

- Embaixo de cada ponto aparece uma **caveira com quantas vezes você morreu** naquela fase, e depois de cada
  fase o Puck, a Emilia ou a Beatrice **comentam** (as falas ficam em `Textos.cs`).
- **Fase secreta:** pegue TODAS as moedas da fase 5 ("Pegue as moedas! Todas!") sem morrer no meio. Uma ilha
  aparece no lago; no ponto 5, aperte seta para baixo para ir até ela.

## Biblioteca Proibida (o quebra-cabeça das portas)
A fase 8 é dividida em salas por paredes. Cada porta tem um **número** em cima e funciona em pares de
ida e volta, sempre iguais (dá para decorar): a lista `portas` da fase em `Fases.cs` diz quem leva para quem
(`"1-4"` = a 1 leva para a 4 e a 4 volta para a 1). Ao atravessar, aparece "Porta 1 -> porta 4" para ajudar
a lembrar, e você precisa esperar 1 segundo antes de entrar em outra porta. Uma das portas leva para um
beco sem saída cheio de Mabeasts; o caminho certo passa pela sala das armadilhas e pela sala das moedas
até a Beatrice. Aperte S na frente da porta.

## Controles invertidos
Na fase "Cadê a direita?", cruzar uma linha invisível começa uma contagem de 2 segundos antes de inverter
os controles (dá tempo de se preparar, e voltar antes cancela). A última fase não tem mais inversão.

## Chefe: a Baleia Branca
No fim da última fase, ao entrar na arena, paredes de névoa fecham a passagem, a câmera trava e toca a
música de chefe. A Baleia tem **3 de vida** (barra no topo da tela) e repete três ataques:
1. **Investida:** atravessa a tela alta (não pule) ou baixa (pule por cima); o "!" mostra a altura.
2. **Chuva de névoa:** bolas de névoa caem do céu; a sombra no chão mostra onde.
3. **Mergulho:** a sombra dela te segue e ela despenca de barriga. Fica **atordoada** (estrelinhas):
   **pule na cabeça dela** para tirar 1 de vida.

A cada golpe ela fica mais rápida e faz mais investidas. Derrotada, a névoa some e o caminho até a
bandeira e a Emilia abre. Se morrer depois disso, você renasce depois da arena.

## Ponto de save
O cristal (`s` no mapa) vira o lugar onde você renasce. Mas, como no anime, às vezes (25% das mortes) o
ponto de save "muda de lugar" e você volta para o começo. :) (Menos na fase da Baleia.)

## Conquistas, volume e créditos
- 15 conquistas (ver no título): morrer para a nuvem, pisar em 10 coelhos, desviar da Baleia, achar a
  Beatrice, zerar... Ficam salvas em PlayerPrefs (`Conquistas.cs`).
- Na pausa: setas para cima/baixo escolhem música ou efeitos, esquerda/direita mudam o volume.
- Depois de zerar vêm os créditos. **Coloque os nomes do grupo em `Textos.cs` (lista `Creditos`).**

No título dá para **Continuar** a partida salva (fica em PlayerPrefs: onde o Subaru está, mortes, moedas e tempo)
ou começar um **Novo jogo**.

A animação da morte pode ser pulada depois de 0,5 s e fica mais curta a partir da 3ª morte na mesma fase.

## Música
O jogo toca `Assets/Resources/Sons/musica_fundo` (mp3, ogg ou wav) se o arquivo existir.
Sem ele, cada fase toca uma música **original gerada por código** em clima "indie aconchegante" com harmonia de
**bossa nova** (`FabricaDeBossa`): violão de nylon dedilhado, baixo de bossa, flauta, chocalho e aro de caixa.
A luta contra a Baleia Branca tem uma música própria, mais rápida e tensa.
Se não existir, toca uma música **lofi 8-bit original gerada por código** (`FabricaDeMusica`), com uma variação por fase.
A música abaixa sozinha quando você morre (para a OST do Retorno pela Morte aparecer) e na pausa.

## Visual
- **Fundo em camadas com paralaxe:** céu em degradê, sol com raios de luz (ou lua e estrelas na fase noturna),
  montanhas, castelo, floresta, árvores, neblina perto do chão e uma folhagem escura bem na frente da câmera,
  que passa mais rápido que o chão. Cada camada anda numa velocidade diferente (`Paralaxe.cs`).
- **Pós-processamento do URP** (`EfeitosDeTela.cs`): bloom nas partes claras, vinheta roxa, saturação e contraste.
  Na morte a tela perde a cor e ganha aberração cromática; no renascimento, um "soco" de distorção de lente.
- **Luzes 2D** (`Luzes.cs`): moedas, blocos `?`, cristais de save, portas, bandeiras, lampiões e a mão do cano
  brilham. A última fase é de noite (o Subaru ilumina em volta) e a biblioteca tem luz quente.
- **Sombras:** a sombrinha projetada roxa em tudo e uma sombra oval no chão embaixo de quem está no ar.
- **Clima** (`Ambiente.cs`): pólen, pétalas nas fases rosadas, vaga-lumes de noite, poeira dourada na biblioteca.
- **Chão e enfeites:** blocos com rachaduras e florzinhas, e o cenário ganha capim, flores, arbustos, pedras,
  cogumelos, cercas e lampiões automaticamente.

## Organização do código (`Assets/Scripts`)
- **Nucleo/**
  - `GerenciadorDoJogo` — máquina de estados (Título → Mapa → Jogando ⇄ Pausado → Morreu → Fase concluída → Mapa ... → Vitória),
    contagem de mortes/moedas/tempo, menu do título e as marcas das mortes.
  - `Interface` — tudo o que é desenhado por cima do jogo (HUD, título, pausa, morte, vitória, falas, transição entre fases). Só lê o estado do `GerenciadorDoJogo`.
  - `SonsDaMorte` — o áudio do Retorno pela Morte (sincronizado com o renascimento) e a OST com fade.
  - `Musica` e `FabricaDeMusica` — música de fundo (arquivo ou lofi gerado por código).
  - `MapaDoMundo` — o mapa de fases desenhado em texto (terreno, caminhos, névoa, personagens, Subaru andando).
  - `Progresso` — o que fica salvo em PlayerPrefs (continuar, fase máxima, mortes por fase, fase secreta, recorde).
  - `Conquistas`, `Opcoes` (volume) e `Textos` (falas do mapa e créditos).
  - `Fases` — **as fases são desenhadas em texto**; cada caractere é um bloco (legenda no topo do arquivo).
  - `ConstrutorDeFase` — lê o texto e cria os objetos. O chão vira um único `CompositeCollider2D`.
  - `FabricaDeSprites` — toda a arte é gerada por código (pixel art escrita em texto + desenhos por fórmula),
    inclusive a fonte pixel (`TextoEmPixel`, com acentos) usada no logo e no HUD.
  - `FabricaDeSons` — efeitos sonoros 8-bit gerados por código (ondas quadradas e ruído).
  - `Animacao` (enfeites e nuvens com paralaxe), `AnimacaoDeQuadros` (bandeira tremulando, brilho dos blocos `?`),
    `Paralaxe`, `MarcaDaMorte`, `CameraSeguir`, `Controles` (Input System), `Efeitos`, `Particula`, `Sombra`.
- **Jogador/** `Jogador` — física com Rigidbody2D, pulo variável, *coyote time* e *buffer* de pulo.
- **Armadilhas/** uma classe por pegadinha: `ChaoQueCai`, `EspinhoEscondido`, `EspinhoQueCai`,
  `BlocoInvisivel`, `BlocoSurpresa`, `BlocoQueFoge`, `Mola`, `Serra`, `Esmagador`, `Cano`, `NuvemAssassina`,
  `MoedaAssassina`, `Espinho`, e `Perigo` (base: "encostou, morreu").
- **Objetos/** `Inimigo`, `Coelho`, `BaleiaBranca`, `Emilia`, `Porta`, `PontoDeSave`, `Moeda`, `Placa` (o Puck e a Beatrice), `Bandeira`, `BandeiraFujona`,
  `BandeiraFalsa`, `BandeiraVolta`.

## Fases que mudam
Recurso opcional (desligado em todas as fases): uma fase pode **mudar depois que você morre** (estilo Level Devil),
com armadilhas trocando de lugar. Para usar, adicione uma lista `mudancas` à fase em `Fases.cs`
(o formato está explicado no topo do arquivo).

## Criando uma fase nova
Copie um bloco `new Fase { ... }` em `Fases.cs` e desenhe com os caracteres da legenda
(`#` chão, `P` jogador, `G` bandeira, `C` chão que cai, `h` espinho escondido, `r` coelho, `w` Baleia Branca...).
Acrescente também o número dela no desenho do `MapaDoMundo`, ligado à anterior por um caminho de `+` (senão a fase nova não aparece no mapa).
Para testar uma fase direto: durante o Play, selecione o objeto `GerenciadorDoJogo`, mude `Fase Inicial` no
Inspector, volte ao título e escolha **Novo jogo**: o Subaru começa nesse ponto do mapa, já liberado.

## Publicar no itch.io (para jogar no navegador)
1. No Unity Hub, instale o módulo **Web Build Support** na versão do Unity do projeto (se ainda não tiver).
2. No Unity: **File → Build Profiles → Web → Switch Platform**.
3. Em **Player Settings → Publishing Settings**, deixe **Compression Format = Gzip** e marque
   **Decompression Fallback** (assim funciona em qualquer servidor, inclusive o do itch.io).
4. **Build** numa pasta vazia. Compacte o CONTEÚDO da pasta (o `index.html` tem que ficar na raiz do .zip).
5. No itch.io: **Upload new project → Kind of project: HTML**, envie o .zip e marque
   "This file will be played in the browser". Tamanho da tela: 1280 x 720, com botão de tela cheia.

O progresso (PlayerPrefs) fica salvo no navegador de cada jogador.
