# CILADA À VISTA! — GDD (Design, Dinâmica, Experiência)

> Documento de design organizado pelo **DDE** (*Design, Dynamics, Experience*; Walk, Görlich e Barrett).
> Cada seção curta pode virar um cartão no Milanote.
> - **Design** é o que a equipe controla.
> - **Dinâmica** é o que acontece quando alguém joga.
> - **Experiência** é o que o jogador sente.
>
> Os números deste documento vêm do código (`Assets/Scripts`). O README tem os controles, os créditos e como rodar o jogo.

---

# 0. VISÃO GERAL

## High concept
"*Cat Mario* num mar de piratas": um plataforma 2D em que **tudo mente**. Cada morte é uma *Volta da Maré*:
a onda traz o Grumete de volta, ele lembra onde errou e passa, de ilha em ilha, até reencontrar a Marina.

## Ficha
- **Gênero:** plataforma 2D "troll" / *masocore* leve, com lutas de chefe no estilo *Helltaker*.
- **Público:** fãs de jogos de "raiva" (*Level Devil*, *Cat Mario*, *I Wanna Be the Guy*) e de aventuras de pirata, de 12 anos para cima. Também quem só quer rir das próprias mortes.
- **Plataforma:** navegador (WebGL no itch.io), com teclado ou controle. Também roda no editor e em build de PC.
- **Duração:** 13 fases (com 4 chefes) + 1 secreta, de 45 a 90 minutos na primeira vez (estimativa: depende muito das mortes).
- **Engine:** Unity 6 (URP 2D), C#. Toda a arte e todo o áudio são procedurais.

## Referências
- **Cat Mario / Syobon Action** e **Level Devil**: as armadilhas que quebram a expectativa.
- **Helltaker**: o chefe com etapas, checkpoints, padrões fixos e ataques telegrafados.
- **Super Mario World**: o mapa do mundo com caminhos e pontos.
- **Octopath Traveler** (HD-2D) e **Stardew Valley** com mods: pixel art com luz, profundidade, raios de sol e sombras de nuvem.
- **Histórias de pirata** (mapas do tesouro, navios fantasmas, o Kraken das lendas nórdicas, aqui uma lula gigante vermelha) e **Moby Dick**,
  de Herman Melville (a Baleia Branca): o tema e o tom.

## Pilares
1. **Passar raiva com graça.** A morte é a piada. O jogo ri junto (mensagens de morte, o Louro mentiroso, a bandeira que foge), mas nunca humilha.
2. **Aprender morrendo (Volta da Maré).** Morrer é rápido, barato e informativo. Você volta na hora, com um X vermelho ("X marca o lugar!") mostrando onde morreu.
3. **Sempre justo depois que você sabe.** Toda armadilha pega uma vez. Na segunda tentativa existe uma resposta clara (pular, esperar, não pegar, ir por outro lado).
   Toda fase é passável (o checador de fases confere o terreno), e todo ataque de chefe avisa antes.

---

# 1. DESIGN (o que a equipe controla)

## 1.1 Blueprint: mundo e narrativa

### Mundo
Um **arquipélago pirata**, visto como um mapa do mundo (112 x 21 células) que começa coberto pela **névoa do alto-mar** e vai se revelando.
As ilhas são ligadas por pontes de madeira que atravessam os estreitos, e há navios navegando em volta:
1. a **Ilha do Porto**, com o castelo e o farol;
2. a **Ilha da Selva**, cheia de palmeiras;
3. a **Ilha da Lagoa**, com a **ilhota secreta**, o **forte da Capitã** e a vila;
4. a **Ilha do Desfiladeiro** (o abismo), com a mesa de pedra no meio e a gruta da Cozinheira na beirada;
5. os rochedos com o **navio fantasma** encalhado (as cabines da Dóris) e o penhasco dos ventos, com os baús de tesouro;
6. as **ilhas mortas do Kraken**, de palmeiras secas (o mar da Baleia, o tsunami e o rochedo do pôr do sol);
7. o mar escuro, com a **Rocha da Caveira** e o **covil do Kraken Vermelho**.

### Personagens
| Personagem | Papel no jogo |
|---|---|
| **Grumete** | O jogador. O marujo mais novo do navio, de bandana vermelha e camisa listrada. Morre e volta com a maré. |
| **Louro** | O papagaio das "placas" de dica. Às vezes ajuda, às vezes mente. Comenta no mapa depois das fases. |
| **Marina** | A sereia: o objetivo final. O Kraken prendeu ela no covil; ela espera no fim da fase 13 e comemora. Também comenta no mapa. |
| **Capitã Âncora** | Chefe da fase 4, com a âncora presa na corrente. Fica **furiosa** na etapa 3. |
| **Cozinheira** | A ex-cozinheira do navio fantasma (foi expulsa). Joga cutelos das sombras e é a chefe da fase 7, na gruta escura. |
| **Dóris** | A fantasminha do Navio Fantasma (fase 8), com as portas embaralhadas. |
| **Baleia Branca** | Chefe da fase 10: uma orca gigante com chifre, mandada pelo Kraken. Também aparece nadando no céu do mapa. |
| **Kraken Vermelho** | O **chefe final** (fase 13): a lula gigante vermelha por trás de tudo. Os tentáculos vermelhos dos canos e da tela de morte são dela, e foi ela que levantou o **tsunami** da fase 11. Os ataques dela **roubam moedas**. |
| Caranguejos e Ratos do Porão | Inimigos comuns. Os ratos se multiplicam. |

