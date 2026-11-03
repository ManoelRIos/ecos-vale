# Arquitetura de Ecos da Meia-Noite

A única cena é `Assets/_Game/Scenes/Haunt.unity`. Ela contém o componente `HauntGame`, que constrói os objetos visuais e controla a incursão local. A assembly de runtime é `Haunt.Runtime`; as ferramentas e os testes do Editor usam `Haunt.Editor` e `Haunt.Tests`.

## Núcleo independente do Unity

`DungeonFloor` gera uma árvore de salas em uma grade. A mesma semente e o mesmo número de andar reproduzem o mapa e o arquétipo do guardião. As portas são cardinais, as conexões são recíprocas e o chefe ocupa uma folha de distância máxima da entrada. Apenas a sala atual é instanciada no Unity. Cada arena tem limites ±14,5 por ±8,5 unidades e paredes em ±15/±9. A área jogável é 29 × 17 unidades. A câmera ortográfica de tamanho 6,3 acompanha o jogador com interpolação e limites calculados para a proporção da janela. Ao entrar em uma sala, sua posição é ajustada imediatamente ao ponto de chegada; ela mostra uma parte da arena por vez.

`RunState` mantém vida, moedas, dano, cadência, personagem, recarga do poder, arsenal adquirido, arma equipada, vitória sobre o chefe e compras da visita atual. Cada instância tem sua própria seleção e arsenal; a simulação Unity ainda controla um único jogador. As compras são atômicas: validar e cobrar acontecem antes de aplicar a melhoria. A passagem de andar exige chefe vencido e uma incursão viva; ela reinicia o estoque da loja e preserva as melhorias.

`CombatGeometry` calcula a primeira interseção de um segmento com círculos e caixas. O runtime compara as frações de contato para que um alvo antes de uma coluna receba o disparo e um alvo depois dela fique protegido. As verificações de `Tools/HauntChecks.csproj` compilam esses mesmos arquivos de produção.

## Simulação e apresentação

`HauntGame` possui o jogador, inimigos, projéteis, obstáculos, efeitos e estado das salas. O movimento usa passos pequenos e deslizamento por eixos. A esquiva bloqueia dano durante sua duração; o dano recebido também concede uma breve proteção. Projéteis hostis deixam de causar dano quando a sala é purificada.

O fluxo é: menu → entrada → salas de combate → chefe → Mercado das Almas → próximo andar. A loja é uma área separada e segura. Sua interface modal substitui o HUD e organiza o estoque em duas abas; o jogador não se move nem compra com E atrás do painel. O botão Descer avança para o próximo andar. Morrer oferece reinício com a mesma semente ou outra; não há persistência de incursões entre sessões.

`HauntGame.Visuals.cs` reúne a construção visual das salas, profundidade por posição, sprites animados, marcadores, partículas e luzes de velas/portas. Os atores usam animações em cache; as partículas reutilizam até 320 objetos ativos. O deslocamento visual vertical de atores e projéteis não altera suas coordenadas de colisão.

`HauntGame.UI.cs` contém o menu, HUD, mapa, loja, pausa e tela de morte em IMGUI. O Input System lê teclado e mouse. `HauntPixelArt` gera e mantém sprites e texturas com filtro de ponto e 16 pixels por unidade. O material `HauntSprites` usa um shader URP sem iluminação, com `unity_SpriteProps`/`unity_SpriteColor` conforme o shader oficial do URP 17. A biblioteca fornece `Frame(id, frame)` com quatro quadros por ciclo, incluindo o candelabro, e quatro variantes de piso para cada um dos cinco temas. O shader mantém mistura alfa padrão para atores e permite uma instância `GlowMaterial` com mistura aditiva `SrcAlpha/One` e emissão 2,2 para os elementos luminosos. Esses recursos pertencem à instância e são liberados ao encerrá-la.

## Preparação do projeto

`HauntSetup` prepara apenas o pipeline `HauntURP`, o renderer `HauntRenderer`, as configurações gráficas e a cena habilitada no build. Os recursos URP são criados no Editor com as APIs do pacote. `HauntRendering` seleciona o pipeline preparado antes da construção da apresentação.

`HauntProject` abre a cena somente com o Play parado e aguarda importação/compilação quando chamado pelo atalho. O comando de smoke confere a cena, os recursos e a geração inicial, sem executar combate ou validar imagens.

## Limites

