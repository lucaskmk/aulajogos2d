# Onde paramos (e o que falta)

Arquivo de passagem de bastão: dá para continuar daqui (no Claude Code ou na mão).
O README explica o jogo e o código; o GDD explica o design. Aqui fica só o estado do trabalho.

## Pronto
- **13 fases + a secreta** (3 fases e um chefe, depois 2 fases e um chefe, três vezes), cada uma com um tema próprio, todas integradas
  (`Fases.cs`), com o mapa do mundo (`MapaDoMundo.cs`) e as falas (`Textos.cs`). As fases parecidas da versão de 16 viraram uma só, mais longa.
- **Os 4 chefes no molde Helltaker** (3 etapas de 2 golpes, aviso antes de todo ataque, padrões fixos, checkpoint por etapa):
  `ChefeCapita.cs` (fase 4), `ChefeCozinheira.cs` (fase 7), `BaleiaBranca.cs` (fase 10) e `ChefeKraken.cs` (fase 13, o final, que rouba moedas),
  com a base em `Chefe.cs`. O dano é **pular na cabeça no momento de vulnerabilidade** de cada um (âncora presa, clarão do lampião,
  atordoada depois do mergulho, cansada com a cabeça no convés).
- **O tsunami** (`Tsunami.cs`, a letra `N`) no lugar da Maré Negra, e os tentáculos do jogo (cano e tela de morte) vermelhos, do Kraken.
- **Recordes por fase e medalhas** (`Progresso.cs`, `Medalhas.cs`, `Interface.cs`, `MapaDoMundo.cs`) e a conquista "Perfeccionista".
- **Atalhos de teste:** F9 no mapa (libera tudo só na partida atual) e F10 (painel de gráficos, FPS e pior quadro).
- **Visual v3 (HD-2D):** blocos (`ArteDosBlocos.cs`), fundo nítido com a luz pintada, raios de sol, sombras de nuvens, vento nas plantas e a tela pixelada
  (o tilt-shift ficou desligado de fábrica; dá para ligar no F10).
- **O final com escolha e o modo difícil:** o baú no fim da fase 13 (`c`), devolver ou roubar, a tempestade nas fases e no mapa, o tsunami em toda fase e 3 finais
  (veja "O final e o modo difícil" no GDD). Testar: F8 liga o modo difícil e F7 vai direto para a escolha, os dois no mapa.

## Coop local até 4 (pronto)
- **A Tripulação** (`Tripulacao.cs`, tecla T no mapa), a câmera compartilhada, o **balão de resgate**, o pulo na cabeça, o empurrão,
  o balão de propósito e os títulos do fim da fase (`JogadorCoop.cs`, `GerenciadorCoop.cs`, `InterfaceCoop.cs`). As regras estão no README ("Coop local").
- **Por jogador:** bandeira de morto (da cor dele, fincada na hora), contagem de mortes ao lado do rostinho e os itens da Loja
  (escudos, elixir e duplo salto; TAB na Loja escolhe para quem é a compra). Moedas e poções continuam da tripulação.
- **Chefes:** a arena puxa para dentro quem ficou de fora; a escuridão tem um círculo de luz por Grumete (`EscuridaoDaTripulacao.cs`);
  a Cozinheira corta também em cima dos degraus e mira um jogador de cada vez. Fase 7 com um save a mais e a cozinheira do leque mais alta.
- As armadilhas e os chefes agora olham **todos** os Grumetes vivos (`GerenciadorDoJogo.ProximoVivo` e `JogadorMaisPerto`), não só o líder.
- **Teste automático:** menu **Cilada (teste) > Testar coop** (ou `-executeMethod TesteDoCoop.RodarEFechar` na linha de comando).
- **Falta jogar com gente de verdade:** as 13 fases e os 4 chefes com 2 a 4 jogadores. O que olhar:
  - se o líder da câmera troca na hora certa (principalmente nas fases que voltam para a esquerda e no Navio Fantasma);
  - se os chefes continuam difíceis com 4 (hoje a vida deles é a mesma: talvez dar mais golpes por etapa no coop);
  - se o pulo na cabeça não pula trechos inteiros de fase (é 115% do pulo e não tem limite: ajustar `forcaDoPuloNaCabeca` no `Jogador`);
  - a Capitã, a Baleia e o Kraken ainda miram só no líder (a Cozinheira já alterna): ver se precisa alternar neles também;
  - se os recordes de tempo no coop devem ir para um ranking separado (hoje vão para o mesmo).