### Narrativa e tom
- O Grumete caiu do navio num arquipélago onde tudo quer afundá-lo, e o único "poder" dele é a maré, que sempre o traz de volta.
- A história é contada:
  - pelas placas do Louro dentro das fases;
  - pelas falas no mapa depois de cada fase (o Louro e a Marina, com uma fala diferente se você passou sem morrer);
  - pelas mensagens de morte;
  - pelo final com a Marina e os créditos.
- **Tom:** comédia de "trollagem" em cima de uma aventura de pirata. As mensagens de morte misturam meme ("Skill issue.", "KKKKKKKKKKK") com o mar ("O Kraken mandou lembranças.", "Volta da Maré, patrocinada pelo Kraken.").

## 1.2 Mecânicas

### Movimento e física (Jogador.cs)
| Parâmetro | Valor | Efeito |
|---|---|---|
| Velocidade | 7 blocos/s | — |
| Aceleração | 80 blocos/s² | Chega à velocidade máxima em cerca de 0,09 s: resposta quase instantânea. |
| Força do pulo | 14 blocos/s | Pulo máximo de cerca de **2,85 blocos** de altura e cerca de 5,7 blocos de distância. |
| Gravidade | 3,5 × 9,81 | Cerca de 34 blocos/s². |
| Queda máxima | 20 blocos/s | — |
| Pulo variável | Soltar o botão na subida corta a velocidade pela metade | — |
| Coyote time | 0,10 s | Ainda dá para pular depois de sair da beirada. |
| Buffer de pulo | 0,12 s | Apertar pular um pouco antes de tocar o chão também vale. |
| Colisor | 0,62 × 0,86, com bordas arredondadas, sem atrito | Não "gruda" em paredes. |
| Mola | Força 22 | Cerca de 7 blocos de altura. |

- **Game feel:** estica no pulo, achata na aterrissagem (mais forte quanto maior a queda), poeira no pulo e na queda, "respiração" parado e sombra oval no chão.

### Armadilhas clássicas (uma linha cada)
- **Chão que cai (`C`):** parece chão e despenca quando você pisa.
- **Chão falso (`F`):** você atravessa.
- **Bloco invisível (`I`):** aparece quando você bate a cabeça. Normalmente no meio de um pulo sobre um buraco.
- **Espinho escondido (`h`):** pula do chão quando você chega perto.
- **Espinho que cai (`v`):** despenca do teto quando você passa embaixo.
- **Espinho (`^`):** parado e visível. "O mais honesto do jogo."
- **Bloco `?` / bloco surpresa (`K`):** iguais. Um dá moeda e o outro solta um inimigo em cima de você.
- **Moeda assassina (`m`):** igual à moeda. Pegou, virou espinho.
- **Nuvem assassina (`o`):** igual às nuvens do cenário, mas tem dentes.
- **Esmagador (`T`):** despenca quando você chega perto. Se você já passou, ele se arrasta atrás de você.
- **Serra (`<` / `>`):** uma vem por trás e persegue, a outra vem pela frente. Depois de passar, volta uma vez.
- **Cano (`U`):** de tempos em tempos sai um tentáculo (vermelho) do Kraken. Dá para pular por cima no tempo certo.
- **Mola (`S`):** lança para o alto, às vezes direto num espinho.
- **Bloco tímido (`M`):** foge quando você pula perto. O truque é assustá-lo antes com um pulinho.
- **Bandeira fujona (`R`):** foge para outro lugar quando você chega perto.
- **Bandeira falsa (`Z`):** fica vermelha, brotam espinhos e você morre.
- **Bandeira volta (`W`):** manda você de volta para o último checkpoint (ou para o começo, se não pegou nenhum). Não conta como morte, "só como raiva".
- **Inimigos:**
  - **Caranguejo (`E`):** pise para derrotar. Se ele passar de você, volta.
  - **Caranguejo espinhoso (`e`):** não pise.
  - **Rato do Porão (`r`):** persegue você e **se multiplica**.

### Mecânicas temáticas (uma por fase nova)
- **Âncora da Capitã (`Q` / `q`):** a âncora gira na corrente e só a **âncora** mata. Em cima do eixo é seguro ("olho do furacão").
  O `q` fica bravo e inverte o giro, 2x mais rápido.
- **Controles invertidos (`X`):** cruzar a linha avisa "Invertendo os controles em..." e inverte depois de **0,4 s** ("bombordo é estibordo"). Voltar antes cancela.
  Só a fase 5 usa isso, e só num trecho.
- **Plataformas mágicas (`j` / `J` / `D`):** vão e voltam até 6 blocos.
  - `J`: o cristal fica vermelho e ela despenca depois de 0,5 s.
  - `D`: o cristal fica amarelo, ela **dispara** e freia de repente. Quem pula enquanto ela treme cai.
- **Vento do mar (`y`):** rajadas para a esquerda (calmo 1 s, aviso 0,5 s com folhas, rajada 2 s). Segure para a frente ou se esconda atrás de um bloco.
- **Redemoinho (`n`):** levanta o Grumete até o topo da zona. Os "traiçoeiros" engasgam e desligam por 1,5 s.
- **Portas das cabines do Navio Fantasma (`0`–`9`):** pares fixos de ida e volta (`1-4`, `3-5`, `2-9`, `6-7`, `8-0`). Uma leva a um beco cheio de caranguejos.
- **Blocos do ritmo (`a` / `b` / `A`):** o azul (da maré) e o rosa (do coral) se revezam a cada **1,4 s**, piscando e fazendo "tic, tic, TAC" antes da troca.
  O `A` pisca junto com o azul, mas nunca fica sólido.
