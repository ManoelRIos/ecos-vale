# Arquitetura de Ecos da Meia-Noite

A única cena é `Assets/_Game/Scenes/Haunt.unity`. `HauntGame` constrói e simula a incursão local; `Haunt.Runtime`, `Haunt.Editor` e `Haunt.Tests` separam jogo, ferramentas e testes. A arte combina a folha de personagem fornecida pelo usuário, suas variantes e recursos procedurais. Sons e música são sintetizados localmente, sem rede no runtime.

## Núcleo independente do Unity

`DungeonFloor` gera uma árvore cardinal, com conexões recíprocas e chefe em uma folha de distância máxima da entrada. Apenas uma sala é instanciada por vez. `RoomCountForNumber` calcula 7 + 2 por andar, limitado a 27. A semente e o número reproduzem planta, guardião, perfil `EncounterKind` e salas com miniboss. O gerador de encontros tem sequência independente do crescimento da planta. Cada tema possui três candidatos entre sete chefes.

Salas comuns exigem três ondas (`WavesCleared`, `TotalWaves`); entrada e chefe têm uma. Há uma sala de miniboss por andar, duas a partir do quarto, sempre fora da entrada e da sala do chefe. Perfis Swarm/Ambush/Crossfire/Elite escolhem composição e formação em vez de repetir o mesmo grupo. Vida dos inimigos tem crescimento moderado; emergência dura 0,9 s com IA, colisão ofensiva e dano recebido suspensos.

`RunState` mantém vida, moedas, dano, cadência, personagem, poder, arsenal, arma equipada, relíquias e compras da visita. `ShopWeapons` retorna um clone estável de uma ou duas ofertas determinísticas, preferindo armas ainda não possuídas; esgotar o arsenal gera estoque vazio. `IsOffered` e `TryBuy` compartilham a seleção. Compra fora do estoque, sem saldo, repetida ou inválida é atômica e não cobra moedas. Comprar não sorteia outra arma durante a visita. `NextFloor` preserva o inventário e invalida as ofertas; nova incursão restaura os padrões.

`TryWeaponForItem` associa explicitamente os 12 produtos de arma a `WeaponKind`; não há cálculo por deslocamento entre enums. As quatro almas têm armas iniciais próprias. `ProjectileScale` cresce de 1 a 1,6, `MoveSpeedMultiplier` de 1 a 1,25 e `HasCharacterRelic` concede um efeito particular. Hunter/Reaper recebem `ProjectileDamageBonus`; Medium/Alchemist alteram recarga e cura no próprio núcleo.

`CombatGeometry` calcula a primeira fração de contato de segmentos com círculos e caixas. Esses mesmos arquivos são compilados por `Tools/HauntChecks.csproj`, sem simulação de tipos Unity.

`WeaponAmmo.Stats` centraliza os valores de carregador, reserva e tempo das 17 armas. `RunState` mantém um registro por arma e o relógio da recarga: consumo só é aceito se vivo, carregado e sem recarga; a transferência da reserva ocorre ao concluir, sem descartar cartuchos em cancelamentos. Arma inicial e revólver têm reserva -1 (infinita), mas carregadores finitos. Trocar arma cancela a recarga; trocar de andar preserva munição; uma nova incursão cria outros registros.

`HauntGame.Ammunition.cs` conecta R/Triângulo, sons, drops de 12% por morte e coleta. O hash usa semente, andar, sala e total de mortes sem consumir o RNG de encontros. Registros persistem por sala no andar; reconstruir limpa apenas visuais e restaura caixas não coletadas. Reserva cheia não consome pickup. `FireWeapon` verifica espaço para a salva completa antes de debitar uma carga; `UpdateBeam` debita apenas nos pulsos de dano, inclusive o último. `Fire`/`FireSpecial` são emissores sem cobrança para preservar poderes e padrões hostis. Veja [MUNICAO_E_TERROR.md](MUNICAO_E_TERROR.md).

## Game feel e padrões

`BulletPatternGenerator` contém a matemática pura de Fan, Ring, GapRing, Spiral e CrossFormation, além de mira por interceptação, desaceleração e giro limitado. `HauntGame.Patterns.cs` transforma essas geometrias em ataques anunciados e sequências com cancelamento por geração do combate. `HauntGame.Weapons.cs` aplica movimento e primeira colisão aos tiros, ricochetes, divisão por distância e efeitos ao terminar. A morte de um alvo pode iniciar uma rajada; concluir a onda cancela os filhos pendentes para preservar a limpeza do combate.

