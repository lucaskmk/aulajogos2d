#!/usr/bin/env python3
"""Checa se uma fase do Re:CILADA! (mapa em texto, 15 linhas) dá para passar.

Simula a física do Jogador.cs (velocidade 7, aceleração 80, pulo 14, gravidade 9.81*3.5,
queda máxima 20, caixa 0.72 x 0.96) com passos de 0.02 s, explorando pulos e caminhadas a partir
de cada posição em que o Subaru pode ficar parado. Diz se algum alvo (G, L, ou o '*' da bandeira
fujona) é alcançável a partir do P sem encostar em células mortais.

Uso:
  python3 checar_fase.py arquivo.txt [--solido "ab"] [--mortal "xy"] [--alvo "G"] [--de "s"]
O arquivo tem as 15 linhas do mapa (pode ser um .json com {"mapa": [...]}).
--solido: caracteres extras que são chão firme (ex.: plataformas novas)
--mortal: caracteres extras que matam ao encostar
--de: começa a busca também a partir destes caracteres (ex.: 's' = checkpoint) -> testa cada trecho

Simplificações: inimigos, serras, esmagadores, espinhos que caem e coisas que se mexem são ignorados
(o teste só garante que o TERRENO deixa passar). 'C' (cai) e 'M' (foge) contam como chão, 'F' (falso)
e 'I' (invisível) contam como vazio. Molas 'S' lançam com força 22. Portas não teletransportam.
"""
import json, sys, math, argparse
from collections import deque

G = 9.81 * 3.5
VEL, ACEL, PULO, QUEDA, MOLA = 7.0, 80.0, 14.0, 20.0, 22.0
DT = 0.02
MW, MH = 0.36, 0.48  # meia largura / meia altura da caixa do jogador

SOLIDO = set('#BC?KM')
MORTAL = set('^hZom')
ALVOS = set('GL*')


def carregar(caminho):
    txt = open(caminho, encoding='utf-8').read()
    if caminho.endswith('.json'):
        d = json.loads(txt)
        return d['mapa'] if isinstance(d, dict) else d
    return txt.split('\n')[:15]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('arquivo')
    ap.add_argument('--solido', default='')
    ap.add_argument('--mortal', default='')
    ap.add_argument('--alvo', default='')
    ap.add_argument('--de', default='')
    a = ap.parse_args()
    mapa = carregar(a.arquivo)
    resultado = checar(mapa, a.solido, a.mortal, a.alvo, a.de)
    print(resultado['texto'])
    sys.exit(0 if resultado['ok'] else 1)