- **Esmagador do ritmo (`t`):** cai toda vez que o azul acende. "Rosa aceso = caminho livre."
- **Facas da Cozinheira (`l` / `k`):** dois olhos nas sombras. A menos de 14 blocos e na mesma altura, ela joga um cutelo a cada 1,6 s (olhos vermelhos 0,4 s antes).
  O `k` joga três em leque. A faca crava em blocos, então esconder funciona.
- **Tsunami (`N`):** a onda gigante que o Kraken levantou persegue você a **5,2 blocos/s** (o Grumete anda a 7). Parar para pensar custa caro.
  Quando você fica perto, um jato d'água sai da onda (avisa recuando e piscando). Ela tem espuma, a crista enrolando lá em cima e destroços boiando.
- **Kraken Vermelho (`g`):** a arena do chefe final, com a poça dela no fim (4 colunas sem chão).

### Checkpoints que "mudam de lugar"
- O **cristal de save (`s`)** marca onde você renasce.
- Em **25%** das mortes o save "muda de lugar" ("O ponto de save mudou de lugar... :)") e você volta para o **checkpoint anterior**
  (ou para o começo, se era o primeiro). Antes voltava sempre para o começo, mas com as fases mais longas isso virou maldade demais.
- A **bandeira que volta (`W`)** também manda para o último checkpoint (sem checkpoint, para o começo).
- Isso nunca acontece em fase com chefe.

### Chefes (molde Helltaker)
- 3 etapas × 2 golpes (vida 6). Tudo mata com um toque.
- Todo ataque tem aviso: 0,6 s na etapa 1, 0,48–0,5 s na etapa 2 e 0,38–0,4 s na etapa 3, mas nunca menos de 0,55 s para o que pede pulo.
- Os padrões são fixos. Morreu, volta na mesma etapa, sem a introdução.
- O único jeito de dar dano é **pular na cabeça** do chefe no momento de vulnerabilidade:
  - **Capitã Âncora:** a âncora fica presa no chão e ela fica parada puxando a corrente. Essa janela é a mais longa dos chefes
    (3 s, 2,7 s e 2,4 s por etapa), porque a Capitã é o primeiro chefe e o que ensina a regra;
  - **Cozinheira:** o clarão do lampião (ela cai tonta, à vista);
  - **Baleia:** fica atordoada depois do mergulho;
  - **Kraken:** cansa no fim de cada rodada e deita a cabeça no convés (3 s, 2,6 s e 2,2 s). Os ataques dela: chicotada (baixa = pule,
    alta = não pule), pancada do céu (a marca segue você), **agarrão amarelo** (não mata, rouba moedas e te joga para trás) e tinta (etapa 2 em diante).
- **O roubo do Kraken:** cada agarrão leva até 5 / 7 / 10 moedas (primeiro as da tentativa, depois as guardadas, nunca negativo).
  É salvo na hora. A cada pisão ela devolve metade e, derrotada, devolve o resto. Morrer não devolve, e o escudo não protege do roubo.
  A ideia: o chefe final mexe com a economia do jogo inteiro (a Loja), e acertar a cabeça dela vira também "recuperar o tesouro".
- **A tempestade de âncoras da Capitã:** ela salta para o meio da arena e dispara várias âncoras rápidas (19 a 23 blocos/s) para todos os lados.
  Elas cravam na parede, no chão ou nas vigas do teto e voltam pelo mesmo caminho (a volta também mata; a corrente não).
  As linhas mostram cada caminho antes: a linha baixa pede pulo (na ida e na volta) e a que bate no chão pede distância.
  Cada rajada muda o lugar seguro, e na etapa 3 vêm duas seguidas: o ataque obriga o jogador a ler e se mexer, em vez de ficar num canto.

### Moedas
- Moedas `$` e blocos `?` somam no HUD e no save.
- Muitas são **isca** para uma armadilha.
- Pegar **todas** as da fase 3 (a fase inteira, numa mesma tentativa, sem pegar a moeda assassina) libera a fase secreta.

### Fase secreta
- A **Ilha do Louro** fica numa ilhota da lagoa.
- Para chegar lá: no ponto 3 do mapa, aperte ↓.
- "Sem pegadinha"... só uma.

### Conquistas
- São 27, salvas para sempre. Alguns exemplos:
  - A maré te trouxe, Grumete imortal (100 mortes), Confiou na bandeira, Ganância, Olhou para o céu, Suspeito de hack (passar de fase sem morrer);
  - Pescador de caranguejos, Controle de pragas, Buuu!, Mudaram meu save!, Explorador, Passageiro da Expressa, Caldo do Tsunami;
  - do Kraken: Assaltado (ter moedas roubadas), Calamar à doré (vencer) e Bolso fechado (vencer sem ela roubar nenhuma moeda);
  - as vitórias contra os chefes, Fora do Ritmo, Perfeccionista (as 3 medalhas de uma fase) e Terra à vista! (zerar o jogo).

### Recorde
- O recorde é zerar com o menor número de mortes, jogando em ordem.
- No fim, o número de mortes dá um título ao jogador, de "Suspeito de hack" até "Grumete imortal".

### Loja do Porto
- Abre no mapa depois da fase 2 (o Louro apresenta numa cutscene e dá o primeiro escudo).
- Vende o escudo, o elixir de super salto e o duplo salto, e tem a roleta no meio (veja 2.5).
- A roleta sorteia o resultado e salva o saldo no começo do giro: fechar o jogo no meio não muda nada.
- Os itens ficam na partida salva; um Novo jogo zera.

### Recordes por fase e medalhas (rejogabilidade)
- Cada fase guarda o **melhor tempo**, o **menor número de mortes** e o **maior número de moedas** (cada um por si).
  Valem para sempre: um Novo jogo não apaga.