`HauntGame.Feel.cs` reúne avanço de câmera na mira, trauma Perlin, recuo por disparo, flash individual no shader, deformação, empurrão com colisão, cápsulas e afterimages. `DodgeRules` define a proteção dos primeiros 60% do rolamento; a recuperação é vulnerável, enquanto salto e duração de movimento continuam independentes. `HauntGame.Lighting.cs` e `HauntFlickerLight` controlam sprites radiais aditivos, variação de intensidade/raio, tons noturnos e vinheta antes do HUD. Consulte [GAME_FEEL.md](GAME_FEEL.md) para os contratos e o catálogo dos ataques.

## Combate e objetos

`HauntGame.cs` contém ciclo da incursão, movimento, ondas, IA, sete chefes e transições. Movimento subdivide deslocamentos e desliza por eixos; a esquiva dura 0,24 s, evita dano nos primeiros 0,144 s e pode cruzar abismos durante a animação completa. Quedas custam uma vida e retornam ao último chão seguro. Inimigos terrestres evitam buracos, enquanto fantasma, morcego e lanterna flutuam. Há separação entre criaturas para diminuir aglomeração. `FireHostile` limita disparos hostis a 140, respeita o limite total de 420 e reserva 72 entradas para o jogador.

`HauntGame.Weapons.cs` implementa armas, raios, homing limitado, ricochetes e pooling. Projéteis têm corpo e halo reutilizados juntos, na mesma altura visual. `FindSolidContact`, `FindRoomPropContact` e a geometria dos inimigos disputam o primeiro contato; dano é aplicado apenas após selecionar o alvo mais próximo. O raio segue a mira continuamente, mas causa dano na cadência da arma. Ele para ao soltar, esquivar, pausar, morrer ou abrir o catálogo. Balas gigantes aumentam visual e raio físico apenas dos tiros amigáveis; o dano do passivo é aplicado uma vez. A luva usa `MoveEnemy` para o empurrão respeitar cobertura e abismos.

`HauntGame.Environment.cs` usa uma sequência própria para distribuir barris e urnas fora das rotas centrais. Seus limites entram na lista de obstáculos. Ao quebrar, retiram a colisão e registram uma chave andar/sala/objeto em `brokenRoomProps`; revisitas mantêm os destroços. Um novo jogo limpa esse conjunto. Barris têm fusível de 0,55 s com marcador antes de explodir. A explosão respeita cobertura e usa um snapshot dos inimigos da onda para não ferir uma onda recém-criada pela morte do último alvo. Propagação para outros barris é anunciada e também respeita cobertura; não remove projéteis durante iterações.

Urnas criam até dez poças, com área elíptica correspondente ao desenho e duração de 14 s. Andar nelas multiplica a velocidade por 0,6; os fatores não acumulam. A esquiva usa sua velocidade própria. A água some ao reconstruir a sala, enquanto o estado quebrado do recipiente continua. Partículas têm limite de 320 e os objetos interativos de cada sala são limitados a oito.

## Profundidade visual e interface

`HauntGame.Visuals.cs` constrói pisos, faces e coroamentos de paredes, cobertura, relevos, velas, candelabros, neblina e vitrines. O combate permanece em XY com câmera ortográfica de tamanho 6,3; a arena mede 29 × 17. `Depth(y)` ordena sprites pelos pés; altura aparente e sombras são apenas apresentação. As paredes à frente do jogador ficam translúcidas para manter leitura. É uma apresentação de profundidade por camadas; não há malhas 3D ou conversão da física para XZ.

Neblina oscila abaixo dos atores em temas e salas específicos. Projéteis e seus halos usam altura visual +0,38. O material aditivo de tiros tem ganho menor que o das luzes ambientes. Os materiais copiam o shader `HauntSprites`, respeitando cor/espelhamento do SpriteRenderer do URP 17. Não há iluminação física dinâmica. A biblioteca `HauntPixelArt` usa filtro de ponto e cache de poses: as folhas nativas dos personagens usam 16 pixels por unidade; folhas procedurais e equipamentos detalhados usam 32, enquanto projéteis e demais recursos legados conservam 16. A grade de posicionamento visual `Snap` usa 32 passos por unidade, como a câmera. Os recursos de propriedade da biblioteca são liberados ao destruir o jogo.