def checar(mapa, solido_extra='', mortal_extra='', alvo_extra='', de_extra=''):
    linhas = list(mapa)
    msgs = []
    if len(linhas) != 15:
        msgs.append(f'AVISO: o mapa tem {len(linhas)} linhas (o jogo espera 15).')
    H = len(linhas)
    W = max(len(l) for l in linhas)
    grade = [l.ljust(W) for l in linhas]
    solidos = SOLIDO | set(solido_extra)
    mortais = MORTAL | set(mortal_extra)
    alvos = ALVOS | set(alvo_extra)

    def cel(x, r):
        if r < 0 or r >= H or x < 0 or x >= W:
            return ' '
        return grade[r][x]

    solid = [[False] * W for _ in range(H)]
    for r in range(H):
        for x in range(W):
            if grade[r][x] in solidos:
                solid[r][x] = True
    # cano 'U': 2 de largura, desce até o chão
    for r in range(H):
        for x in range(W):
            if grade[r][x] == 'U':
                rr = r
                while rr < H and not (cel(x, rr) in '#CF' and rr != r):
                    for dx in (0, 1):
                        if x + dx < W:
                            solid[rr][x + dx] = True
                    rr += 1
    # bordas: paredes invisíveis em x=-1 e x=W
    def eh_solido(x, r):
        if x < 0 or x >= W:
            return True
        if r < 0 or r >= H:
            return False
        return solid[r][x]

    def wy(r):  # y do centro da célula da linha r
        return H - 1 - r

    def linha_de(y):
        return int(math.floor(H - 1 - y + 0.5))

    def col_de(x):
        return int(math.floor(x + 0.5))

    def colide(px, py):
        x0, x1 = col_de(px - MW + 1e-4), col_de(px + MW - 1e-4)
        r0, r1 = linha_de(py + MH - 1e-4), linha_de(py - MH + 1e-4)
        for r in range(r0, r1 + 1):
            for x in range(x0, x1 + 1):
                if eh_solido(x, r):
                    return True
        return False

    def toca(px, py, conjunto, encolhe=0.15):
        x0, x1 = col_de(px - MW + encolhe), col_de(px + MW - encolhe)
        r0, r1 = linha_de(py + MH - encolhe), linha_de(py - MH + encolhe)
        for r in range(r0, r1 + 1):
            for x in range(x0, x1 + 1):
                if 0 <= x < W and 0 <= r < H and grade[r][x] in conjunto:
                    return True
        return False

    def no_chao(px, py):
        return colide(px, py - 0.03)

    def sobre_mola(px, py):
        r = linha_de(py)
        for x in (col_de(px - MW + 0.1), col_de(px + MW - 0.1)):
            if 0 <= r < H and 0 <= x < W and grade[r][x] == 'S':
                return True
        return False

    def mover(px, py, vx, vy):
        nx = px + vx * DT
        if colide(nx, py):
            # encosta na parede
            passo = 0.01 if vx > 0 else -0.01
            nx = px
            while not colide(nx + passo, py) and abs(nx - px) < abs(vx * DT):
                nx += passo
            vx = 0.0
        ny = py + vy * DT
        if colide(nx, ny):
            passo = 0.01 if vy > 0 else -0.01
            ny = py
            while not colide(nx, ny + passo) and abs(ny - py) < abs(vy * DT):
                ny += passo
            vy = 0.0
        return nx, ny, vx, vy

    # (direção, segurar pulo em segundos (None = não pula), direção depois do ápice)
    ACOES = []
    for d in (-1, 0, 1):
        for segura in (None, 0.04, 0.12, 0.2, 9.0):
            for d2 in ({d, 0} if segura is not None else {d}):
                ACOES.append((d, segura, d2))
    ACOES.append((1, None, 1))

    def simular(px, py, vx, acao):
        d, segura, d2 = acao
        vy = 0.0
        t = 0.0
        pulou = False
        pulando = False
        ok_chao_antes = True
        trajeto = 0
        while t < 4.0:
            alvo_vx = (d if (not pulou or vy > 0) else d2) * VEL
            vx = max(min(vx + ACEL * DT, alvo_vx), vx - ACEL * DT) if vx != alvo_vx else vx
            chao = no_chao(px, py)
            if vy <= 0.1 and t > 0 and toca(px, py, 'S', encolhe=0.1):
                vy = MOLA
                pulando = False
                pulou = True
                chao = False
            if segura is not None and not pulou and chao:
                vy = PULO
                pulou = True
                pulando = True
            if pulando and t > (segura if segura is not None else 0) and vy > 0:
                vy *= 0.5
                pulando = False
            if vy <= 0:
                pulando = False
            if not chao or vy > 0:
                vy -= G * DT
                if vy < -QUEDA:
                    vy = -QUEDA
            px, py, vx, vy = mover(px, py, vx, vy)
            t += DT
            trajeto += 1
            if py < -2:
                return ('morreu', None)
            if toca(px, py, mortais):
                return ('morreu', None)
            if toca(px, py, alvos, encolhe=0.0):
                return ('alvo', (px, py))
            if no_chao(px, py) and vy <= 0 and (pulou or segura is None) and trajeto > 3:
                if segura is None:
                    # caminhando: para depois de andar ~0.6 bloco
                    if t >= 0.12 or not ok_chao_antes:
                        return ('parou', (px, py))
                else:
                    return ('parou', (px, py))
            if segura is None and not no_chao(px, py):
                ok_chao_antes = False
        return ('parou', (px, py))

    def chave(px, py):
        return (round(px * 2), round(py * 2))

    inicios = []
    for r in range(H):
        for x in range(W):
            c = grade[r][x]
            if c == 'P' or (de_extra and c in de_extra):
                inicios.append((c, x, r))
    if not any(c == 'P' for c, _, _ in inicios):
        return {'ok': False, 'texto': 'ERRO: não tem P no mapa.'}

    def busca(x, r):
        px, py = float(x), float(wy(r))
        # assenta no chão
        for _ in range(400):
            if no_chao(px, py) or py < -2:
                break
            py -= 0.02
        visto = set()
        fila = deque([(px, py)])
        visto.add(chave(px, py))
        mais_longe = px
        while fila:
            px, py = fila.popleft()
            mais_longe = max(mais_longe, px)
            for acao in ACOES:
                for vx0 in (0.0, acao[0] * VEL):
                    res, pos = simular(px, py, vx0, acao)
                    if res == 'alvo':
                        return True, pos[0]
                    if res == 'parou':
                        k = chave(*pos)
                        if k not in visto:
                            visto.add(k)
                            fila.append(pos)
            if len(visto) > 6000:
                break
        return False, mais_longe

    ok_total = True
    for c, x, r in inicios:
        ok, longe = busca(x, r)
        nome = 'P (início)' if c == 'P' else f"'{c}' na coluna {x}"
        if ok:
            msgs.append(f'OK: de {nome} dá para chegar ao alvo.')
        else:
            ok_total = False
            msgs.append(f'FALHOU: de {nome} não achei caminho até G/L/*. Mais longe que cheguei: x = {longe:.1f} (largura {W}).')
    return {'ok': ok_total, 'texto': '\n'.join(msgs)}


if __name__ == '__main__':
    main()
