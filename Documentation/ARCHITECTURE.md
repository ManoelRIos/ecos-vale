# Arquitetura de Ecos da Meia-Noite

A única cena é `Assets/_Game/Scenes/Haunt.unity`. Ela contém o componente `HauntGame`, que constrói os objetos visuais e controla a incursão local. A assembly de runtime é `Haunt.Runtime`; as ferramentas e os testes do Editor usam `Haunt.Editor` e `Haunt.Tests`.

## Núcleo independente do Unity

`DungeonFloor` gera uma árvore de salas em uma grade. A mesma semente e o mesmo número de andar reproduzem o mapa e o arquétipo do guardião. As portas são cardinais, as conexões são recíprocas e o chefe ocupa uma folha de distância máxima da entrada. Apenas a sala atual é instanciada no Unity. Cada arena tem limites ±14,5 por ±8,5 unidades e paredes em ±15/±9. A área jogável é 29 × 17 unidades. A câmera ortográfica de tamanho 6,3 acompanha o jogador com interpolação e limites calculados para a proporção da janela. Ao entrar em uma sala, sua posição é ajustada imediatamente ao ponto de chegada; ela mostra uma parte da arena por vez.

`RoomCountForNumber` calcula `7 + 2 * min(max(1, andar) - 1, 10)`: 7 salas no primeiro andar e duas adicionais por andar, até 27. Cada `DungeonRoom` mantém `WavesCleared`, `TotalWaves` e `HasMiniBoss`; salas de combate exigem três ondas, entrada e chefe têm uma. Após escolher a sala do chefe, o gerador marca uma sala de combate para miniboss, ou duas a partir do quarto andar, sem repetir sala nem ocupar entrada/chefe. Essa seleção é determinística e usa o gerador de encontros separado. O crescimento dos andares altera intencionalmente os mapas da versão anterior; repetir semente e andar nesta versão mantém planta, guardião e salas com miniboss.

`RunState` mantém vida, moedas, dano, cadência, personagem, recarga do poder, arsenal adquirido, arma equipada, vitória sobre o chefe e compras da visita atual. Cada instância tem sua própria seleção e arsenal; a simulação Unity ainda controla um único jogador. As compras são atômicas: validar e cobrar acontecem antes de aplicar a melhoria. A passagem de andar exige chefe vencido e uma incursão viva; ela reinicia o estoque da loja e preserva as melhorias.

`StartingWeaponFor` associa Hunter/Medium/Reaper/Alchemist a HunterCarbine/VeilWand/BoneCaster/ArcCoil. O construtor concede e equipa a arma da alma escolhida; `StartingWeapon` permite voltar a ela com a tecla 1. O revólver continua no conjunto adquirido para compatibilidade do núcleo. `TryWeaponForItem` faz o mapeamento explícito dos nove itens de arma aceitos pelo núcleo; não há dependência de deslocamento aritmético entre os enums, pois as armas iniciais não são produtos da loja.

`CombatGeometry` calcula a primeira interseção de um segmento com círculos e caixas. O runtime compara as frações de contato para que um alvo antes de uma coluna receba o disparo e um alvo depois dela fique protegido. As verificações de `Tools/HauntChecks.csproj` compilam esses mesmos arquivos de produção.

## Simulação e apresentação

`HauntGame` possui o jogador, inimigos, projéteis, obstáculos, abismos, efeitos e estado das salas. O movimento usa passos pequenos e deslizamento por eixos. A esquiva bloqueia dano durante sua duração; o dano recebido também concede uma breve proteção. Projéteis hostis deixam de causar dano quando a sala é purificada.

`SpawnWave` cria grupos de inimigos em pontos afastados do jogador, da cobertura e dos abismos. Na terceira onda de uma sala com `HasMiniBoss`, também cria o inimigo de tipo 11. Ao eliminar uma onda, `CompleteRoom` incrementa `WavesCleared` e inicia a seguinte; somente a última marca `Cleared`, paga a recompensa e abre as portas. Cada inimigo começa com `SpawnRemaining = .9f`: uma fenda animada anuncia a emergência, enquanto IA, contato e recebimento de dano ficam suspensos. O chefe principal continua sendo um encontro único.