`HauntGame.UI.cs` usa IMGUI e `HauntPixelText` para glifos bitmap originais 5 × 7 em toda a interface. HUD, corações, notificações e etiquetas não têm tarjas. A barra de chefe inferior usa uma moldura para cada arquétipo. O mapa tem ilustrações temáticas: pequeno em 45% de opacidade, atlas 1060 × 580 em 95%, sobre fundo escuro de tela inteira a 78%. As sobreposições permanecem passivas.

A loja é uma sala explorável. Balcão e prateleiras têm colisão; as uma ou duas vitrines usam exatamente `State.ShopWeapons`. Interagir a até 2,8 unidades do atendente abre o catálogo e bloqueia movimento; sair da loja exige fechar a conversa e usar o portal. O catálogo apresenta até três cartões por página. Equipamentos e relíquias duram até o fim da incursão, sem save ou multiplayer.

## Entrada e áudio

`HauntGame.Input.cs` unifica teclado, mouse e Gamepad do Input System. Dispositivos DualShock reconhecidos usam o mesmo contrato: analógicos para movimento/mira, R2 tiro, L2/Círculo esquiva, Quadrado poder, X interação, L1/R1 armas, Triângulo recarga fora do catálogo, Options pausa e Share/touchpad mapa. Tab e roda também percorrem todo o inventário. `PollControlInput` navega menus e mantém a direção da mira após soltar o analógico. O contexto modal, consumo por frame e liberação do X impedem abrir catálogo e comprar com a mesma pressão. Não há plugin nativo ou mouse virtual.

`HauntGame.Audio.cs` sintetiza 19 efeitos PCM mono em 22.050 Hz, com oito vozes limitadas, intervalo mínimo por efeito e um loop de raio. Cinco temas musicais usam cama harmônica/melódica e camada rítmica, em loops de 16 tempos a 112 BPM. Os clipes são cacheados; duas duplas de fontes fazem a transição de 1,2 s entre temas. A intensidade depende de menu, loja, combate e chefe. `UpdateMusic` roda mesmo antes dos retornos por menu/pausa; áudio respeita mute, pausa, foco e suspensão do app. `DisposeAudio` libera fontes e clipes.

## Validação e empacotamento

Checks Python cobrem arquivos, referências, metadados, versões, atalho e ZIP determinístico; checks C# executam o núcleo real. Helpers externos com tipos Unity simulados permitem conferir lógica e assinaturas, sem validar o motor. Testes Editor em `Assets/_Game/Haunt/Tests/Editor` precisam de Unity licenciado. A preparação URP acontece em `HauntSetup`, e `HauntProject` abre cena e gera build quando solicitado.

`Tools/package_project.py` empacota arquivos rastreados com os conteúdos atuais, timestamps fixos e raiz curta `EcosDaMeiaNoite/`; rejeita caminhos inseguros, symlinks e projeto incompleto. Exclui Distribution, builds e caches. Os parciais de combate, câmera e iluminação, o componente de flicker, as regras de evasão e o gerador matemático com seus metadados são arquivos obrigatórios. O runtime não tem dependências de APIs externas ou segredos.

## Salas irregulares e narrativa

`RoomLayout` gera uma máscara de 29×17 células para L/T/H/octógono/alcovas, com núcleo e quatro aproximações conectadas. `IsInside`/`IsFloor` representam a pegada incluindo abismos, `IsPit` separa risco de queda. Retângulos de vazio unidos por faixas entram nos mesmos obstáculos consultados por movimento, cover, tiros e raios. `ContainsCircle` valida o raio do corpo; BFS fornece passos seguros. `HauntGame.Rooms.cs` instancia a geometria, os contornos e os caches de mapas, além de spawn e navegação que também consideram obstáculos dinâmicos.

`HauntGame.Bestiary.cs` concentra os quatro comportamentos novos e a imunidade temporária da gárgula. `HauntGame.Environment.cs` adiciona pilares, barris de ácido e zonas de dano em pulsos, com snapshots de criaturas para não atingir ondas recém-criadas. Estado quebrado mantém a seleção de posições determinística nas revisitas. A física continua 2D.

`BossFrame` no gerador de arte fornece cinco estados×quatro quadros por guardião. `AttackVisualTime`, `Tell` e vida selecionam a pose; `HauntGame.BossPresentation.cs` mantém morte temporária, orbes e fumaça sem manter o boss vivo na simulação. A animação de morte é descartada ao trocar de sala.