- O tempo conta desde que você entra na fase pelo mapa, com o tempo das mortes. As moedas são as da tentativa em que você passou.
- Cada recorde dá uma **medalha**:
  - **relógio:** passar dentro do tempo da meta (estimado pelo tamanho da fase; os chefes e o Navio Fantasma têm a meta deles);
  - **coração:** passar sem morrer;
  - **moeda:** pegar todas as moedas numa tentativa só (fase sem moeda: vem com a primeira vitória).
- As medalhas aparecem do lado de cada fase no **mapa** (apagadas = faltam), com a linha de recorde embaixo,
  e na tela de "fase concluída", com **"NOVO RECORDE!"** no que você melhorou.

## 1.3 Interface
- **HUD:**
  - nome e número da fase;
  - mortes;
  - moedas;
  - tempo;
  - barra de progresso até a bandeira mais distante (para não entregar qual é a verdadeira);
  - barra de vida do chefe, dividida em etapas;
  - avisos no meio da tela ("CONTROLES INVERTIDOS :)").
- **Título:**
  - o logo "CILADA À VISTA!" em dourado, na fonte pixel, com o mapa escurecido atrás;
  - opções: Continuar, Novo jogo, Opções, Como Jogar, Conquistas (x/24), Créditos e Sair;
  - o recorde.
- **Mapa do mundo:**
  - o Grumete anda entre os pontos;
  - pontos verdes (já passou) e amarelos (liberados);
  - caveira com as mortes de cada fase e as 3 medalhas;
  - a névoa some pelo caminho novo;
  - falas dos personagens depois de cada fase.
- **Caixas de fala:** quando o Grumete chega perto do Louro (ou da Dóris, nas Cabines), aparece no alto da tela uma caixa com moldura de
  madeira, o retrato de quem fala, o nome numa etiqueta e o texto na fonte pixel, quebrado para caber (nunca tampa o Grumete nem o chão).
  No mapa, a fala depois de cada fase fica na faixa de baixo, sem cobrir as ilhas.
- **Pausa (em lista):** Continuar, Reiniciar fase, Opções (volume da música, volume dos efeitos, gráficos HD-2D ou Simples), Como Jogar, Voltar ao mapa e Voltar ao título.
- **Morte:**
  - a tela perde a cor e os tentáculos do Kraken avançam das bordas;
  - aparece uma mensagem engraçada;
  - dá para pular a animação depois de 0,5 s.
- **Vitória e créditos:** a Marina comemora, aparecem o resumo (mortes, moedas, tempo e título) e os créditos rolando.

---

# 2. DINÂMICA (o que acontece quando se joga)

## 2.1 Loops
- **Micro (segundos):** ver → tentar → morrer → **voltar na hora** (1,3 s, ou 0,7 s a partir da 3ª morte, e pode pular) → tentar de outro jeito.
- **Médio (minutos):** atravessar a fase de save em save, decorando as armadilhas. O X vermelho mostra onde você já morreu.
- **Macro (sessão):** passar a fase → o Grumete anda no mapa → a névoa se abre → uma fala → a próxima fase. A cada 2 ou 3 fases vem um chefe.
- **Meta:** conquistas, a fase secreta, as medalhas de cada fase (tempo, sem morrer e todas as moedas) e o recorde de mortes.

## 2.2 Curva de dificuldade
- **Fase 1:** o tutorial curto (82 blocos). Daqui em diante, toda fase é longa (112 a 218 blocos) e tem checkpoints.
- **Fases 2 e 3:** ensinam a "regra do jogo" (nada é confiável), duas trollagens clássicas por fase.
- **Fase 4:** o primeiro chefe, o mais "didático" (o Louro dá dicas na etapa 1, e a janela de dano é a mais longa).
- **Fases 5 e 6:** controles invertidos, blocos tímidos e plataformas sobre o abismo.
- **Fase 7:** o segundo chefe, que junta escuridão e leitura de aviso.
- **Fases 8 e 9:** a mais longa (portas e ritmo no Navio Fantasma) e o vento.
- **Fase 10:** a Baleia Branca, rápida e com muitos ataques.
- **Fase 11:** pressão contínua (o tsunami persegue). **Fase 12:** "prova final" que revisita as trollagens.
- **Fase 13:** o chefe final, que além de tudo mexe nas suas moedas.
- A **secreta** é um respiro.

## 2.3 Como as trollagens subvertem a expectativa
- **Convenção de plataforma virando armadilha:**
  - bloco `?` → inimigo;
  - moeda → espinho;
  - bandeira → morte, fuga ou volta ao começo;
  - nuvem → dentes.
- **O Louro mente**, mas nem sempre: "Pula com força! (ou não)". O jogador aprende a ler o tom.
- **"Igualzinho ao anterior"**: a segunda plataforma, o segundo redemoinho e a segunda âncora são iguais na aparência e diferentes no comportamento.
- **Nomes que mentem:** "O Final (mudei de ideia)" é a fase 3, e o "final" continua depois da primeira metade.
- **O save que muda de lugar:** a "promessa" de checkpoint também pode trair (só fora dos chefes).

## 2.4 Ritmo dos chefes
- **Etapa 1:** um ataque por vez, para aprender.
- **Etapa 2:** mais rápido e com combinações (dois ataques juntos, a volta do arremesso em outra altura, tempestades com mais âncoras).
- **Etapa 3:** tudo mais rápido, mais o "twist":
  - a Capitã fica furiosa;
  - a escuridão da Cozinheira fecha;
  - o chão da Baleia desmorona e a arena escurece;
  - o Kraken fica furioso e os ataques vêm juntos.
- Cada rodada termina na **janela de dano** (pular na cabeça). Erro significa recomeçar a rodada, não a luta.