Os dez tipos comuns preservam abóbora, fantasma, cultista, morcego, larva, sentinela óssea e acólito do sino. Os tipos 8/9/10 acrescentam esqueleto que avança e atira, slime com saltos e rajada radial, e aranha que circunda e investe em linha. O tipo 4 identifica o chefe principal; o tipo 11 é um miniboss mais resistente, com leque de cinco tiros e aviso de ataque no chão. O tema favorece sua criatura característica sem excluir o restante do catálogo.

O fluxo é: menu → entrada → salas de combate → chefe → Mercado das Almas → próximo andar. A loja é uma sala segura e explorável. O atendente fica em `MerchantPosition = (0, 3.6)`, atrás de um balcão com colisão; seis vitrines exibem as armas e seus preços. Clicar no atendente ou pressionar E abre o catálogo apenas a até 2,8 unidades. As armas ocupam duas páginas de três itens, com relíquias em sua categoria. `shopMenuOpen` bloqueia movimento e recebe os cliques do catálogo; `InShop` sozinho não congela o jogador. Esc fecha o catálogo antes de pausar. `Buy` exige o catálogo aberto; avançar exige caminhar até o portal e pressionar E, com a conversa fechada. Morrer oferece reinício com a mesma semente ou outra; não há persistência de incursões entre sessões.

`HauntGame.Visuals.cs` reúne a construção visual das salas, profundidade por posição, sprites animados, marcadores, partículas e luzes de velas/portas. Os atores usam animações em cache; as partículas reutilizam até 320 objetos ativos. O deslocamento visual vertical de atores e projéteis não altera suas coordenadas de colisão.

Algumas salas de combate a partir do segundo andar, com `Id % 3 == 0`, recebem dois abismos retangulares de 2 × 1,2 unidades fora das rotas centrais de chegada. A esquiva existente dura 0,24 s a 12,5 unidades/s e permite atravessá-los; terminar sobre um abismo causa 1 de dano e retorna a `safeGround`, atualizado somente longe das bordas. Inimigos terrestres evitam os abismos; fantasma e morcego podem atravessá-los. Nos temas 1/3/4, seis manchas de neblina oscilam abaixo dos atores, com alfa do renderer em torno de 0,42. Sombras no chão e detalhes adicionais dos personagens reforçam a profundidade.

`HauntGame.Weapons.cs` concentra disparos, raios, colisões varridas e reciclagem dos projéteis na mesma classe parcial. `UpdateWeaponTrigger` mantém o raio visível seguindo a mira enquanto o botão é segurado, mas aplica dano somente nos pulsos de cadência. Soltar o botão, esquivar, abrir o catálogo ou interromper a incursão oculta o raio.

`HauntGame.UI.cs` contém o menu, HUD, mapa, loja, pausa e tela de morte em IMGUI. O Input System lê teclado e mouse. `HauntPixelArt` gera e mantém sprites e texturas com filtro de ponto e 16 pixels por unidade. O material `HauntSprites` usa um shader URP sem iluminação, com `unity_SpriteProps`/`unity_SpriteColor` conforme o shader oficial do URP 17. A biblioteca fornece `Frame(id, frame)` com quatro quadros por ciclo, incluindo o candelabro, e quatro variantes de piso para cada um dos cinco temas. O shader mantém mistura alfa padrão para atores e permite uma instância `GlowMaterial` com mistura aditiva `SrcAlpha/One` e emissão 2,2 para os elementos luminosos. Esses recursos pertencem à instância e são liberados ao encerrá-la.

## Preparação do projeto

`HauntSetup` prepara apenas o pipeline `HauntURP`, o renderer `HauntRenderer`, as configurações gráficas e a cena habilitada no build. Os recursos URP são criados no Editor com as APIs do pacote. `HauntRendering` seleciona o pipeline preparado antes da construção da apresentação.

`HauntProject` abre a cena somente com o Play parado e aguarda importação/compilação quando chamado pelo atalho. O comando de smoke confere a cena, os recursos e a geração inicial, sem executar combate ou validar imagens.

## Limites