`HauntGame.Conversations.cs` possui diálogo, escolhas, máquina de escrever e intro de três segundos. O laço principal atualiza esse relógio e retorna antes do combate enquanto a apresentação está ativa. Input modal é consumido antes de armas/loja, impedindo negociações e compras com a mesma pressão. A música consulta esse estado para fazer a transição ao sexto tema exclusivo de chefe; vozes são clipes originais curtos cacheados.

## Personagens importados e estados variáveis

`Assets/_Game/ArtSources/PilotOriginal.png` preserva o PNG fornecido pelo usuário. `Tools/Pilot/source-clips.json` identifica os retângulos, pivôs, cores de fundo e sequências da prancha. `Tools/import_pilot_sheet.py` gera as quatro folhas de `Resources/CharacterSheets` e `catalog.json`. O primeiro personagem copia os pixels RGB originais; as outras três variantes conservam a geometria e a organização das poses, com paletas e acessórios próprios. A conversão não redimensiona o desenho nativo.

`HauntPixelArt.Characters.cs` carrega esse catálogo e oferece `CharacterFrame`, `CharacterFrameCount`, `CharacterFrameRate` e `CharacterClipDuration`. Os dados distinguem clip, direção, postura da arma e traje alternativo. A amostragem respeita o comprimento e a repetição de cada sequência. Direções ou posturas ausentes usam uma pose existente da fonte. Attack, Reload e Cast selecionam a pose Idle correspondente: o recuo, a recarga da arma e os efeitos de poder continuam separados do sprite corporal.

As folhas usam 16 PPU e células de organização de 48×48 com preenchimento transparente. O retângulo de renderização e o pivô de cada frame ficam no catálogo, já convertidos para a origem inferior esquerda do Unity. Assim o pé fica no chão sem exigir que todas as poses tenham o mesmo tamanho. Texturas de Resources são compartilhadas; a biblioteca libera somente seus sprites recortados e recursos procedurais próprios.

`HauntGame.SpriteAnimation.cs` seleciona os clips a partir de movimento, mira, arma, disparo, poder, recarga, evasão e morte. Contagens e tempos vêm da biblioteca de personagens, em vez do limite de quatro quadros dos protótipos. A arma fica em um renderer separado. As poses de mesa, baú, voo, fantasma e reviver permanecem disponíveis como dados de animação, sem criar essas mecânicas por sua presença na folha. Físicas e colisões continuam usando as posições do chão. Consulte [CHARACTER_MODEL.md](CHARACTER_MODEL.md).

## Folhas procedurais e equipamento

`HauntPixelArt.Actors.cs` conserva os protótipos de atores em 48 px e os seis mobs com cache por estado, direção, frame e equipamento. `HauntPixelArt.ReferenceBosses.cs` desenha os sete bosses de 96 px e objetos de 32 px. `HauntPixelArt.SpriteSheets.cs` centraliza `ReferencePixelsPerUnit = 32`, carrega `Resources/ReferenceSheets` e recorta o Sprite solicitado. As 24 folhas e 1.700 células desse conjunto incluem os protótipos dos jogadores usados como fallback quando o catálogo nativo não está disponível. `HauntPixelArt(false)` ignora Resources para exportar pixels procedurais legíveis.

`HauntPixelArt.Equipment.cs` identifica armas, relíquias e decoração por `HasDetailedEquipment` e acrescenta detalhes em `RefineEquipment` sobre uma tela de resolução dobrada. `CacheSprite` recebe os pixels por unidade do recurso, permitindo 32 PPU nos desenhos detalhados e 16 PPU nos recursos legados. As 17 armas e os itens decorativos continuam procedurais. Remanescentes de morte dos seis mobs duram 0,42 s e têm limite de 32; a morte nativa dos jogadores acompanha a duração do seu clip. Limpeza por sala cancela essas apresentações.

O HUD e os retratos usam `DrawSpriteCell`: ajusta a proporção e recorta UVs pelo `sprite.rect`, evitando desenhar a folha inteira. `Tools/export_sprite_sheets.py` compila o gerador real por um executável .NET com interfaces de pixels mínimas e grava as folhas procedurais, metadados e prévias. Os comandos `--check` dos dois importadores/exportadores comparam os resultados sem alterá-los. Isso não executa Unity. Consulte [SPRITESHEETS.md](SPRITESHEETS.md).