O protótipo usa quatro armas, quatro personagens com poderes diferentes, sete inimigos e cinco padrões de chefe. Os cinco chefes possuem identidades visuais: a abóbora original, a Viúva, o Bispo, a Matriarca e o Sino. Cada arquétipo de chefe alterna rajadas e, conforme seu padrão, carga ou ataque marcado no chão. Cada tema oferece três arquétipos possíveis em `DungeonFloor.BossCandidatesForTheme`; o sorteio usa um gerador independente da geração do mapa. Tema e guardião são estados separados: a composição de inimigos comuns e a arquitetura seguem `ThemeIndex`, enquanto arte e ataques do chefe seguem `BossArchetype`. O combate, a arte, as animações e o áudio ainda precisam de refinamento. Não há multiplayer, serviços externos ou gravação de incursões. As verificações fora do Editor não certificam a importação, os shaders ou a execução no Unity.

## Evolução para coop

O estado de personagem, arsenal e recarga está no núcleo e pode ser instanciado por jogador. Ainda será necessário extrair posição, entrada, mira e colisões do jogador em entidades independentes, definir autoridade da simulação e sincronizar geração, projéteis, compras e transições. Não há transporte de rede ou coop local/online nesta revisão.

## Apresentação desta revisão

Cada tema usa os IDs `floor-themeN-v0..v3`, `wall-themeN`, `wall-reliefN`, `decor-themeN` e `map-themeN`. Um hash da semente da sala e das coordenadas do ladrilho escolhe variante, tom, espelhamento e rotação de 90° dos pisos sem avançar o gerador de inimigos. Relevos com sombras e candelabros suspensos com quatro quadros dão volume às paredes. O atlas desenha corredores e miniaturas, com indicadores de entrada, guardião e sala atual. Efeitos ambientes usam o tempo de simulação e não consomem o gerador aleatório de inimigos.

Projéteis guardam cor, temporizador de rastro e um halo filho com `GlowMaterial`. O halo acompanha o projétil, recebe escala própria e é ocultado/reativado junto com ele pelo pool; novos disparos reaproveitam o mesmo par de objetos. Impactos combinam anel, clarão e estilhaços direcionais; o canhão tem efeito maior. Avisos de chão são uma lista limitada a 16 marcadores, com 1,1 s de preparo, limpeza ao sair da sala e cancelamento ao purificar. A pausa congela esses tempos. Chamas, brilhos e efeitos luminosos usam sprites aditivos; atores e demais efeitos mantêm o material adequado ao seu desenho. Esses brilhos não são luzes físicas dinâmicas e não alteram colisões. `GlowMaterial` pertence à instância de `HauntGame` e é destruído em `OnDestroy`, assim como a biblioteca de arte libera suas texturas e material.

O PNG de prévia usa os pixels produzidos pelas rotinas Canvas do próprio código C#, fora do Editor, sem simular material/câmera/URP. Não certifica a aparência no jogo.

## Orientação de saída

`TryNearestExit` só oferece destino em uma sala purificada, sem inimigos e durante uma incursão viva. O destino é a porta de menor distância ao jogador; após vencer o guardião, o portal do mercado tem prioridade. A interface desenha a seta na direção desse destino, ajudando a explorar a arena com a câmera próxima. Pausa, loja e combate não exibem uma indicação de saída aberta.

## Catálogo de guardiões

Os pools de `DungeonFloor.BossCandidatesForTheme` são: Jardins `[Rei, Viúva, Matriarca]`, Galeria `[Viúva, Bispo, Sino]`, Catedral `[Bispo, Sino, Rei]`, Ninho `[Matriarca, Viúva, Rei]` e Abismo `[Sino, Matriarca, Bispo]`. O método retorna uma cópia para impedir mutação do catálogo compartilhado. O gerador de encontros usa semente e andar com sal próprio; suas chamadas não alteram os mapas determinísticos existentes. Os cinco desenhos de chefes são preservados e podem aparecer em mais de um tema.

## HUD e atlas transparentes

O HUD de combate usa texto com uma sombra nos próprios caracteres, sem tarjas, painéis ou barras de fundo. Os corações ficam no canto superior esquerdo; os demais dados são compactos, e os avisos da sala somem após a entrada. `WorldTag`, notificações e legenda de saída também são textos sem caixas. Menus e loja continuam com controles próprios.

O atlas é desenhado antes do HUD, sem painel. Corredores, miniaturas e retratos recebem alfa de 0,16 no mapa pequeno e 0,30 no ampliado; só os marcadores da sala atual recebem mais 0,10. Os textos e o personagem permanecem acima da sobreposição. `PointerOverInterface` bloqueia apenas menu, loja, pausa e morte: HUD e ambas as visões do mapa não capturam cursor, mira ou tiro. A seta de saída continua disponível com o mapa ampliado.