O fluxo jogável oferece quatro armas iniciais exclusivas, seis armas inusitadas de loja, quatro personagens com poderes diferentes, dez inimigos comuns, um tipo de miniboss e cinco arquétipos de chefe. Os cinco chefes possuem identidades visuais: a abóbora original, a Viúva, o Bispo, a Matriarca e o Sino. Cada arquétipo de chefe alterna rajadas e, conforme seu padrão, carga ou ataque marcado no chão. Cada tema oferece três arquétipos possíveis em `DungeonFloor.BossCandidatesForTheme`; o sorteio usa um gerador independente da geração do mapa. Tema e guardião são estados separados: a composição de inimigos comuns e a arquitetura seguem `ThemeIndex`, enquanto arte e ataques do chefe seguem `BossArchetype`. O combate, a arte, as animações e o áudio ainda precisam de refinamento. Não há multiplayer, serviços externos ou gravação de incursões. As verificações fora do Editor não certificam a importação, os shaders ou a execução no Unity.

## Evolução para coop

O estado de personagem, arsenal e recarga está no núcleo e pode ser instanciado por jogador. Ainda será necessário extrair posição, entrada, mira e colisões do jogador em entidades independentes, definir autoridade da simulação e sincronizar geração, projéteis, compras e transições. Não há transporte de rede ou coop local/online nesta revisão.

## Apresentação desta revisão

Cada tema usa os IDs `floor-themeN-v0..v3`, `wall-themeN`, `wall-reliefN`, `decor-themeN` e `map-themeN`. Um hash da semente da sala e das coordenadas do ladrilho escolhe variante, tom, espelhamento e rotação de 90° dos pisos sem avançar o gerador de inimigos. Relevos com sombras e candelabros suspensos com quatro quadros dão volume às paredes. O atlas desenha corredores e miniaturas, com indicadores de entrada, guardião e sala atual. Efeitos ambientes usam o tempo de simulação e não consomem o gerador aleatório de inimigos.

Projéteis guardam cor, temporizador de rastro e um halo filho com `GlowMaterial`. O halo acompanha o projétil, recebe escala própria e é ocultado/reativado junto com ele pelo pool; novos disparos reaproveitam o mesmo par de objetos. Impactos combinam anel, clarão e estilhaços direcionais. Cada arma inusitada escolhe sprite e comportamento próprios do projétil. Avisos de chão são uma lista limitada a 16 marcadores, com 1,1 s de preparo, limpeza ao sair da sala e cancelamento ao purificar. A pausa congela esses tempos. Chamas, brilhos e efeitos luminosos usam sprites aditivos; atores e demais efeitos mantêm o material adequado ao seu desenho. Esses brilhos não são luzes físicas dinâmicas e não alteram colisões. `GlowMaterial` pertence à instância de `HauntGame` e é destruído em `OnDestroy`, assim como a biblioteca de arte libera suas texturas e material.

Os PNGs de prévia usam os pixels produzidos pelas rotinas Canvas do próprio código C#, com tipos auxiliares mínimos para executar o desenho fora do Editor. Esses stubs servem apenas à geração das imagens; não são execução Unity, não simulam material/câmera/URP e não certificam a aparência no jogo. Os checks de núcleo compilam os arquivos reais sem stubs Unity.

## Orientação de saída

`TryNearestExit` só oferece destino em uma sala purificada, sem inimigos e durante uma incursão viva. O destino é a porta de menor distância ao jogador; após vencer o guardião, o portal do mercado tem prioridade. A interface desenha a seta na direção desse destino, ajudando a explorar a arena com a câmera próxima. Pausa, loja e combate não exibem uma indicação de saída aberta.

## Catálogo de guardiões

Os pools de `DungeonFloor.BossCandidatesForTheme` são: Jardins `[Rei, Viúva, Matriarca]`, Galeria `[Viúva, Bispo, Sino]`, Catedral `[Bispo, Sino, Rei]`, Ninho `[Matriarca, Viúva, Rei]` e Abismo `[Sino, Matriarca, Bispo]`. O método retorna uma cópia para impedir mutação do catálogo compartilhado. O gerador de encontros usa semente e andar com sal próprio; suas chamadas não consomem o gerador da planta das salas. Os cinco desenhos de chefes são preservados e podem aparecer em mais de um tema.

## HUD, barra de chefe e atlas

O HUD normal usa corações no canto superior esquerdo e rótulos compactos, sem tarjas de fundo. `WorldTag`, notificações e legenda de saída também usam letras sem caixas. Durante encontros, `DrawBossHealth` posiciona uma barra centralizada na parte inferior: `boss-bar0..4` e `boss-fill0..4` seguem `BossVariant`, com moldura e preenchimento em pixel art para cada guardião. Nome e preenchimento acompanham o chefe vivo; a barra não ocupa o cabeçalho.