## 2.5 Economia (moedas e recordes)
- As moedas são **isca** (puxam o jogador para o perigo), **desafio opcional** (a secreta e a medalha de moedas de cada fase)
  e o dinheiro da **Loja do Porto** (no mapa, depois da fase 2; o botão fica no canto de baixo à direita, ou a tecla L):
  - **escudo** (15 moedas, até 3): te salva de uma morte;
  - **elixir de super salto** (20, até 3): entrar numa fase gasta um, e o pulo fica bem mais alto a fase inteira;
  - **duplo salto** (45, uma vez): um segundo pulo no ar, para a partida toda;
  - **roleta**: aposte de 1 a 100 moedas numa cor. São 15 casas (7 vermelhas, 7 pretas e 1 verde): vermelho e preto pagam 2x,
    o verde paga 14x. A casa leva uma pequena vantagem, como numa roleta de verdade: é um jeito de arriscar, não de enriquecer.
- As mortes são a "moeda" que importa: aparecem no HUD, nas caveiras do mapa, no recorde, na medalha do coração e no título final.
- O tempo vira desafio na volta: depois de decorar as armadilhas, a medalha do relógio puxa o jogador para refazer a fase rápido.

## 2.6 Comportamento emergente
- **Bandos de ratos** que se multiplicam e lotam a tela se o jogador demora.
- **Esmagadores e serras** que perseguem e às vezes matam inimigos ou bloqueiam o caminho de um jeito diferente a cada tentativa.
- **Speedrun natural:**
  - correr sem parar contra o tsunami;
  - pular a animação da morte;
  - ir de save em save.
- **Jogadores testando o Louro:** depois de algumas mortes, o jogador passa a fazer o contrário da placa (e o jogo às vezes recompensa ser honesto: "dessa vez é sério").

---

# 3. EXPERIÊNCIA (o que o jogador sente)

## 3.1 Emoções-alvo
| Momento | Emoção |
|---|---|
| Primeira trollagem de cada fase | **Surpresa** ("COMO ASSIM?") |
| Morrer de novo para a mesma coisa | **Raiva engraçada**: a mensagem de morte ri junto |
| Passar do trecho que matou 10 vezes | **Alívio** |
| Fazer a fase inteira "de cor" | **Domínio** |
| Chefes | **Tensão → foco → triunfo** |
| Mapa entre as fases | **Descanso e curiosidade** (a névoa esconde a próxima ilha) |
| Final com a Marina | **Recompensa e carinho** |

## 3.2 Emoção por fase (alvo)
1. **Bem-vindo :)** — surpresa ("até o tutorial mente").
2. **Confia em mim (e corre!)** — desconfiança que vira urgência.
3. **O Final (mudei de ideia)** — ironia e ganância punida.
4. **A Capitã Âncora** — primeiro "boss rush" de adrenalina e aprendizado.
5. **Cadê a direita? (Foge, bloco!)** — confusão controlada e frustração cômica.
6. **Ponte para o Porto** — vertigem e paranoia.
7. **A Gruta da Cozinheira** — medo do escuro.
8. **O Navio Fantasma** — quebra-cabeça e memória, depois fluxo e ritmo.
9. **Vento em Popa!** — luta contra a força invisível.
10. **A Baleia Branca** — clímax épico (e a revelação: foi o Kraken).
11. **O Tsunami (CORRE!)** — pânico.
12. **Verdadeiro Final** — "eu já sei de tudo" (domínio).
13. **O Kraken Vermelho** — o confronto final, com o tesouro em jogo.
14. **Secreta** — recompensa.

## 3.3 Estética
- **Visual HD-2D de pixel art:**
  - **blocos e personagens:** blocos com rampas de cor e *hue shifting*, personagens chibi;
  - **profundidade:** fundo em camadas com paralaxe e desfoque de profundidade (as ilhas e o vulcão lá longe, um galeão no horizonte,
    palmeiras e o mato da praia), tudo nítido: a distância vem da cor (as camadas de longe ficam mais claras e azuladas);
  - **luz:** raios de sol que somem quando uma nuvem passa, sombras de nuvens, hora do dia (manhã no início, entardecer dourado no fim) e luzes 2D;
  - **vida no cenário:** vento nas plantas e partículas de clima, uma por tema (pólen, folhas, pétalas, poeira dourada, vaga-lumes, almas, chuva e bolhas);
    no chão, barris, estrelas-do-mar e lampiões;
  - **o mapa:** um arquipélago com mar fundo e praias, palmeiras, o farol, navios navegando, o forte da Capitã, o navio fantasma e a Rocha da Caveira;
  - **pixel nítido:** a tela é pixelada em escala inteira.
- **Trilha sonora de pirata, procedural** (`Musicas.cs`): toda composta e sintetizada por código, com um sintetizador próprio
  (acordeão, apito, violão de cordas beliscadas, violino, tambor de mão, pisadas no convés, palmas, o sino do navio e as ondas do mar):
  - **fases:** "Jiga do Grumete", uma dança de taverna em 6/8 (forma AABB) em ré menor, com o acordeão e o apito revezando a melodia;
    cada fase toca num tom um pouco diferente;
  - **mapa:** "Balada do Arquipélago", calma, em 3/4 e lá menor: violão dedilhado, ondas indo e vindo e o sino de vez em quando;
  - **uma música para cada chefe**, no clima de "música de chefe" de jogo indie (rápida, com um gancho que gruda), misturando os instrumentos de pirata com ondas 8-bit:
    - **Capitã Âncora:** mi menor, 160 bpm, gancho no ritmo 3-3-2 no acordeão, violino e o violão "girando" em semicolcheias, pisadas e palmas;
    - **Cozinheira:** dó menor, uma valsa sombria em 6/8 com acordeão, violino e caixinha de música;
    - **Baleia:** ré menor, 172 bpm, com estalos de orquestra, coro, baixo galopando, o "canto da baleia" (notas que deslizam) e o gancho subindo um tom no clímax;
    - **Kraken:** "Abraço do Abismo", fá frígio, 150 bpm: tímpanos, coro, o "lamento" da lula (notas deslizando para baixo), um riff no baixo que se enrola
      como um tentáculo (fá, sol bemol, fá) e o gancho subindo meio tom na volta;
  - morrer na luta não recomeça a música: ela continua de onde estava.
