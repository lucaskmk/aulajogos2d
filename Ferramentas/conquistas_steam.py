"""Lista as conquistas do jogo para cadastrar no Steamworks (Stats & Achievements > Achievements).

O id de cada conquista na Steam (o "API Name") TEM que ser igual ao id de Conquistas.cs: é ele que o jogo manda
(Steam.cs). O nome e a descrição saem em português e em inglês (a tradução vem de Traducoes.cs).

Uso:  python Ferramentas/conquistas_steam.py            (tabela na tela)
      python Ferramentas/conquistas_steam.py --csv x.csv (planilha para ir copiando)
"""
import csv
import io
import os
import re
import sys

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def ler(caminho):
    with io.open(os.path.join(RAIZ, caminho), encoding="utf-8") as f:
        return f.read()


def texto_cs(s):
    return s.replace('\\"', '"')


def main():
    conquistas = re.findall(r'new Conquista\("([^"]+)",\s*"((?:[^"\\]|\\.)*)",\s*"((?:[^"\\]|\\.)*)"\)', ler("Assets/Scripts/Nucleo/Conquistas.cs"))
    traducoes = dict(re.findall(r'\{\s*"((?:[^"\\]|\\.)*)",\s*"((?:[^"\\]|\\.)*)"\s*\}', ler("Assets/Scripts/Nucleo/Traducoes.cs")))
    linhas = []
    for id_, nome, descricao in conquistas:
        nome, descricao = texto_cs(nome), texto_cs(descricao)
        linhas.append((id_, nome, descricao, texto_cs(traducoes.get(nome, "")), texto_cs(traducoes.get(descricao, ""))))

    if len(sys.argv) > 2 and sys.argv[1] == "--csv":
        with io.open(sys.argv[2], "w", encoding="utf-8-sig", newline="") as f:
            escritor = csv.writer(f)
            escritor.writerow(["API Name", "Nome (PT)", "Descrição (PT)", "Name (EN)", "Description (EN)"])
            escritor.writerows(linhas)
        print(f"{len(linhas)} conquistas em {sys.argv[2]}")
        return

    sys.stdout.reconfigure(encoding="utf-8")
    for id_, nome, descricao, nome_en, descricao_en in linhas:
        print(f"{id_:24} {nome}  |  {nome_en or '(sem tradução)'}")
        print(f"{'':24} {descricao}  |  {descricao_en or '(sem tradução)'}")
    print(f"\n{len(linhas)} conquistas. Ícones: 256x256 (desbloqueada e bloqueada, em cinza) para cada uma.")


if __name__ == "__main__":
    main()