O atlas é desenhado antes do HUD. O mapa pequeno mantém alfa 0,45 e não tem painel; M abre uma área de 1060 × 580 na interface de referência 1280 × 720, com fundo escuro de tela inteira a alfa 0,78 e corredores, miniaturas e retratos a 0,95. Os marcadores da sala atual recebem mais 0,10, limitados a 1. As duas visões são passivas: não capturam cursor, mira ou tiro nem pausam o combate. Menu, catálogo do atendente, pausa e morte continuam capturando a interação. A seta de saída permanece disponível com o mapa ampliado.

## Tipografia em pixel art

`HauntPixelText` desenha glifos originais de 5×7 pixels, ampliados por fatores inteiros com filtro de ponto. Todos os rótulos visíveis da interface, botões, valores e avisos usam esse desenho; a normalização converte acentos, caixa e símbolos para o repertório bitmap. O campo de semente conserva a edição nativa de IMGUI, mas oculta os glifos nativos e sobrepõe as letras bitmap. Ícones, preços e descrições curtas substituem parágrafos no catálogo.

As texturas dos glifos são criadas sob demanda e mantidas em cache por caractere; `Dispose` as libera junto com a instância. A fonte não exige uma instalação de fonte do sistema. Layout e aparência finais ainda dependem de validação no Unity.

## Armas do mercado

O catálogo oferece `ShopItem.Stapler`, `Banana`, `SpinningTop`, `MirrorDisc`, `RayLantern` e `BoneFlute`, mapeados por `RunState.TryWeaponForItem`. Os IDs de bacamarte, repetidora e canhão permanecem aceitos no núcleo para compatibilidade, mas essas três armas convencionais não estão à venda no fluxo atual. O atalho 1 seleciona `StartingWeapon`; 2–7 selecionam grampeador, banana, pião, disco, lanterna e flauta quando adquiridos.

- Grampeador: dois grampos a 19 unidades/s, dano base por grampo, intervalo base ×1,4.
- Banana: um projétil com dano ×3 e curva de 52°/s, intervalo base ×2,2.
- Pião do Caos: projétil giratório a 8 unidades/s, dano ×2, duração de 2,8 s e no máximo três inimigos distintos por lançamento. Um mesmo inimigo não recebe dano repetido do mesmo pião. Paredes e cobertura encerram seu trajeto. Intervalo base ×3,5.
- Disco Espelho: dano ×2, velocidade 15 unidades/s e até dois ricochetes em paredes ou cobertura; o próximo contato sólido encerra o trajeto. A reflexão usa a normal da primeira colisão varrida. Intervalo base ×1,7.
- Lanterna de Raios: raio contínuo de alcance 11, dano ×2 por pulso e intervalo base ×0,6. O raio para no primeiro inimigo ativo ou obstáculo.
- Flauta de Ossos: três ossos em leque, a 16 unidades/s, dano ×2 por osso e intervalo base ×2.

Os preços iniciais 55/65/80/70/90/75 recebem a escala normal do andar. A compra atômica verifica saldo e propriedade antes de cobrar, concede a arma e a equipa. Arsenal e equipamento persistem entre andares; reiniciar apaga as aquisições e restaura a arma inicial da alma.

As armas iniciais têm cadências base ×1/×1,4/×1,8/×0,65: a carabina dispara a 22 unidades/s; a varinha lança três tiros com dano base; o lançador de ossos causa dano ×2; a bobina mantém um raio de alcance 7, com dano base por pulso. Os novos desenhos de projétil têm 6–8 pixels e usam escalas de 0,35–0,7; halos e impactos continuam luminosos sem ampliar sua forma física. Raios e projéteis usam o mesmo cálculo de cobertura, impedindo disparos através de paredes.

## Direção dos próximos conteúdos

As armas vendidas no Mercado seguem objetos inusitados e divertidos, com silhuetas e disparos próprios, como grampeador, banana e pião. Novas armas da loja devem manter essa direção. Todo novo texto visível deve passar por `HauntPixelText`, incluindo nomes, valores, botões e avisos; preserve mensagens curtas e prefira ícones quando suficientes.