### Mais mecânicas de coop (ideias, da mais barata para a mais cara)
- **Carregar e arremessar:** segurar o empurrão perto do amigo pega ele no colo; soltar joga em arco (atravessa buracos, alcança moedas altas).
- **Baú de dois:** um baú pesado que só abre com 2 Grumetes em cima da tampa ao mesmo tempo (moedas extras só no coop).
- **Alavanca e portão:** um segura a alavanca, o outro passa. A cilada: o portão fecha se o da alavanca rir (soltar).
- **Moeda fantasma:** quem está no balão pega moedas atravessando paredes (dá o que fazer para quem morreu).
- **Balão que flutua para cima:** deixar o balão levar um amigo pendurado por 2 s (um "elevador" de emergência).
- **Modo traíra (versus):** no fim da fase, quem pegou mais moedas ganha a coroa e os outros perdem 1 moeda por empurrão levado.
- **Chefes no coop:** um ataque a mais por rodada para cada jogador extra, e o pisão só vale se dois pularem em sequência.
- **Online:** a Steam tem o **Remote Play Together** (um compra, os amigos entram pela internet no coop local, sem código de rede).

## Leva do tema pirata (pronta, falta testar na Unity)
O tema de Re:Zero saiu inteiro (por causa dos direitos autorais). O jogo agora é **CILADA À VISTA!**, uma aventura de pirata:
- **Nomes e textos:** o Grumete, a Marina (sereia), o Louro (papagaio), a Dóris (fantasminha), a Capitã Âncora, a Cozinheira,
  o Kraken e a Baleia Branca; fases, falas, conquistas, mensagens de morte, créditos, README e GDD.
- **Desenhos novos:** os personagens, a âncora, os cutelos, os caranguejos e ratos e o tentáculo.
- **Áudio 100% próprio:** os dois MP3 do anime foram apagados. A Volta da Maré agora é gerada por código (`SomDaMare.cs`: a onda, o sino do navio e um lamento de acordeão).
- **Trilha pirata** (`Musicas.cs`, sintetizador próprio com acordeão, apito, violão, tambor de mão, pisadas e sino):
  - "Jiga do Grumete" nas fases (6/8, ré menor, AABB), num tom por fase (`FabricaDeMusica.DaFase`);
  - "Balada do Arquipélago" no mapa;
  - as músicas dos 3 chefes reorquestradas com os instrumentos de pirata.
  - A `FabricaDeEpico.cs` (a música antiga, "abertura de anime") foi apagada.
- **Mapa do arquipélago:** as ilhas separadas por estreitos (as pontes de madeira atravessam), o mar no norte e no sul com navios,
  palmeiras, o farol, o forte da Capitã, o navio fantasma, naufrágios, baús e a Rocha da Caveira.
  Os caminhos são os mesmos de antes (só ganharam pontes onde passam por cima do mar), então a ordem das fases e os atalhos não mudaram.
- **Fundos das fases:** ilhas com um vulcão lá longe, um galeão no horizonte, palmeiras e o mato da praia (as camadas de `Paralaxe.cs`).
  No chão das fases, barris e estrelas-do-mar no lugar das cercas e dos cogumelos.
- **Capitã Âncora:** mais difícil (avisos mais curtos, tudo mais rápido, mais ataques por rodada) e com a **tempestade de âncoras**
  (âncoras rápidas para todos os lados, que voltam pelo mesmo caminho). A janela da âncora presa continua mais longa que a original
  (3 / 2,7 / 2,4 s). As outras dificuldades dos chefes voltaram a ser as de antes.