- **Sons:**
  - efeitos 8-bit gerados por código (pulo, moeda, pisão, mola, serra, porta, risada, rugido, conquista, vitória);
  - a **Volta da Maré**: uma onda que sobe e quebra, com o sino do navio caindo bem no renascimento, e um lamento curto de acordeão.

## 3.4 Imersão
- A Volta da Maré vira a regra do jogo: o Grumete lembra e o mundo esquece.
- Os personagens comentam o desempenho (fala diferente se você passou sem morrer).
- A névoa do mapa esconde as ilhas até você chegar nelas, e a Baleia Branca nada no céu do mapa como ameaça distante.

## 3.5 Feedback
- **Morte:**
  - a tela perde a cor;
  - aberração cromática;
  - os tentáculos do Kraken (vermelhos) avançam, com o fundo vermelho-escuro;
  - a onda e o sino da Volta da Maré;
  - uma mensagem de morte (são mais de 60).
- **Renascimento:** um "soco" de distorção de lente e uma tremidinha de tela.
- **Impacto:** tremida de tela (esmagador, chefes, a morte), poeira proporcional à queda e estica-e-achata.
- **Avisos dos perigos:** linhas vermelhas, "!", marcas no chão, olhos vermelhos, cristais mudando de cor, as linhas da tempestade de âncoras, "tic-tic-TAC" e folhas antes do vento.
- **Progresso:** barra de progresso, barra do chefe com as etapas, aviso de conquista, as caveiras e as medalhas no mapa, o "NOVO RECORDE!" e o X vermelho onde você morreu.

---

# 4. LEVEL DESIGN

Dificuldade de 1 a 5 (estimativa da equipe). "Saves" = cristais `s` na fase. A largura está em blocos (todas as fases têm 15 de altura).

| # | Fase (tema) | Largura | Mecânica nova | Trollagem principal | Saves | Dif. |
|---|---|---|---|---|---|---|
| 1 | Bem-vindo :) (porto) | 82 | Chão que cai, `?`/`K`, bloco invisível, espinho escondido, espinho do teto, cano, bandeira fujona | O tutorial manda "pular BEM alto" para dentro do bloco invisível, e a bandeira foge | 0 | 1 |
| 2 | Confia em mim (e corre!) (selva) | 156 | Esmagador, nuvem assassina, mola, chão falso, bandeira falsa; serras (por trás e pela frente) | "Última bandeira. Agora é sério." é uma bandeira falsa, e logo depois "Não olhe para trás" | 2 | 2 |
| 3 | O Final (mudei de ideia) (lagoa) | 142 | Ratos do Porão, moeda assassina | O "final" continua; "Pegue as moedas! TODAS!", e uma delas mata (a secreta exige todas as outras) | 1 | 2 |
| 4 | A Capitã Âncora (forte) | 126 | Âncora giratória (também sobre buracos); **chefe Capitã Âncora** | "Vai lá dar oi. Eu espero aqui. Bem aqui. Longe." | 1 | 3 |
| 5 | Cadê a direita? (Foge, bloco!) (vila) | 172 | Controles invertidos, caranguejo espinhoso, bloco tímido, bandeira que volta | "Tudo normal por aqui", e depois do save tudo inverte; a primeira "bandeira" do fim manda você de volta ao checkpoint | 2 | 3 |
| 6 | Ponte para o Porto (desfiladeiro, noite) | 196 | Plataformas `j`, `J` (despenca) e `D` (expressa) | "Essa aqui é IGUALZINHA à outra", e não é | 3 | 3 |
| 7 | A Gruta da Cozinheira (gruta escura) | 112 | Fase escura, facas da Cozinheira (na frente e atrás); **chefe Cozinheira** | "A moeda ali em cima também é de confiança. :)" (não é) | 1 | 4 |
| 8 | O Navio Fantasma (cabines e convés) | 218 | Portas das cabines, blocos do ritmo, bloco falso `A`, esmagador do ritmo | Uma porta leva a um beco cheio de caranguejos; "Agora é só ritmo puro, sem pegadinha", e vem uma bandeira falsa | 3 | 4 |
| 9 | Vento em Popa! (penhasco) | 208 | Vento do mar e redemoinho | O segundo redemoinho, "igualzinho", desliga no meio | 2 | 4 |
| 10 | A Baleia Branca (mar aberto, noite) | 140 | **Chefe Baleia Branca** | Aquecimento noturno com nuvens, espinhos que caem e chão que despenca, depois a arena | 1 | 5 |
| 11 | O Tsunami (CORRE!) (ilhas do Kraken, chuva) | 210 | O tsunami do Kraken | Mola "para as moedas", que leva às nuvens assassinas | 2 | 4 |
| 12 | O Verdadeiro Final (juro) (pôr do sol) | 192 | — (revisão, bem mais longa) | "Pode encostar na bandeira. Essa é de verdade", mas há bandeiras falsas e chão que cai | 1 | 4 |
| 13 | O Kraken Vermelho (covil, fundo do mar) | 100 | Canos com tentáculos; **chefe final Kraken Vermelho** (rouba moedas) | O tentáculo AMARELO "não mata"... mas leva as suas moedas | 1 | 5 |
| ★ | Ilha do Louro (secreta) | 72 | — | "Aqui não tem pegadinha"... "tá, só uma." | 0 | 1 |

