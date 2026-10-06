import re, json, sys
src = open(sys.argv[1], encoding='utf-8').read()
blocos = re.findall(r'nome = "([^"]*)".*?mapa = new\[\]\s*\{(.*?)\n\s*\},', src, re.S)
for i, (nome, corpo) in enumerate(blocos):
    linhas = re.findall(r'^\s*"((?:[^"\\]|\\.)*)",?\s*$', corpo, re.M)
    json.dump({'nome': nome, 'mapa': linhas}, open(f'{sys.argv[2]}/fase_{i:02d}.json', 'w'), ensure_ascii=False)
    print(i, nome, len(linhas), max(len(l) for l in linhas))
