#!/usr/bin/env python3
"""Gera o icone do jogo (Assets/Icone/icone.png, 256 x 256) a partir do desenho do Grumete.

Le o desenho ("Grumete") e a paleta direto de Assets/Scripts/Nucleo/ArteDosPersonagens.cs: mudou o
personagem? Rode de novo:  python Ferramentas/gerar_icone.py
O icone e o Grumete (16 x 16, ampliado 12x, sem borrar) num fundo de mar com a moldura dourada do jogo.
"""
import io, os, re
from PIL import Image

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
fonte = io.open(os.path.join(RAIZ, 'Assets/Scripts/Nucleo/ArteDosPersonagens.cs'), encoding='utf-8').read()

paleta = {m.group(1): tuple(int(v) for v in m.group(2, 3, 4)) for m in
          re.finditer(r"\{ '(.)', new Color32\((\d+), (\d+), (\d+), 255\) \}", fonte)}
inicio = fonte.index('static readonly string[] Grumete =')
linhas = re.findall(r'"([^"]{16})"', fonte[inicio:fonte.index('};', inicio)])
assert len(linhas) == 16, len(linhas)

LADO, ESCALA = 256, 12
MAR_CLARO, MAR_ESCURO, MOLDURA, FILETE, ESCURO = (66, 150, 214), (30, 82, 150), (70, 42, 26), (226, 176, 78), (22, 18, 32)
icone = Image.new('RGBA', (LADO, LADO), ESCURO + (255,))
px = icone.load()
for y in range(LADO):
    for x in range(LADO):
        borda = min(x, y, LADO - 1 - x, LADO - 1 - y)
        if borda < 6: cor = ESCURO
        elif borda < 16: cor = MOLDURA
        elif borda < 20: cor = FILETE
        else:
            t = (y - 20) / (LADO - 40)                       # o ceu em cima, o mar embaixo, em faixas (pixel art)
            faixa = int(t * 8) / 8
            cor = tuple(int(MAR_CLARO[i] + (MAR_ESCURO[i] - MAR_CLARO[i]) * faixa) for i in range(3))
        px[x, y] = cor + (255,)
x0, y0 = (LADO - 16 * ESCALA) // 2, (LADO - 16 * ESCALA) // 2 + 6
for ly, linha in enumerate(linhas):
    for lx, letra in enumerate(linha):
        if letra not in paleta: continue
        for dy in range(ESCALA):
            for dx in range(ESCALA):
                px[x0 + lx * ESCALA + dx, y0 + ly * ESCALA + dy] = paleta[letra] + (255,)
os.makedirs(os.path.join(RAIZ, 'Assets/Icone'), exist_ok=True)
icone.save(os.path.join(RAIZ, 'Assets/Icone/icone.png'))
print('icone gerado: Assets/Icone/icone.png')
