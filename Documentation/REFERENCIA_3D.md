# Personagens 2D para adaptação 3D

[TODOS_OS_PERSONAGENS.png](TODOS_OS_PERSONAGENS.png) reúne **30 entidades em um PNG de 3200 × 4118 pixels**: quatro jogadores, 17 mobs, sete chefes, o Coletor de Almas e o minichefe do roguelike 2D original. As quatro armas iniciais acompanham os jogadores.

Os pixels vêm dos arquivos e do gerador C# atuais. O exportador recorta apenas margens transparentes e amplia em múltiplos inteiros com filtro de ponto. Não redesenha, suaviza ou inventa detalhes dos personagens.

- **P01–P04:** frente, lado e costas dos atlas nativos; a arma aparece separada.
- **M01–M17:** poses existentes. Os personagens sem vistas de costas não recebem uma vista inventada.
- **B01–B07:** repouso, ataque e fúria do mesmo chefe.
- **X01–X02:** NPC e minichefe presentes na versão 2D original.

Os cartões são ampliados separadamente para leitura, portanto não indicam a escala física relativa. [TODOS_OS_PERSONAGENS.json](TODOS_OS_PERSONAGENS.json) registra as fontes, os recortes, o zoom, os pixels por unidade e as assinaturas RGBA de cada pose.

## Orientação para a modelagem

Use o código do cartão para identificar cada asset. A pose de repouso define o desenho base; as demais mostram movimentos e efeitos. Preserve silhueta, paleta, proporções, roupas e equipamentos. Para personagens sem uma vista traseira existente, trate as costas como uma interpretação que precisa de revisão.

Uma instrução possível para o Gemini:

> Adapte o personagem do cartão P01 para um modelo 3D com visual cartoon e texturas pixeladas, mantendo a silhueta, as proporções, as cores e os acessórios da referência. Apresente frente, lado e costas. Separe personagem, arma e efeitos. Use os demais quadros como referência de animação, preservando a identidade do desenho. Não transforme partículas e halos em partes sólidas do corpo.

Para integração como personagem 3D no Unity, serão necessários arquivos de malha, como **FBX ou GLB**, com texturas e, para animação, rig. Imagens renderizadas podem orientar essa modelagem. Mantenha arma e acessórios separáveis e identifique os materiais por personagem.

## Reproduzir a exportação

Com .NET 8, Pillow e uma fonte Open Sans ou DejaVu Sans disponíveis:

```bash
python3 Tools/export_character_reference.py
```

`DOTNET_PATH` pode selecionar o SDK. O programa `Tools/CharacterReferenceExport` extrai os pixels do C# real; o Python também confere as poses contra os PNGs de referência versionados e recorta os jogadores diretamente de `CharacterSheets` usando seu catálogo. Esta operação gera a imagem e o manifesto, sem executar o Unity ou alterar os assets do jogo.
