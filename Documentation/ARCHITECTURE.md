# Arquitetura de Ecos da Meia-Noite

A única cena é `Assets/_Game/Scenes/Haunt.unity`. Ela contém o componente `HauntGame`, que constrói os objetos visuais e controla a incursão local. A assembly de runtime é `Haunt.Runtime`; as ferramentas e os testes do Editor usam `Haunt.Editor` e `Haunt.Tests`.

## Núcleo independente do Unity

`DungeonFloor` gera uma árvore de salas em uma grade. A mesma semente e o mesmo número de andar reproduzem o mapa. As portas são cardinais, as conexões são recíprocas e o chefe ocupa uma folha de distância máxima da entrada. Apenas a sala atual é instanciada no Unity.

`RunState` mantém vida, moedas, dano, cadência, personagem, recarga do poder, arsenal adquirido, arma equipada, vitória sobre o chefe e compras da visita atual. Cada instância tem sua própria seleção e arsenal; a simulação Unity ainda controla um único jogador. As compras são atômicas: validar e cobrar acontecem antes de aplicar a melhoria. A passagem de andar exige chefe vencido e uma incursão viva; ela reinicia o estoque da loja e preserva as melhorias.

`CombatGeometry` calcula a primeira interseção de um segmento com círculos e caixas. O runtime compara as frações de contato para que um alvo antes de uma coluna receba o disparo e um alvo depois dela fique protegido. As verificações de `Tools/HauntChecks.csproj` compilam esses mesmos arquivos de produção.

## Simulação e apresentação

`HauntGame` possui o jogador, inimigos, projéteis, obstáculos, efeitos e estado das salas. O movimento usa passos pequenos e deslizamento por eixos. A esquiva bloqueia dano durante sua duração; o dano recebido também concede uma breve proteção. Projéteis hostis deixam de causar dano quando a sala é purificada.

O fluxo é: menu → entrada → salas de combate → chefe → Mercado das Almas → próximo andar. A loja é uma área separada e segura. Morrer oferece reinício com a mesma semente ou outra; não há persistência de incursões entre sessões.

`HauntGame.Visuals.cs` reúne a construção visual das salas, profundidade por posição, sprites animados, marcadores, partículas e luzes de velas/portas. Os atores usam animações em cache; as partículas reutilizam até 240 objetos ativos. O deslocamento visual vertical de atores e projéteis não altera suas coordenadas de colisão.

`HauntGame.UI.cs` contém o menu, HUD, mapa, loja, pausa e tela de morte em IMGUI. O Input System lê teclado e mouse. `HauntPixelArt` gera e mantém sprites e texturas com filtro de ponto e 16 pixels por unidade. O material `HauntSprites` usa um shader URP sem iluminação, com `unity_SpriteProps`/`unity_SpriteColor` conforme o shader oficial do URP 17. A biblioteca fornece `Frame(id, frame)` com quatro quadros por ciclo e seis tipos de piso. Esses recursos pertencem à instância e são liberados ao encerrá-la.

## Preparação do projeto

`HauntSetup` prepara apenas o pipeline `HauntURP`, o renderer `HauntRenderer`, as configurações gráficas e a cena habilitada no build. Os recursos URP são criados no Editor com as APIs do pacote. `HauntRendering` seleciona o pipeline preparado antes da construção da apresentação.

`HauntProject` abre a cena somente com o Play parado e aguarda importação/compilação quando chamado pelo atalho. O comando de smoke confere a cena, os recursos e a geração inicial, sem executar combate ou validar imagens.

## Limites

O protótipo usa quatro armas, dois personagens com poderes diferentes, sete inimigos e cinco padrões de chefe. Os dois novos chefes e três novas criaturas possuem sprites próprios. O combate, a arte, as animações e o áudio ainda precisam de refinamento. Não há multiplayer, serviços externos ou gravação de incursões. As verificações fora do Editor não certificam a importação, os shaders ou a execução no Unity.

## Evolução para coop

O estado de personagem, arsenal e recarga está no núcleo e pode ser instanciado por jogador. Ainda será necessário extrair posição, entrada, mira e colisões do jogador em entidades independentes, definir autoridade da simulação e sincronizar geração, projéteis, compras e transições. Não há transporte de rede ou coop local/online nesta revisão.