### A estrutura (13 fases e 4 chefes)
- **3 fases e a Capitã, 2 fases e a Cozinheira, 2 fases e a Baleia, 2 fases e o Kraken.** Antes eram 16 fases curtas; as que eram parecidas
  viraram uma fase só, mais longa (Confia + Corre, O Final + Eu mudo de ideia, Cadê a direita + Foge bloco, Cabines + Dança das Marés),
  e as outras ganharam um trecho novo e mais difícil (manguais sobre buracos, atiradoras de facas, plataforma com serra, rajada logo no começo,
  mais parkour no tsunami e uma revisão bem mais longa no Verdadeiro Final). A fase 1 continua sendo o tutorial curto.
- Na emenda de duas fases, a bandeira da primeira some e no lugar do começo da segunda fica um **checkpoint**.
- **Cada fase tem um tema único:** um lugar (porto, selva, lagoa, forte, vila, desfiladeiro, gruta, navio fantasma, penhasco, mar aberto,
  ilhas do Kraken, rochedo, covil), a cor do céu, a hora (dia, noite ou escuro) e as partículas de clima (pólen, folhas, pétalas, poeira,
  vaga-lumes, almas, chuva, bolhas).
- **A história fecha no Kraken Vermelho:** é ela que está por trás de tudo (os tentáculos dos canos e da tela de morte são dela, vermelhos),
  mandou a Baleia e levantou o tsunami.

### Princípios usados
- **Uma ideia nova por fase**, apresentada num lugar seguro e depois combinada com o que já se conhece.
- **Ensinar com o Louro, trair com o Louro:** a placa é tutorial e armadilha ao mesmo tempo. Nos chefes as dicas dele são sempre verdadeiras.
- **Saves antes dos trechos longos:** toda fase longa tem pelo menos um checkpoint, e as emendas de duas fases ganharam um no lugar do começo da segunda.
- **Fases desenhadas em texto** (`Fases.cs`): iterar é editar uma linha e dar Play.
- **O mapa também é texto** (`MapaDoMundo.cs`): as ilhas, o mar e as pontes. Um script de apoio confere se cada fase continua ligada à próxima, sem atalhos.

---

# 5. BALANCEAMENTO E TESTES

## Ferramentas
- **Checador de fases** (`Ferramentas/checar_fase.py`):
  - simula a física real do `Jogador.cs` (passos de 0,02 s) e procura um caminho do `P` até a bandeira sem encostar em células mortais;
  - com `--de s`, testa cada trecho a partir de cada save;
  - garante o pilar "sempre passável" a cada mudança de mapa.
- **Simuladores dos chefes:** scripts usados durante o desenvolvimento (fora do repositório). Reproduzem os ataques e os tempos para conferir se sempre existe um lugar seguro e se o aviso dá tempo de reagir.
  O da tempestade de âncoras confere cada rajada (a ida e a volta de todas as âncoras): de qualquer lugar da arena existe um jeito de escapar
  (andar até um lugar e pular na hora certa), andando a 7 blocos/s e com o pulo de verdade.
- **Gerador das músicas** (`Ferramentas/ouvir_musicas`): gera cada música em WAV (uma vez e duas vezes seguidas, para ouvir se o loop emenda) e mostra o volume de cada parte da mistura.
- **Painel F10 e atalho F9:** permitem testar efeitos, desempenho (FPS e pior quadro) e qualquer fase ou chefe sem jogar tudo.

## Ajustes depois dos playtests
- **Inversão de controles:**
  - antes estava em várias fases; agora fica **só num trecho** da fase 7, depois do save;
  - a contagem ficou em **0,4 s**, com aviso e cancelamento se você voltar;
  - quem corria cruzava as duas linhas e a inversão era cancelada sem querer, então o trecho foi alongado.
- **Bandeiras falsas:** agora têm um **degrau** (tijolo) antes delas, e dá para pular por cima. A bandeira tem 3 de altura e o pulo alcança cerca de 2,8.
- **Folga nas hitboxes dos chefes:**
  - o perigo só mata se encostar de verdade (folga de 0,15 na Capitã e 0,08 na Cozinheira e na Baleia);
  - a "caixa" do Grumete que conta para morrer é menor que o desenho.
- **Aviso mínimo de 0,55 s** para todo ataque que pede pulo, mesmo na etapa 3.
- **Capitã Âncora:** a janela de dano (a âncora presa) ficou mais longa (era 2,2 / 1,9 / 1,6 s e virou 3 / 2,7 / 2,4 s), mas a luta ficou mais
  rápida e cheia de combos (avisos mais curtos, arremessos e giros mais rápidos, mais ataques por rodada) e ganhou a **tempestade de âncoras**.
- **Fundos nítidos:** antes as camadas do fundo eram borradas (e o tilt-shift borrava o alto e o pé da tela em faixas): ficava com cara de mancha.
  Agora as camadas são desenhadas com volume e detalhe (tábuas, velas, folhas), sem borrar.
- **Caixas de fala:** antes o texto usava a fonte padrão numa caixa de tamanho fixo e era cortado. Agora é a fonte pixel, quebrada em linhas.
- **A morte ficou mais curta** a partir da 3ª morte na mesma fase, e pode ser pulada.
- **O save não "muda de lugar" em fases de chefe** ("seria maldade demais").
- **Checkpoint por etapa nos chefes** (como em Helltaker), em vez de recomeçar a luta inteira.
- **Gráficos "Simples"** para computadores fracos e navegadores lentos. A tela pixelada também reduz muito o custo de desenho.

