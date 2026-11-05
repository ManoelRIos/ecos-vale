# Spritesheets e importação de personagens

O jogo usa dois conjuntos de arte: as folhas nativas dos quatro personagens, baseadas no arquivo original enviado pelo usuário, e as folhas procedurais de mobs, bosses e objetos. Os protótipos antigos dos jogadores continuam disponíveis para fallback e exportação.

## Personagens ativos

[CharacterSheets](../Assets/_Game/Resources/CharacterSheets) contém `pilot.png`, `medium.png`, `reaper.png`, `alchemist.png` e `catalog.json`. O primeiro personagem usa os pixels do Pilot original; as outras três folhas mantêm as mesmas poses, direções e índices, com paletas e acessórios próprios.

O PNG fonte fica intacto em [PilotOriginal.png](../Assets/_Game/ArtSources/PilotOriginal.png). O importador recorta os quadros sem ampliar, filtrar, repintar ou suavizar o Pilot. Apenas as cores de fundo explicitamente catalogadas se tornam transparentes na folha derivada. Os detalhes da fonte e a prévia dos quatro personagens estão em [CHARACTER_MODEL.md](CHARACTER_MODEL.md).

As folhas usam **16 pixels por unidade**. A célula de organização tem 48×48, mas contém o desenho em sua resolução nativa, com preenchimento transparente. Cada frame informa seu retângulo de renderização e pivô nos pés; célula e retângulo não obrigam a arte a ocupar 48 pixels.

`catalog.json` identifica clip, direção, postura da arma, traje alternativo, frames, FPS e repetição. As quantidades variam: por exemplo, o Idle frontal original tem seis quadros e o lateral tem quatro; Walking tem seis quadros nas direções presentes. A biblioteca consulta os dados em vez de reduzir todas as ações a quatro poses.

As posturas são `Empty`, `OneHanded` e `TwoHanded`. A fonte tem até cinco vistas: `Front`, `FrontRight`, `Right`, `BackRight` e `Back`; a lateral esquerda usa espelhamento. Algumas ações têm quatro vistas ou somente uma. Quando falta uma vista ou postura, o carregador escolhe uma pose existente da fonte.

O jogo seleciona as poses conforme movimento, mira, arma, evasão e morte. Disparo, recarga e poder usam a postura original apropriada com o feedback da arma e dos efeitos, pois a prancha não fornece clips corporais próprios para todas essas ações. Poses de mesa, baú, voo, fantasma, reviver e outras ações estão na biblioteca; sua presença não cria novas mecânicas ou multiplayer.

![Modelo original e variantes](CHARACTER_MODEL.png)

## Folhas procedurais

[ReferenceSheets](../Assets/_Game/Resources/ReferenceSheets) conserva **24 PNGs RGBA transparentes e 1.700 células**, com filtro Point, sem mipmaps, sem compressão e sem redimensionamento NPOT. Esse conjunto usa **32 pixels por unidade**.

| Pasta | Folhas | Célula | Grade | Uso |
| --- | ---: | --- | --- | --- |
| actors | 14 | 48×48 | 4 colunas × 27 linhas | Seis mobs e protótipos de quatro heróis, com equipamento e apenas corpo |
| bosses | 7 | 96×96 | 4 × 5 | Repouso, preparação, ataque, fúria e morte |
| props | 3 | 32×32 | 4 × 4 | Barril explosivo, barril de ácido e baú decrépito |

Essas folhas receberam detalhes na grade maior: costuras, dobras, fivelas, juntas, dentes, rebites, lâminas e superfícies. Resolução e PPU dobrados preservam células de 1,5 unidade para esses atores, 3 para bosses e 1 para esses objetos. Os jogadores ativos de `CharacterSheets` seguem o arquivo nativo, em vez dessa grade de protótipo.

O `manifest.json` de `ReferenceSheets` registra retângulos com origem superior esquerda em `rect` e origem inferior esquerda em `unityRect`. As quatro colunas são frames 0–3. Atores procedurais usam nove estados, com três linhas de direção por estado; o pivô é `(24,4)` pixels Unity. Bosses usam uma linha por `Idle`, `Telegraph`, `Attack`, `Enrage` e `Death`, com pivô central.

Os objetos têm quatro estágios animados: explosivo intacto/ignição/explosão/fumaça; ácido intacto/rachadura/vazamento/poça; baú fechado/abrindo/aberto/vazio. Barris usam esses estágios no dano, fusível e destruição. A folha do baú fica preparada para uma futura interação.

A [prévia procedural](REFERENCE_SPRITESHEETS.png) inclui os protótipos anteriores dos jogadores; a aparência atual deles está na prévia do modelo original acima. As montagens mostram pixels dos arquivos, sem renderização Unity.

## Armas, relíquias e decoração

As 17 armas recebem resolução dobrada e detalhes próprios de metal, madeira, empunhaduras, canos, mecanismos e partes luminosas. Relíquias, urna de água, caixa de munição, lápides, grades, velas ossuárias, decoração dos cinco temas e atendente recebem o mesmo tratamento. São gerados por `HauntPixelArt.Equipment.cs`, em 32 PPU, separados das folhas nativas e das 24 folhas procedurais. Veja [EQUIPMENT_DETAIL.png](EQUIPMENT_DETAIL.png).

Projéteis conservam 16 PPU para manter a leitura dos núcleos, rastros e halos. A grade visual usa 32 passos por unidade, como a câmera. Colisões e posições físicas continuam no plano do chão.

## Reproduzir e verificar

Para importar a fonte original e suas variantes, com Python e Pillow:

```bash
python3 -m pip install -r Tools/requirements-sprite-import.txt
python3 Tools/import_pilot_sheet.py
python3 Tools/import_pilot_sheet.py --check
```

O mapa de recortes é `Tools/Pilot/source-clips.json`; o PNG original permanece em `Assets/_Game/ArtSources`. O importador verifica a identidade do arquivo e os pixels copiados para o Pilot, e gera quatro folhas, catálogo, metadados e prévia.

Para os recursos procedurais, com Python e .NET 8:

```bash
python3 Tools/export_sprite_sheets.py
python3 Tools/export_sprite_sheets.py --check
```

Os comandos sem `--check` escrevem os resultados. Os comandos com `--check` comparam sem alterar arquivos. Alterações manuais nas folhas derivadas podem ser substituídas pela próxima geração; mantenha a fonte e o mapa de recortes para reproduzir o resultado.

Essas ferramentas não executam Unity. Importação no motor, animações renderizadas, áudio e os testes Editor ainda precisam de Unity 6000.0.85f1; consulte [VALIDATION.md](VALIDATION.md).
