# Desenha os quadros da HISTÓRIA (a cutscene da abertura e a do final) em pixel art, no estilo do jogo.
# Os personagens são os MESMOS desenhos do jogo: o script lê os desenhos em texto direto dos .cs
# (ArteDosPersonagens, ArteDaCapita, ChefeCozinheira...) com as mesmas paletas.
#
# Uso (na pasta do projeto):   python Ferramentas/historia/desenhar_historia.py
# Saída: Assets/Resources/Arte/historia/<nome>.png (256 x 144, o jogo amplia sem borrar)
#        Ferramentas/previews/historia/<nome>.png (a mesma imagem 4x maior, para ver fora do jogo)
import math, os, random, re, sys
from PIL import Image

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SCRIPTS = os.path.join(RAIZ, 'Assets', 'Scripts')
SAIDA = os.path.join(RAIZ, 'Assets', 'Resources', 'Arte', 'historia')
PREVIA = os.path.join(RAIZ, 'Ferramentas', 'previews', 'historia')
W, H = 256, 144

# ------------------------------------------------------------------ lendo os desenhos e as paletas dos .cs

def ler(arquivo):
    return open(os.path.join(SCRIPTS, arquivo), encoding='utf-8-sig').read()

def desenhos(arquivo):
    """Todos os 'static readonly string[] Nome = { "...", ... };' do arquivo."""
    texto = ler(arquivo)
    saida = {}
    for m in re.finditer(r'static readonly string\[\] (\w+)\s*=\s*(?://[^\n]*)?\s*\{(.*?)\};', texto, re.S):
        linhas = re.findall(r'"([^"]*)"', m.group(2))
        if linhas:
            saida[m.group(1)] = linhas
    return saida

def paleta(arquivo, nome_do_dicionario):
    texto = ler(arquivo)
    m = re.search(nome_do_dicionario + r'\s*=\s*new Dictionary<char, Color32>\s*\{(.*?)\};', texto, re.S)
    cores = {}
    for c, r, g, b, a in re.findall(r"\{ '(.)', new Color32\((\d+), (\d+), (\d+), (\d+)\) \}", m.group(1)):
        cores[c] = (int(r), int(g), int(b), int(a))
    return cores

PAL_PERSONAGENS = paleta('Nucleo/ArteDosPersonagens.cs', 'Paleta')
PAL_JOGO = paleta('Nucleo/FabricaDeSprites.cs', 'paleta')
PAL_CAPITA = paleta('Objetos/ArteDaCapita.cs', 'paleta')
D_PERSONAGENS = desenhos('Nucleo/ArteDosPersonagens.cs')
D_CAPITA = desenhos('Objetos/ArteDaCapita.cs')
D_COZINHEIRA = desenhos('Objetos/ChefeCozinheira.cs')

def sprite(linhas, pal, escala=1, espelhar=False, troca=None):
    """Um desenho em texto vira uma imagem (cada letra = um pixel da paleta; o resto é transparente)."""
    troca = troca or {}
    h, w = len(linhas), max(len(l) for l in linhas)
    img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    px = img.load()
    for y, linha in enumerate(linhas):
        for x, c in enumerate(linha):
            c = troca.get(c, c)
            if c in pal:
                px[x, y] = pal[c]
    if espelhar:
        img = img.transpose(Image.FLIP_LEFT_RIGHT)
    if escala != 1:
        img = img.resize((w * escala, h * escala), Image.NEAREST)
    return img

def grumete(escala=1, espelhar=False):
    return sprite(D_PERSONAGENS['Grumete'], PAL_PERSONAGENS, escala, espelhar)

def louro(escala=1, espelhar=False):
    return sprite(D_PERSONAGENS['Louro'], PAL_PERSONAGENS, escala, espelhar)

def personagem(nome, escala=1, espelhar=False, troca=None):
    return sprite(D_PERSONAGENS[nome], PAL_PERSONAGENS, escala, espelhar, troca)

def capita(furiosa=False, escala=1, espelhar=False):
    cabeca = list(D_CAPITA['Cabeca'])
    if furiosa:
        cabeca[11] = D_CAPITA['OlhoFurioso'][0]
        cabeca[12] = D_CAPITA['OlhoFurioso'][1]
        cabeca[6] = D_CAPITA['CaveiraFuriosa'][0]
    return sprite(cabeca[4:] + D_CAPITA['CorpoParada'], PAL_CAPITA, escala, espelhar)

def cozinheira(maligna=False, escala=1, espelhar=False):
    troca = {'V': 'r'} if maligna else {}
    return sprite(D_COZINHEIRA['ArteCozinheiraDePe'], PAL_JOGO, escala, espelhar, troca)

# ------------------------------------------------------------------ desenhos novos (só da história)

# a paleta dos desenhos novos: a dos personagens (ouro, madeira, pele...) com alguns extras
PAL = dict(PAL_PERSONAGENS)
PAL.update({
    'h': (250, 236, 210, 255),   # barba branca (luz)
    'H': (206, 190, 170, 255),   # barba (sombra)
    'j': (120, 70, 170, 255),    # roxo do manto
    'J': (82, 44, 128, 255),     # roxo do manto (sombra)
    's': (176, 222, 255, 255),   # joia azul
    'q': (255, 120, 140, 255),   # joia vermelha
})