---

# 6. GERENCIAMENTO DE ESTADOS E ERROS

## Máquina de estados (GerenciadorDoJogo.cs)
`Título ⇄ Conquistas` → `Mapa` → `Jogando ⇄ Pausado` → `Morreu → Jogando` → `FaseConcluida → Mapa` → ... → `Vitória → Créditos → Título`

- Um único `GerenciadorDoJogo` (singleton) que **se cria sozinho** ao dar Play. A `Interface` só lê o estado e não decide nada.
- **Transições com sombras:** a troca de tela só acontece com a tela coberta. Durante a transição, nenhuma entrada é processada.
- **Pausa:** `Time.timeScale = 0` e o áudio pausado (a música só abaixa). Ao destruir o gerenciador, o `timeScale` volta a 1, para não travar o editor.
- **Morte:** ignorada fora do estado `Jogando` (não dá para morrer duas vezes nem durante "fase concluída").
- **Recriação da fase a cada morte:** a fase é destruída e montada de novo do texto. Nenhuma armadilha fica "meio disparada" e não sobra estado sujo.
  O que precisa sobreviver (save, etapa do chefe, chefe já vencido, marcas das mortes) fica no gerenciador.

## Saves (Progresso.cs, PlayerPrefs)
- O que é salvo:
  - a partida em andamento (o ponto do mapa, a fase mais longe, mortes, moedas, tempo e se a partida vale recorde);
  - as mortes por fase;
  - a fase secreta;
  - o recorde;
  - os recordes de cada fase (tempo, mortes e moedas), que dão as medalhas;
  - as conquistas e as opções.
- **Validação de versão:** o save guarda quantas fases o jogo tinha. Se a lista de fases mudou (um save antigo de 16 fases), o "Continuar" some, em vez de carregar a fase errada.
- **Recordes pelo nome da fase:** os recordes e medalhas são guardados pelo nome (`cilada_recorde_v2_<nome>`), não pelo número: reordenar as fases não manda
  um recorde para a fase errada. O "v2" fez as 13 fases novas começarem do zero (a secreta, que não mudou, manteve os recordes).
- **O roubo do Kraken** salva as moedas na hora (fechar o jogo no meio da luta não devolve nada).
- Os índices são limitados (`Mathf.Clamp`) ao carregar o mapa.
- A leitura das mortes por fase tolera texto inválido (vira 0).
- O jogo salva ao entrar numa fase, ao passar de fase, ao voltar ao mapa e ao fechar.

## Robustez
- **Shaders próprios com plano B:** se a placa de vídeo não aceitar o shader, o efeito usa o material padrão (nunca aparece um sprite rosa).
- **Conquista com id desconhecido:** gera um aviso no Console e não quebra o jogo.
- **Letra desconhecida na fonte pixel:** não quebra o texto.
- **Desenho que não existe** (alguém mudou o nome de um sprite do mapa): o mapa avisa no Console e usa uma moeda no lugar.
- **Músicas geradas uma vez só:** cada música é gerada na primeira vez (meio segundo) e fica guardada; a do chefe é gerada quando a fase carrega, atrás da transição.
- **"Enter Play Mode Options" sem recarregar o domínio:** os estáticos são limpos com `RuntimeInitializeOnLoadMethod`.

---

# 7. EQUIPE, CRONOGRAMA E RISCOS

## Divisão de tarefas *(editar)*
| Integrante | Responsabilidades |
|---|---|
| (Nome 1) | (ex.: programação do núcleo, chefes) |
| (Nome 2) | (ex.: level design, checador de fases) |
| (Nome 3) | (ex.: arte procedural, efeitos visuais) |
| (Nome 4) | (ex.: música e som, GDD, página do itch.io, playtests) |

## Cronograma *(editar as datas)*
| Semana | Entrega |
|---|---|
| (data) | Conceito, pilares, protótipo do movimento e das primeiras armadilhas |
| (data) | Fases 1 a 5, mapa do mundo, Volta da Maré |
| (data) | Mecânicas temáticas (fases 6 a 15) e chefe Capitã Âncora |
| (data) | Chefes Cozinheira e Baleia Branca, visual HD-2D |
| (data) | Menus (Opções, Como Jogar, Créditos), narrativa, polimento |
| (data) | Tema pirata (mapa do arquipélago, fundos, trilha), recordes e medalhas, playtests, build WebGL |
| **11/11/2026 23:50** | **Entrega:** build no itch.io (jam), README, GDD |

## Riscos e mitigação
| Risco | Mitigação |
|---|---|
| Difícil demais e o jogador desiste | Morte rápida e pulável, saves, aviso nos chefes, checkpoint por etapa, checador de fases, playtests |
| Desempenho ruim no navegador | Tela pixelada (menos pixels), gráficos "Simples", nada é criado durante o jogo, painel F10 para medir |
| Direitos autorais | Mundo, personagens, arte, música e sons todos originais e feitos por código; nenhum arquivo de mídia externo; a Baleia Branca é homenagem a uma obra em domínio público |
| Save quebrado entre versões | Validação pelo número de fases e valores limitados |
| Escopo grande para o prazo | Fases e mapa em texto (iteração rápida), mecânica nova = arquivo novo, lista de pendências em `PROXIMOS_PASSOS.md` |
| Bug só no build WebGL | Gerar o build cedo e testar no itch.io (página em rascunho) antes da entrega |