- **Gráficos:** fundos nítidos e com detalhe (sem o desfoque), caixas de fala novas (fonte pixel, retrato, moldura, no alto da tela),
  os personagens redesenhados com três tons por material (`ArteDosPersonagens.cs`), a Capitã com rosto novo e as chefes com sombreamento,
  e a névoa do mapa mais macia.

### Para conferir na Unity
- O Console ao dar Play: `[Cilada à Vista] visual v3 (HD-2D escuro) rodando...` (sem erro vermelho).
- **Capitã:** a tempestade de âncoras (as linhas piscam antes; as âncoras cravam e voltam), se a luta ficou difícil na medida,
  a janela da âncora presa e se o pisão fora da janela não dá dano.
- **Cozinheira e Baleia:** se a luta ficou igual à de antes (dificuldade e janela de dano).
- **Música:** a jiga em cada fase (o tom muda de fase para fase), a balada no mapa, a de cada chefe na luta (continua depois de morrer e volta para a da fase depois da vitória).
  E o engasgo ao gerar a música (meio segundo), principalmente no navegador.
- **Morte:** a onda e o sino da Volta da Maré caem bem no renascimento.
- **Mapa:** os navios no mar, a luz do farol, as palmeiras ao vento, o horizonte (mar com galeões e ilhas) e a névoa abrindo pelas pontes novas.
- **Fundos das fases:** o galeão e as palmeiras no HD-2D e no Simples; se nada pontudo no fundo parece espinho.

## Decisões para a Steam (10/11/2026)
- **Online:** vai ser pelo **Remote Play Together** (o coop local serve do jeito que está; os convidados entram como controles a mais).
  Não tem código de rede. Só dá para testar com o jogo já na Steam.
- **Roleta:** fica. É aposta simulada: na Austrália isso dá classificação 18+ e o PEGI tem regra parecida. Responder com isso em mente
  no questionário de conteúdo da Steam. Ao lado dela entraram os **jogos de habilidade** (Pontaria e Reflexo, com fichas, sem aposta).
- **Duração:** em vez de fases novas, o que já existe ficou à vista (Modo Tempestade no menu depois de zerar, contador de medalhas
  no título e no mapa e o "E agora?" na vitória).
- **Nome do produto:** agora é `Cilada à Vista!` (a empresa ainda é `DefaultCompany`: trocar em Project Settings > Player).

## Publicar na Steam (o que falta)
1. **Conta no Steamworks** (partner.steamgames.com): cadastro, dados fiscais e bancários e a taxa de US$ 100 por jogo (Steam Direct;
   não é devolvida, mas é descontada dos repasses depois de US$ 1.000 em vendas). A Valve leva alguns dias para aprovar a conta.
   Prazos: a página precisa ficar **pelo menos 2 semanas como "Em breve"** antes do lançamento, e a Valve pede cerca de 30 dias entre
   pagar a taxa e lançar (o painel do Steamworks mostra a data mais cedo possível).
2. **Nome da empresa** no Unity (Project Settings > Player): ainda é `DefaultCompany` (o do produto já é `Cilada à Vista!`).
   Trocar ANTES de lançar (muda a pasta do save: quem já jogou perde o save local).
3. **Build de Windows** (File > Build Profiles > Windows, x86_64). O WebGL do itch não serve para a Steam.
   Testar o build em tela cheia, em 16:9 e 16:10, com controle de Xbox e de PlayStation.
4. **Steamworks SDK** (opcional, mas vale): o pacote Steamworks.NET para as **conquistas** (as 31 de `Conquistas.cs` viram conquistas da Steam)
   e o Steam Cloud (o save está em PlayerPrefs, no registro do Windows: para a nuvem, passar o save para um arquivo em `Application.persistentDataPath`).