# O BAÚ do tesouro, aberto e transbordando de ouro (28 x 21)
BAU = [
    "......kkkkkkkkkkkkkkkk......",
    ".....kBBBBBBBBBBBBBBBBk.....",
    "....kBTTTTTTTTTTTTTTTTBk....",
    "....kBTkkkkkkkkkkkkkkTBk....",
    "....kBTkXYYXYYqYYXYYkTBk....",
    "...kkkkXYYYYXYYYsYYYXkkkk...",
    "..kXYYXYYqYYYXYYYYXYYYXYYk..",
    ".kXYYYYYYYYXYYYYYYYYqYYYYYk.",
    "kYYXYYsYYYYYYYXYYYYYYYYXYYYk",
    "kbBBBBBBBBBBBBBBBBBBBBBBBBbk",
    "kbYYYYYYYYYYYYYYYYYYYYYYYYbk",
    "kbBBBBBBBBBBkYYkBBBBBBBBBBbk",
    "kbBBBBBBBBBBkYDkBBBBBBBBBBbk",
    "kbBBBBBBBBBBkkkkBBBBBBBBBBbk",
    "kbBBBBBBBBBBBBBBBBBBBBBBBBbk",
    "kbYYYYYYYYYYYYYYYYYYYYYYYYbk",
    "kbBBBBBBBBBBBBBBBBBBBBBBBBbk",
    "kbbbbbbbbbbbbbbbbbbbbbbbbbbk",
    ".kkkkkkkkkkkkkkkkkkkkkkkkkk.",
]

# O DEUS DA RIQUEZA: coroa de ouro, barba branca, manto roxo com bordas douradas e uma moeda na mão (24 x 36)
DEUS = [
    "........k..k..k.........",
    ".......kYkkYkkYk........",
    ".......kYsYYqYYk........",
    ".......kYYYYYYYk........",
    "......kk2222222kk.......",
    "......k122222223k.......",
    "......k12E222E23k.......",
    "......k122222223k.......",
    "......khhh222hhhk.......",
    ".....khhhhhhhhhhHk......",
    ".....khhhhhhhhhhHk......",
    "......khhhhhhhhHk.......",
    "....kkkkhhhhhhHkkkk.....",
    "...kjjjYkhhhhHkYjjjk....",
    "..kjjjjYYkhhHkYYjjjjk...",
    "..kjjjjjYYkkkYYjjjjjk...",
    ".kjjjjjjjYYYYYjjjjjjjk..",
    ".kjjjjjjjjYYYjjjjjjjjk..",
    "k22kjjjjjjYYYjjjjjjk22k.",
    "k22kjjjjjjjYjjjjjjjk22k.",
    ".kkjjjjjjjjYjjjjjjjjkk..",
    "..kjjjjjjjjYjjjjjjjjk...",
    "..kjjjjjjjjYjjjjjjjJk...",
    "..kjjjjjjjjYjjjjjjJJk...",
    "..kjjjjjjjjYjjjjjjJJk...",
    "..kjjjjjjjjYjjjjjJJJk...",
    ".kjjjjjjjjjYjjjjjJJJJk..",
    ".kjjjjjjjjjYjjjjJJJJJk..",
    ".kYYYYYYYYYYYYYYYYYYYk..",
    ".kyyyyyyyyyyyyyyyyyyyk..",
    "..kkkkkkkkkkkkkkkkkkk...",
]

# O NAVIO do Grumete: vela vermelha com caveira, mastro e casco de madeira (40 x 34)
NAVIO = [
    "...................kk...................",
    "..................kRRk..................",
    "..................kRRRk.................",
    "..................kBk...................",
    "...........kkkkkkkkBkkkkkkkkk...........",
    "...........kRRRRRRRBRRRRRRRRk...........",
    "..........kRRRRRRRRBRRRRRRRRRk..........",
    "..........kRRRRWWWWBWWWRRRRRRk..........",
    "..........kRRRWWWWWBWWWWRRRRRk..........",
    "..........kRRRWEEWWBWEEWRRRRRk..........",
    "..........kRRRWEEWWBWEEWRRRRrk..........",
    "..........kRRRWWWWEBEWWWRRRRrk..........",
    "..........kRRRRWWWWBWWWWRRRrrk..........",
    "..........kRRRRWkWkBkWkWRRRrrk..........",
    "...........kRRRRRRRBRRRRRRrrk...........",
    "...........kRRRRRRRBRRRRRrrrk...........",
    "............kkkkkkkBkkkkkkkk............",
    "...................B....................",
    "...................B....................",
    "...................B....................",
    "..kk...............B..................k.",
    "..kTk..............B.................kTk",
    "..kTTkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkkTTk",
    "..kTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTk.",
    "...kBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBk..",
    "...kBkYkBBBBkYkBBBBkYkBBBBkYkBBBBBBBk...",
    "....kBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBk....",
    "....kbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbk....",
    ".....kbbbbbbbbbbbbbbbbbbbbbbbbbbbbk.....",
    "......kkkkkkkkkkkkkkkkkkkkkkkkkkkk......",
]

def bau(escala=1):
    return sprite(BAU, PAL, escala)

def deus(escala=1):
    return sprite(DEUS, PAL, escala)

def navio(escala=1, espelhar=False):
    return sprite(NAVIO, PAL, escala, espelhar)

def moeda(escala=1):
    return personagem('Moeda', escala)

# ------------------------------------------------------------------ pincéis de pixel art

BAYER = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5]

def limite(x, y):
    return (BAYER[(y % 4) * 4 + (x % 4)] + 0.5) / 16.0

def misturar(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3)) + (255,)