5. **Página da loja** (tamanhos conferidos na documentação do Steamworks em 10/11/2026):
   - loja: cápsula de cabeçalho 920x430, pequena 462x174, principal 1232x706, vertical 748x896 e **5 ou mais capturas** em 1920x1080 (16:9);
   - biblioteca: cápsula 600x900, cabeçalho 920x430, herói 3840x1240 e o logo (1280 de largura ou 720 de altura, fundo transparente);
   - um trailer, descrição curta e longa em PT e EN, gênero e etiquetas (Plataforma, Difícil, Coop local, Para 4 jogadores, Pixel).
   Marcar **"Remote Play Together"**, "Suporte total a controle" e os idiomas (Português do Brasil e Inglês).
6. **Classificação e questionário de conteúdo** da Steam, e o preço (ou gratuito).
7. **Enviar o build** com o SteamPipe (`steamcmd`), marcar como padrão e pedir a **revisão da página e do build**
   (a da página leva de 3 a 5 dias úteis; mandar com pelo menos uma semana de folga).
8. **Direitos:** tudo no jogo é próprio (arte por código, músicas sintetizadas). Conferir as imagens pintadas em `Assets/Resources/Arte`
   (se foram geradas por IA, a Steam pede para declarar no questionário) e pôr os nomes da equipe nos créditos.

## Ainda falta
- **Página do itch.io:** como jogar e os segredos (fase secreta: todas as moedas da fase 3 e depois seta para baixo no ponto 3).
- **Testar as fases novas jogando:** o checador confere o terreno, mas não as plataformas, o vento, as portas e o tsunami. Jogar os trechos novos
  (fases 4, 6, 7, 9, 10, 11 e 12) e as emendas (2, 3, 5 e 8), e a luta do Kraken inteira, e ajustar se algum ficar injusto.
- **Velocidade da onda no modo difícil:** os números de `velocidadeDaOnda` em `Fases.cs` são contas, não teste. Jogar as 13 fases com o F8 ligado e ajustar,
  principalmente as fases 6, 8 e 9, que obrigam a esperar.
- **Tempos-alvo das medalhas:** são estimativas. Ajustar o campo `tempoAlvo` em `Fases.cs` depois de jogar,
  e conferir se TODAS as moedas de cada fase dá para pegar (senão a medalha de moeda fica impossível).
- **Quadros pintados dos finais:** os quadros novos (`escolha_*`, `roubou_*`, `tempestade_*`, `verdadeiro_1`, `rei_*`) estão desenhados por código, como `final_1` e `final_2`.
- **Modo Justo (opcional, desligado por padrão):** o save nunca "muda de lugar" e a bandeira `W` não manda para o começo.
- **Sons das mecânicas:** efeitos para a âncora, o vento, as facas, o tsunami, o tic do ritmo e as plataformas.
- **Equipe:** pôr os nomes em `Textos.Equipe` (créditos), no README e no GDD.

## Como integrar uma mecânica ou fase nova
Cada mecânica nova é **um arquivo novo** em `Assets/Scripts/Armadilhas/` (com `.meta`). Os sprites são registrados com
`FabricaDeSprites.Registrar` num método `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`. Integrar uma fase nova é:
1. Pôr um `case` no `switch` de `ConstrutorDeFase.Construir`.
2. Acrescentar a fase no lugar certo de `Fases.Todas`, com `corDoFundo` e, se for o caso, `noite`/`escura` e `velocidadeDaOnda` (se ela obrigar a esperar). Pôr também a linha da legenda no topo de `Fases.cs`.
3. Pôr o ponto dela no `MapaDoMundo` e a fala do mapa em `Textos.DepoisDaFase`, pelo nome da fase.
4. Se tiver conquista nova, pôr em `Conquistas.Todas`.
5. Testar o terreno:
   ```
   python3 Ferramentas/extrair_fases.py Assets/Scripts/Nucleo/Fases.cs /tmp/fases
   python3 Ferramentas/checar_fase.py /tmp/fases/fase_XX.json --de s
   ```
6. Para mexer numa música: editar `Musicas.cs` e ouvir com `dotnet run --project Ferramentas/ouvir_musicas -- Builds/musicas`.