class Quadro:
    def __init__(self, fundo=(0, 0, 0)):
        self.img = Image.new('RGBA', (W, H), fundo + (255,))
        self.px = self.img.load()

    def ponto(self, x, y, cor):
        x, y = int(x), int(y)
        if 0 <= x < W and 0 <= y < H:
            if len(cor) == 4 and cor[3] < 255:
                self.px[x, y] = misturar(self.px[x, y], cor, cor[3] / 255)
            else:
                self.px[x, y] = cor[:3] + (255,)

    def ceu(self, y0, y1, cores):
        """Degradê em faixas (com pontilhado entre as faixas, como pixel art de verdade)."""
        n = len(cores) - 1
        for y in range(y0, y1):
            p = (y - y0) / max(1, y1 - y0 - 1) * n
            i = min(int(p), n - 1)
            f = p - i
            for x in range(W):
                self.ponto(x, y, cores[i + 1] if f > limite(x, y) else cores[i])

    def retangulo(self, x0, y0, x1, y1, cor):
        for y in range(int(y0), int(y1)):
            for x in range(int(x0), int(x1)):
                self.ponto(x, y, cor)

    def circulo(self, cx, cy, r, cor):
        for y in range(int(cy - r - 1), int(cy + r + 2)):
            for x in range(int(cx - r - 1), int(cx + r + 2)):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r:
                    self.ponto(x, y, cor)

    def brilho(self, cx, cy, r, cor, forca=1.0):
        """Uma luz redonda que some em pontinhos (Bayer): mais clara no meio."""
        for y in range(int(cy - r), int(cy + r + 1)):
            for x in range(int(cx - r), int(cx + r + 1)):
                d = math.hypot(x - cx, y - cy) / r
                if d < 1 and (1 - d) * forca > limite(x, y):
                    self.ponto(x, y, cor)

    def raio_de_luz(self, x_topo, largura_topo, x_base, largura_base, y0, y1, cor, forca=0.5):
        for y in range(y0, y1):
            t = (y - y0) / max(1, y1 - y0)
            meio = x_topo + (x_base - x_topo) * t
            meia = (largura_topo + (largura_base - largura_topo) * t) / 2
            for x in range(int(meio - meia), int(meio + meia) + 1):
                borda = 1 - abs(x - meio) / max(1, meia)
                if borda * forca * (1 - 0.6 * t) > limite(x, y):
                    self.ponto(x, y, cor)

    def estrelas(self, quantas, y_max, semente, cores=((255, 255, 255), (200, 210, 255))):
        rnd = random.Random(semente)
        for _ in range(quantas):
            x, y = rnd.randrange(W), rnd.randrange(y_max)
            c = rnd.choice(cores)
            self.ponto(x, y, c)
            if rnd.random() < 0.12:  # as maiores: uma cruzinha
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    self.ponto(x + dx, y + dy, misturar(c, (40, 40, 80), 0.5))

    def nuvem(self, cx, cy, largura, cor, sombra, semente=1):
        rnd = random.Random(semente)
        bolhas = [(cx + rnd.uniform(-largura / 2, largura / 2), cy + rnd.uniform(-2, 2), rnd.uniform(largura / 7, largura / 4))
                  for _ in range(6)]
        for y in range(int(cy - largura / 2), int(cy + largura / 3)):
            for x in range(int(cx - largura), int(cx + largura)):
                dentro = any((x - bx) ** 2 + ((y - by) * 1.6) ** 2 <= br * br for bx, by, br in bolhas)
                if dentro and y <= cy + 3:
                    baixo = any((x - bx) ** 2 + ((y + 2 - by) * 1.6) ** 2 <= br * br for bx, by, br in bolhas)
                    self.ponto(x, y, cor if y < cy - 1 or not baixo else sombra)

    def mar(self, y0, cores, onda, brilho_cor, semente=3, agito=1.0):
        """O mar: faixas que escurecem para baixo e risquinhos de onda que brilham."""
        self.ceu(y0, H, cores)
        rnd = random.Random(semente)
        for _ in range(int(140 * agito)):
            y = rnd.randrange(y0 + 1, H)
            x = rnd.randrange(-6, W)
            comp = rnd.randrange(3, 4 + int((y - y0) / 6))
            for i in range(comp):
                self.ponto(x + i, y, onda)
            if rnd.random() < 0.35:
                self.ponto(x + comp // 2, y - 1, brilho_cor)

    def caminho_de_luz(self, cx, y0, largura, cor, semente=4):
        """O reflexo da lua/do sol no mar: risquinhos claros numa coluna."""
        rnd = random.Random(semente)
        for y in range(y0, H, 2):
            meia = largura * (0.3 + 0.7 * (y - y0) / (H - y0))
            for _ in range(2):
                x = cx + rnd.uniform(-meia, meia)
                comp = rnd.randrange(2, 6)
                for i in range(comp):
                    self.ponto(x + i, y, cor)

    def morro(self, x0, x1, y_base, altura, cor, semente=5, picos=3):
        rnd = random.Random(semente)
        centros = [(rnd.uniform(x0, x1), rnd.uniform(altura * 0.6, altura)) for _ in range(picos)]
        for x in range(int(x0) - 10, int(x1) + 10):
            topo = 0
            for cxm, alt in centros:
                largura = (x1 - x0) / picos * 0.9
                topo = max(topo, alt * max(0, 1 - ((x - cxm) / largura) ** 2))
            for y in range(int(y_base - topo), y_base):
                self.ponto(x, y, cor)

    def palmeira(self, x, y_base, altura, cor):
        for i in range(altura):
            self.ponto(x + int(math.sin(i / 4) * 1.5), y_base - i, cor)
        tx, ty = x + int(math.sin(altura / 4) * 1.5), y_base - altura
        for ang in (-150, -110, -60, -20, 200):
            for r in range(1, 8):
                a = math.radians(ang)
                self.ponto(tx + math.cos(a) * r, ty + math.sin(a) * r + (r * r) / 18, cor)

    def raio(self, x, y0, y1, semente, cor=(255, 250, 200), brilho_cor=(180, 170, 255)):
        rnd = random.Random(semente)
        y, xx = y0, x
        while y < y1:
            passo = rnd.randrange(3, 7)
            dx = rnd.choice((-3, -2, 2, 3))
            for i in range(passo):
                px = xx + dx * i / passo
                self.ponto(px, y + i, cor)
                self.ponto(px + 1, y + i, brilho_cor)
            xx += dx
            y += passo

    def chuva(self, quantas, cor, semente=9):
        rnd = random.Random(semente)
        for _ in range(quantas):
            x, y = rnd.randrange(W + 20), rnd.randrange(-10, H)
            for i in range(4):
                self.ponto(x - i, y + i * 2, cor)

    def colar(self, spr, x, y, alfa=1.0):
        """Cola um sprite com o canto de cima-esquerda em (x, y)."""
        x, y = int(x), int(y)
        sp = spr.load()
        for j in range(spr.size[1]):
            for i in range(spr.size[0]):
                c = sp[i, j]
                if c[3] > 0:
                    a = c[3] / 255 * alfa
                    self.ponto(x + i, y + j, c[:3] + (int(255 * a),))

    def colar_no_chao(self, spr, centro_x, y_pe, alfa=1.0):
        self.colar(spr, centro_x - spr.size[0] // 2, y_pe - spr.size[1], alfa)

    def aura(self, spr, x, y, cor, grossura=2):
        """Um contorno brilhante em volta de um sprite (antes de colar o sprite por cima)."""
        sp = spr.load()
        w, h = spr.size
        for j in range(-grossura, h + grossura):
            for i in range(-grossura, w + grossura):
                perto = 99
                for dj in range(-grossura, grossura + 1):
                    for di in range(-grossura, grossura + 1):
                        ii, jj = i + di, j + dj
                        if 0 <= ii < w and 0 <= jj < h and sp[ii, jj][3] > 0:
                            perto = min(perto, max(abs(di), abs(dj)))
                if 0 < perto <= grossura and (1 - (perto - 1) / grossura) > limite(i + x, j + y) * 0.9:
                    self.ponto(x + i, y + j, cor)

    def vinheta(self, forca=0.5, cor=(0, 0, 0)):
        for y in range(H):
            for x in range(W):
                d = math.hypot((x - W / 2) / (W / 2), (y - H / 2) / (H / 2))
                if d > 0.75 and (d - 0.75) * 2.2 * forca > limite(x, y):
                    self.ponto(x, y, cor + (150,))

    def salvar(self, nome):
        os.makedirs(SAIDA, exist_ok=True)
        os.makedirs(PREVIA, exist_ok=True)
        destino = os.path.join(SAIDA, nome + '.png')
        # já tem uma imagem PINTADA (maior que 256 x 144, gerada com os prompts)? Não sobrescreve
        # (para trocar pela de código mesmo assim: python desenhar_historia.py --forcar)
        if os.path.exists(destino) and '--forcar' not in sys.argv and Image.open(destino).size[0] > W:
            print('pulei', nome, '(já tem a imagem pintada)')
            return
        self.img.convert('RGB').save(destino)
        self.img.convert('RGB').resize((W * 4, H * 4), Image.NEAREST).save(os.path.join(PREVIA, nome + '.png'))
        print('ok', nome)

# ------------------------------------------------------------------ o Kraken (desenhado aqui, com as cores dela)

KR = {
    'contorno': (30, 6, 12), 'funda': (92, 12, 26), 'vermelha': (176, 30, 42), 'clara': (230, 72, 64),
    'brilho': (255, 132, 110), 'ventosa': (255, 178, 160), 'iris': (255, 206, 72), 'brava': (255, 84, 36),
    'pupila': (16, 6, 10),
}

def tentaculo(q, pontos, grossura0, grossura1, ventosas=True):
    """Um tentáculo pelos pontos (uma curva), afinando da base para a ponta."""
    caminho = []
    for (x0, y0), (x1, y1) in zip(pontos, pontos[1:]):
        passos = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
        for i in range(passos):
            caminho.append((x0 + (x1 - x0) * i / passos, y0 + (y1 - y0) * i / passos))
    n = len(caminho)
    for k, (x, y) in enumerate(caminho):  # contorno primeiro
        r = grossura0 + (grossura1 - grossura0) * k / n
        q.circulo(x, y, r + 1, KR['contorno'])
    for k, (x, y) in enumerate(caminho):
        r = grossura0 + (grossura1 - grossura0) * k / n
        q.circulo(x, y, r, KR['vermelha'])
        q.circulo(x - r * 0.35, y - r * 0.35, max(0.6, r * 0.45), KR['clara'])
    if ventosas:
        for k in range(0, n, 6):
            x, y = caminho[k]
            r = grossura0 + (grossura1 - grossura0) * k / n
            if r > 1.6:
                q.circulo(x + r * 0.4, y + r * 0.4, max(0.8, r * 0.3), KR['ventosa'])

def curva(x0, y0, x1, y1, curvatura, partes=12, onda=0.0, fase=0.0):
    pts = []
    for i in range(partes + 1):
        t = i / partes
        x = x0 + (x1 - x0) * t
        y = y0 + (y1 - y0) * t
        # empurrão para o lado (o tentáculo faz um "S")
        nx, ny = -(y1 - y0), (x1 - x0)
        comp = math.hypot(nx, ny) or 1
        desvio = math.sin(t * math.pi) * curvatura + math.sin(t * math.pi * 3 + fase) * onda
        pts.append((x + nx / comp * desvio, y + ny / comp * desvio))
    return pts

def kraken(q, cx, topo, largura, brava=True, escala_olho=1.0):
    """O manto pontudo com barbatanas, os olhos enormes e a boca; os braços ficam por conta de quem chama."""
    altura = largura * 1.25
    for y in range(int(topo), int(topo + altura)):
        t = (y - topo) / altura
        meia = largura / 2 * min(1, 0.15 + t * 1.6) if t < 0.55 else largura / 2 * (1 - (t - 0.55) * 0.5)
        for x in range(int(cx - meia - 1), int(cx + meia + 2)):
            d = abs(x - cx) / max(1, meia)
            if d <= 1:
                cor = KR['contorno'] if d > 0.92 or y == int(topo) else KR['vermelha']
                if d < 0.92 and x < cx - meia * 0.35 and t < 0.7:
                    cor = KR['clara']
                if d < 0.92 and x > cx + meia * 0.55:
                    cor = KR['funda']
                q.ponto(x, y, cor)
    # as barbatanas
    for lado in (-1, 1):
        for i in range(int(largura * 0.35)):
            for j in range(int(largura * 0.3 - i * 0.7)):
                x = cx + lado * (largura * 0.35 + i)
                y = topo + largura * 0.25 + j - i * 0.4
                q.ponto(x, y, KR['funda'] if lado > 0 else KR['vermelha'])
    # manchinhas
    rnd = random.Random(7)
    for _ in range(int(largura)):
        x = cx + rnd.uniform(-largura * 0.35, largura * 0.35)
        y = topo + rnd.uniform(altura * 0.15, altura * 0.6)
        q.ponto(x, y, KR['brilho'] if x < cx else KR['funda'])
    # os olhos
    oy = topo + altura * 0.66
    r = largura * 0.14 * escala_olho
    for lado in (-1, 1):
        ox = cx + lado * largura * 0.22
        q.circulo(ox, oy, r + 1.2, KR['contorno'])
        q.circulo(ox, oy, r, KR['brava'] if brava else KR['iris'])
        q.retangulo(ox - r * 0.25, oy - r * 0.9, ox + r * 0.25 + 1, oy + r * 0.9, KR['pupila'])
        q.ponto(ox - r * 0.5, oy - r * 0.5, (255, 255, 255))
        if brava:  # a sobrancelha brava
            # (a ponta de dentro, perto do meio da cara, fica mais BAIXA: cara de brava)
            comp = int(r * 2.2)
            for i in range(comp):
                x = ox - lado * (r * 1.1) + lado * i
                y = oy - r * 1.9 + (comp - i) * 0.5
                q.ponto(x, y, KR['contorno'])
                q.ponto(x, y - 1, KR['contorno'])

# ------------------------------------------------------------------ os quadros

def quadro_1():
    """Noite. Uma ilha lá longe, com uma luz dourada saindo da gruta. O navio do Grumete chegando."""
    q = Quadro()
    horizonte = 92
    q.ceu(0, horizonte, [(10, 10, 32), (18, 20, 56), (34, 34, 88), (60, 50, 110)])
    q.estrelas(90, horizonte - 10, 1)
    q.circulo(204, 26, 11, (232, 236, 252))
    q.circulo(207, 24, 10, (200, 206, 236))
    q.circulo(201, 29, 2, (176, 182, 214))
    q.circulo(208, 20, 1.5, (176, 182, 214))
    q.nuvem(60, 30, 26, (70, 70, 130), (50, 50, 104), 2)
    q.nuvem(150, 46, 20, (66, 62, 120), (48, 46, 100), 3)
    # a ilha e a gruta com o brilho do tesouro
    q.raio_de_luz(176, 30, 176, 6, 20, horizonte - 6, (255, 220, 110), 0.55)
    q.morro(140, 214, horizonte, 30, (24, 22, 48), 8, 3)
    q.brilho(176, horizonte - 8, 16, (255, 214, 90), 0.9)
    q.retangulo(171, horizonte - 12, 182, horizonte - 3, (40, 26, 20))
    q.brilho(176, horizonte - 7, 6, (255, 246, 190), 1.2)
    q.mar(horizonte, [(28, 30, 76), (20, 24, 60), (12, 14, 40)], (52, 60, 120), (130, 140, 210), 3)
    q.caminho_de_luz(204, horizonte + 1, 14, (190, 196, 236))
    q.caminho_de_luz(176, horizonte + 1, 6, (255, 214, 110), 5)
    # o navio, de lado, chegando
    q.colar(navio(), 30, horizonte - 26)
    q.brilho(36, horizonte - 6, 5, (255, 200, 90), 0.8)
    q.vinheta(0.6)
    q.salvar('abertura_1')

def quadro_2():
    """Dentro da gruta: o baú gigante transbordando de ouro, e o Grumete (e o Louro) de boca aberta."""
    q = Quadro()
    q.ceu(0, H, [(30, 18, 30), (46, 28, 40), (36, 22, 34), (22, 14, 24)])
    rnd = random.Random(11)
    for _ in range(70):  # pedras na parede
        x, y, r = rnd.randrange(W), rnd.randrange(100), rnd.uniform(2, 6)
        q.circulo(x, y, r, (52, 34, 48) if rnd.random() < 0.5 else (24, 14, 22))
    for i in range(18):  # estalactites
        x = rnd.randrange(W)
        comp = rnd.randrange(6, 20)
        for j in range(comp):
            meia = (comp - j) / comp * 3
            q.retangulo(x - meia, j, x + meia + 1, j + 1, (20, 12, 20))
    chao = 116
    q.retangulo(0, chao, W, H, (40, 26, 30))
    q.ceu(chao, H, [(60, 40, 40), (34, 22, 28)])
    # a luz do tesouro
    cx = 150
    for dx, larg in ((-40, 10), (-15, 14), (12, 12), (38, 9)):
        q.raio_de_luz(cx + dx * 0.2, 4, cx + dx * 1.6, larg * 2, 0, chao - 18, (255, 222, 120), 0.45)
    q.brilho(cx, chao - 22, 60, (120, 80, 40), 0.9)
    q.brilho(cx, chao - 22, 40, (220, 160, 60), 0.8)
    q.brilho(cx, chao - 20, 24, (255, 220, 120), 0.9)
    # montes de moedas
    for mx, mr in ((cx - 46, 18), (cx + 44, 16), (cx, 30), (cx - 80, 10), (cx + 78, 9)):
        for y in range(int(chao - mr * 0.6), chao + 4):
            meia = mr * math.sqrt(max(0, 1 - ((chao + 4 - y) / (mr * 0.6 + 4)) ** 2))
            for x in range(int(mx - meia), int(mx + meia) + 1):
                q.ponto(x, y, (255, 212, 72) if (x + y) % 5 else (255, 248, 196))
                if (x * 7 + y * 3) % 23 == 0:
                    q.ponto(x, y, (214, 146, 38))
    q.colar_no_chao(bau(2), cx, chao - 4)
    for x, y in ((cx - 60, chao - 6), (cx + 58, chao - 4), (cx - 20, chao - 40), (cx + 30, chao - 46)):
        q.colar(moeda(), x, y)
    for x, y, c in ((cx - 36, chao - 8, (255, 120, 140)), (cx + 26, chao - 10, (176, 222, 255)), (cx + 52, chao - 2, (120, 230, 140))):
        q.circulo(x, y, 1.6, c)
        q.ponto(x - 1, y - 1, (255, 255, 255))
    # o Grumete (olhando para o tesouro) e o Louro em cima do ombro
    q.colar_no_chao(grumete(2), 48, chao)
    q.colar(louro(1, True), 52, chao - 44)
    q.brilho(60, chao - 18, 10, (255, 230, 150), 0.25)
    q.vinheta(0.7)
    q.salvar('abertura_2')

def quadro_3():
    """O guardião: o Kraken Vermelho sobe do mar na tempestade, agarra o tesouro, e o Grumete cai."""
    q = Quadro()
    horizonte = 100
    q.ceu(0, horizonte, [(24, 6, 18), (48, 10, 28), (84, 18, 38), (120, 34, 44)])
    q.nuvem(40, 18, 34, (70, 20, 40), (50, 14, 32), 4)
    q.nuvem(210, 26, 40, (66, 18, 38), (46, 12, 30), 5)
    q.raio(230, 0, 60, 3)
    # os braços de trás
    tentaculo(q, curva(200, 150, 242, 40, -16, onda=3), 7, 1.5)
    tentaculo(q, curva(110, 150, 70, 52, 18, onda=3, fase=1), 6, 1.5)
    kraken(q, 160, 6, 64, brava=True)
    # o mar agitado na frente do corpo
    q.mar(horizonte, [(80, 20, 40), (52, 14, 34), (28, 8, 24)], (150, 50, 60), (240, 140, 130), 6, 1.4)
    for x in range(0, W, 3):  # espuma
        y = horizonte + int(math.sin(x / 7) * 2)
        q.ponto(x, y, (255, 200, 190))
        q.ponto(x + 1, y, (230, 140, 130))
    # o braço que segura o baú, bem alto
    tentaculo(q, curva(196, 150, 212, 34, 20, onda=2, fase=2), 7, 2)
    q.colar(bau(1), 198, 12)
    q.brilho(212, 22, 14, (255, 214, 90), 0.5)
    # o braço da frente, que derrubou o Grumete
    tentaculo(q, curva(130, 150, 100, 70, -22, onda=2, fase=3), 8, 2.5)
    # o navio virando, pequeno, e o Grumete caindo
    q.colar(navio().rotate(-28, Image.NEAREST, expand=True), 4, horizonte - 34)
    caindo = grumete(1).rotate(140, Image.NEAREST, expand=True)
    q.colar(caindo, 62, 44)
    for i in range(3):  # risquinhos de "caindo"
        for j in range(5):
            q.ponto(68 + i * 4 - j * 0.5, 36 - j, (255, 220, 220))
    q.chuva(120, (200, 120, 140))
    q.vinheta(0.6)
    q.salvar('abertura_3')

def quadro_4():
    """A maldição: os deuses do mar olham das nuvens; o Grumete, com a aura roxa, sempre volta com a maré."""
    q = Quadro()
    q.ceu(0, H, [(8, 6, 22), (24, 12, 46), (40, 18, 70), (20, 10, 40)])
    q.nuvem(128, 18, 90, (46, 30, 82), (32, 20, 62), 6)
    q.nuvem(40, 26, 40, (40, 26, 74), (30, 18, 56), 7)
    q.nuvem(220, 24, 46, (40, 26, 74), (30, 18, 56), 8)
    # os olhos dos deuses nas nuvens
    for ex in (104, 152):
        q.brilho(ex, 22, 12, (180, 200, 255), 0.7)
        q.circulo(ex, 22, 4, (230, 240, 255))
        q.retangulo(ex - 6, 20, ex + 7, 22, (46, 30, 82))  # a pálpebra brava
    q.raio(40, 30, 96, 5)
    q.raio(214, 30, 90, 6)
    horizonte = 104
    q.mar(horizonte, [(30, 20, 70), (20, 14, 50), (10, 8, 30)], (80, 60, 150), (170, 150, 240), 7)
    # o redemoinho da maré em volta da pedra
    for k in range(260):
        a = k / 260 * math.pi * 6
        r = 6 + k / 260 * 42
        x, y = 128 + math.cos(a) * r, 116 + math.sin(a) * r * 0.32
        q.ponto(x, y, (150, 200, 255) if k % 3 else (230, 240, 255))
    q.morro(108, 148, 122, 12, (34, 28, 48), 9, 2)
    # o Grumete amaldiçoado e a "alma" dele subindo
    g = grumete(2)
    q.aura(g, 112, 122 - 32, (176, 96, 230), 3)
    q.colar(g, 112, 122 - 32)
    fantasma = grumete(2)
    q.aura(fantasma, 112, 40, (120, 70, 190), 2)
    q.colar(fantasma, 112, 40, 0.45)
    for i in range(14):  # o rastro entre os dois
        q.brilho(128 + math.sin(i) * 3, 76 + i, 3, (200, 150, 255), 0.6)
    # caveirinhas roxas girando (a maldição)
    caveira = personagem('Caveira', 1)
    for k in range(4):
        a = k / 4 * math.pi * 2 + 0.4
        q.colar(caveira, 120 + math.cos(a) * 34, 92 + math.sin(a) * 14, 0.55)
    q.vinheta(0.7)
    q.salvar('abertura_4')

def quadro_5():
    """O mundo quebrado: o sol eclipsado, o chão rachado e todo mundo de olho vermelho."""
    q = Quadro()
    horizonte = 96
    q.ceu(0, horizonte, [(20, 4, 14), (60, 10, 30), (120, 26, 40), (170, 60, 50)])
    # o eclipse
    q.brilho(128, 30, 26, (255, 120, 70), 0.9)
    q.circulo(128, 30, 13, (255, 170, 110))
    q.circulo(128, 30, 11, (16, 4, 10))
    q.estrelas(30, 60, 12, ((255, 180, 160),))
    # a terra rachada
    q.retangulo(0, horizonte, W, H, (40, 20, 28))
    q.ceu(horizonte, H, [(56, 26, 34), (30, 14, 22)])
    rnd = random.Random(13)
    for _ in range(7):
        x, y = rnd.randrange(W), rnd.randrange(horizonte + 6, H)
        for i in range(18):
            x += rnd.choice((-2, -1, 1, 2))
            y += rnd.choice((0, 1))
            q.ponto(x, y, (176, 96, 230))
            q.ponto(x, y + 1, (60, 20, 80))
    # a Baleia Branca lá no fundo, saindo do mar de sangue
    for y in range(horizonte - 16, horizonte):
        for x in range(180, 240):
            if ((x - 210) / 30) ** 2 + ((y - horizonte) / 16) ** 2 <= 1:
                q.ponto(x, y, (226, 230, 240) if y < horizonte - 9 else (176, 180, 200))
    q.circulo(196, horizonte - 6, 1.5, (255, 40, 40))
    for i in range(6):  # o jato d'água
        q.ponto(222 + (i % 2), horizonte - 18 - i, (200, 220, 255))
    # tentáculos do Kraken saindo do chão
    tentaculo(q, curva(20, H + 4, 14, 52, 10, onda=2), 5, 1.2)
    tentaculo(q, curva(246, H + 4, 236, 60, -10, onda=2, fase=2), 5, 1.2)
    # a Capitã furiosa, a Cozinheira maligna e os caranguejos de olho vermelho
    chao = 132
    q.aura(capita(True, 2), 58, chao - 60, (220, 40, 60), 2)
    q.colar(capita(True, 2), 58, chao - 60)
    coz = cozinheira(True, 2)
    q.aura(coz, 150, chao - 52, (220, 40, 60), 2)
    q.colar(coz, 150, chao - 52)
    carang = personagem('Caranguejo', 1, troca={'E': 'R', 'W': 'Q'})
    for x in (112, 128, 200, 214):
        q.colar_no_chao(carang, x, chao)
    q.vinheta(0.7)
    q.salvar('abertura_5')

def quadro_6():
    """O Deus da Riqueza desce numa luz dourada, com a coroa de moedas. O Grumete, pequeno, olha para cima."""
    q = Quadro()
    q.ceu(0, H, [(60, 30, 70), (150, 80, 70), (240, 170, 90), (255, 220, 140)])
    for dx in (-60, -30, 0, 30, 60):
        q.raio_de_luz(128 + dx * 0.15, 6, 128 + dx * 1.8, 26, 0, 110, (255, 246, 200), 0.5)
    q.nuvem(70, 92, 40, (255, 236, 200), (240, 200, 160), 14)
    q.nuvem(190, 90, 44, (255, 236, 200), (240, 200, 160), 15)
    q.nuvem(128, 96, 54, (255, 244, 220), (246, 214, 170), 16)
    # a auréola de moedas
    q.brilho(128, 34, 40, (255, 250, 210), 0.8)
    for k in range(12):
        a = k / 12 * math.pi * 2
        q.colar(moeda(), 124 + math.cos(a) * 30, 26 + math.sin(a) * 26)
    q.colar(deus(2), 128 - 24, 18)
    q.nuvem(128, 82, 40, (255, 244, 220), (246, 214, 170), 30)  # o Deus em cima da nuvem
    # moedas caindo (longe do Deus)
    rnd = random.Random(17)
    caiu = 0
    while caiu < 10:
        x, y = rnd.randrange(10, W - 18), rnd.randrange(40, 116)
        if 86 < x < 166:
            continue
        q.colar(moeda(), x, y)
        caiu += 1
    # a praia lá embaixo, com o Grumete e o Louro
    q.retangulo(0, 128, W, H, (240, 206, 140))
    q.ceu(128, H, [(246, 214, 150), (214, 170, 110)])
    q.colar_no_chao(grumete(1), 120, 136)
    q.colar(louro(1, True), 132, 114)
    q.salvar('abertura_6')

def quadro_7():
    """A jornada começa: amanhecer, o navio indo para a direita, e lá no fim do mar, uma sombra vermelha."""
    q = Quadro()
    horizonte = 96
    q.ceu(0, horizonte, [(40, 60, 140), (110, 100, 170), (240, 140, 120), (255, 200, 120)])
    q.nuvem(50, 30, 30, (255, 200, 190), (230, 150, 150), 18)
    q.nuvem(170, 22, 26, (255, 200, 190), (230, 150, 150), 19)
    q.brilho(150, horizonte, 30, (255, 220, 140), 0.9)
    q.circulo(150, horizonte, 12, (255, 240, 180))
    # lá longe: a ilha do Kraken, com nuvem escura e olhinhos vermelhos
    q.morro(212, 252, horizonte, 14, (80, 40, 70), 20, 2)
    q.nuvem(232, 70, 22, (70, 40, 70), (50, 30, 54), 21)
    q.ponto(228, horizonte - 8, (255, 60, 50))
    q.ponto(234, horizonte - 8, (255, 60, 50))
    q.mar(horizonte, [(70, 80, 160), (40, 60, 130), (20, 34, 90)], (110, 130, 210), (255, 220, 160), 22)
    q.caminho_de_luz(150, horizonte + 1, 18, (255, 220, 150), 23)
    # o navio, grandão, com o Grumete e o Louro no convés
    nx, ny = 32, horizonte - 50
    q.colar(navio(2), nx, ny - 8)
    convés = ny - 8 + 22 * 2 + 1  # a linha do convés no desenho do navio (linha 22), em escala 2
    q.colar_no_chao(grumete(2), nx + 60, convés)
    q.colar(louro(1, True), nx + 66, convés - 40)
    for x in range(nx + 6, nx + 80, 4):  # a espuma no casco
        q.ponto(x, ny + 52, (230, 240, 255))
    q.salvar('abertura_7')

def quadro_8():
    """O final: o Grumete devolve o baú ao Deus da Riqueza, e a maldição roxa se quebra em pedacinhos."""
    q = Quadro()
    q.ceu(0, H, [(80, 50, 110), (190, 120, 100), (255, 200, 120), (255, 236, 180)])
    for dx in (-40, 0, 40):
        q.raio_de_luz(150 + dx * 0.2, 6, 150 + dx * 1.5, 24, 0, 120, (255, 248, 210), 0.5)
    q.nuvem(170, 100, 50, (255, 240, 210), (246, 210, 170), 24)
    q.brilho(170, 40, 36, (255, 250, 220), 0.8)
    q.colar(deus(2), 146, 18)
    q.colar(bau(1), 108, 70)
    q.brilho(122, 80, 16, (255, 220, 110), 0.6)
    # o chão e o Grumete
    q.retangulo(0, 128, W, H, (240, 206, 140))
    q.ceu(128, H, [(246, 214, 150), (214, 170, 110)])
    q.colar_no_chao(grumete(2), 76, 132)
    # os cacos roxos da maldição voando para longe
    rnd = random.Random(25)
    for _ in range(26):
        x, y = 76 + rnd.uniform(-30, 30), 112 + rnd.uniform(-34, 10)
        for i in range(3):
            q.ponto(x + i, y - i * 0.5, (176, 96, 230))
        q.ponto(x + 1, y, (238, 214, 255))
    q.salvar('final_1')

def quadro_9():
    """O mundo de volta ao normal: dia de sol na praia, todo mundo amigo de novo."""
    q = Quadro()
    horizonte = 88
    q.ceu(0, horizonte, [(70, 150, 240), (110, 180, 250), (170, 214, 255)])
    q.circulo(214, 24, 10, (255, 246, 190))
    q.brilho(214, 24, 20, (255, 246, 190), 0.5)
    q.nuvem(60, 26, 26, (255, 255, 255), (214, 228, 250), 26)
    q.nuvem(150, 38, 22, (255, 255, 255), (214, 228, 250), 27)
    q.morro(150, 240, horizonte, 22, (60, 150, 110), 28, 3)
    q.palmeira(168, horizonte - 10, 14, (40, 110, 70))
    q.mar(horizonte, [(60, 140, 220), (40, 110, 200), (30, 90, 170)], (140, 200, 255), (255, 255, 255), 29)
    areia = 116
    q.retangulo(0, areia, W, H, (246, 220, 160))
    q.ceu(areia, H, [(250, 226, 170), (230, 196, 130)])
    for x in range(0, W, 2):  # a beirinha da água
        q.ponto(x, areia + int(math.sin(x / 6)), (255, 255, 255))
    # a turma: Marina na água, Capitã e Cozinheira de bem, Dóris, o Louro e o Grumete
    q.colar(personagem('Marina', 1), 26, areia - 30)
    q.colar_no_chao(capita(False, 1), 64, areia + 14)
    q.colar_no_chao(cozinheira(False, 1), 92, areia + 14)
    q.colar_no_chao(grumete(2), 128, areia + 16)
    q.colar(louro(1, True), 146, areia - 26)
    q.colar_no_chao(personagem('Doris', 1), 176, areia + 10, 0.85)
    for x in (200, 214, 228):
        q.colar(moeda(), x, areia + 6)
    q.salvar('final_2')

if __name__ == '__main__':
    for f in (quadro_1, quadro_2, quadro_3, quadro_4, quadro_5, quadro_6, quadro_7, quadro_8, quadro_9):
        f()
