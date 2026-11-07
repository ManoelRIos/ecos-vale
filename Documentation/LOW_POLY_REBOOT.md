# Nova base low poly em primeira e terceira pessoa

A direção atual de **Ecos da Meia-Noite** começa em `Assets/_Game/Scenes/HauntLowPoly.unity`. É um jogo 3D com malhas facetadas e materiais toon, alternando entre FPS e câmera sobre o ombro. O conceito mantém as quatro almas, armas incomuns, progressão roguelike, fases macabras e chefes. A branch desta direção é `codex/ecos-lowpoly-fps-tps`.

Esta base foi iniciada no commit `fad78df`. A branch anterior, `codex/ecos-do-vale-vertical-slice`, continuou recebendo atualizações durante o trabalho e foi preservada no GitHub, incluindo o commit `60f5541` observado antes desta publicação. Os perfis de referência incluídos aqui correspondem ao ponto de partida desta base; a evolução posterior continua disponível na branch anterior.

## Abrir

Use Unity **6000.0.85f1** e **Ecos → Abrir jogo low poly**, com Play parado. O atalho do projeto solicita essa mesma entrada. O build contém `HauntLowPoly` primeiro, `HauntFps` depois e `Haunt` por último.

As duas cenas anteriores preservam seus perfis: campanha híbrida ortográfica/FPS e roguelike 2D. O campo serializado `lowPolyMode` está ativo somente na nova cena. O núcleo de combate é compartilhado; entrar na nova cena não inicia os outros bootstraps.

## Combate e câmera

**V / R1** troca FPS e terceira pessoa durante a mesma luta. A câmera de ombro acompanha o jogador e testa o espaço entre o ponto de seguimento e sua posição desejada para evitar atravessar paredes. Em terceira pessoa, o centro da tela escolhe o alvo e a consulta de disparo parte da arma no mundo, com uma verificação adicional entre ombro e cano. A cobertura junto ao personagem continua participando do acerto mesmo quando a câmera enxerga além dela.

Vida, carregadores, reservas, recarga, arma, poder, esquiva, inimigos e projéteis pertencem à simulação comum. Trocar a câmera altera apresentação e mira; não cria uma segunda cópia do encontro e não concede proteção extra. A transição bloqueia o disparo por um curto período. Pausa e perda de foco congelam o combate.

Os quatro personagens mantêm seus poderes e equipamentos: carabina e pulso do Caçador, varinha e lamento do Médium, ossos e colheita do Ceifador, bobina e elixir do Alquimista. Armas iniciais têm reserva infinita, mas exigem recarga manual. Armas adquiridas mantêm reservas próprias. O catálogo de munição e compras vem de `RunState` e `WeaponAmmo`.

| Ação | Teclado / mouse | Controle |
| --- | --- | --- |
| Mover / mirar | WASD e mouse | Analógicos esquerdo e direito |
| Trocar perspectiva | V | R1 |
| Atirar / recarregar | Esquerdo / R | R2 / Triângulo |
| Esquivar / saltar | Direito, Alt ou Ctrl / Espaço | Círculo / X |
| Poder | Q | L1 |
| Trocar arma possuída | Roda do mouse ou 1/2 | Direcional esquerdo/direito |
| Usar porta, portal ou loja | E | Quadrado |
| Pausa / mapa / som | Esc / Tab / M | Options / Share / menu de som |

No menu inicial, L1/R1 escolhem a alma, Triângulo percorre campanha e chefes de treino, Share troca o cenário do treino e X inicia. No comércio, o direcional escolhe o item, X compra e Círculo fecha. O reconhecimento de um controle físico ainda precisa de teste no PC.

## Masmorra e progressão

`DungeonFloor` fornece a planta por semente, com entrada segura, conexões cardinais e chefe distante. Os cinco andares possuem **7, 9, 11, 13 e 15 salas**, respectivamente. Uma sala 3D de 28 × 24 unidades é construída por vez; as arenas usam decoração e cobertura do tema, enquanto a árvore de portas determina a exploração. A apresentação não implementa os recortes L/T/H e abismos do roguelike 2D anterior.

Visitar outra sala conserva as que já foram vencidas. Cada combate comum tem duas ondas, de três e quatro criaturas; ao vencê-las, as portas reabrem. Salas marcadas como encontro de elite incluem uma Sentinela reforçada na segunda onda. Entradas e revisitas a salas limpas não geram outra leva. Ao atravessar uma porta, o personagem entra pelo lado oposto, voltado para o centro. O chefe tem sala própria, introdução, preparação de ataques e fúria; sua derrota libera comércio e portal para o próximo andar.

Há cinco temas: Jardins da Colheita, Galeria dos Véus, Catedral das Cinzas, Ninho da Peste e Abismo dos Sinos. O catálogo tem 17 mobs e sete chefes. A composição, a cobertura, os materiais e a iluminação usam o tema atual. Os recursos são gerados em C# e mantêm limites de vozes, projéteis e efeitos.

**Tab / Share** alterna o mapa compacto, que mostra salas visitadas e suas conexões conhecidas. Ele é uma sobreposição passiva: o combate continua enquanto está aberto. Descobertas pertencem ao andar atual e são limpas ao avançar ou reiniciar.

## Arsenal e Mercado das Almas

Os **17 tipos de arma** têm modelos 3D e usam os carregadores e cadências do núcleo. Os disparos do jogador são consultas instantâneas de colisão com rastros luminosos: tiro em linha, leques, penetração, encadeamento elétrico, ricochetes, explosão no impacto ou golpe próximo, conforme a arma. A Lanterna mantém um rastro por mais tempo para seus pulsos de feixe. Esses efeitos não representam projéteis físicos independentes do jogador; projéteis hostis continuam sendo orbes simulados com movimento, varredura de colisão, bombas em cruz e divisão.

Depois de derrotar o chefe, o Coletor de Almas aparece com balcão e armas expostas. O estoque por semente contém **uma ou duas armas**, além de um elixir que recupera até três pontos de vida. Aproxime-se e use E/Quadrado para negociar. O menu mostra nome, característica curta e preço; uma compra aceita debita uma vez, equipa a arma e mantém o item esgotado naquela visita. Recusas não gastam almas. O combate fica suspenso durante a negociação.

Ao possuir uma arma de reserva finita, uma morte de mob pode gerar munição pelo limiar de 12% do sorteio determinístico. Aproximar-se tenta repor a reserva de uma arma que precise; isso não recarrega o carregador. Caixas não usadas e barris destruídos são lembrados nas revisitas do mesmo andar. A arma inicial e o revólver de apoio conservam reserva infinita, com recarga manual obrigatória.

## Arte e módulos

A geometria usa cores de material e faces planas. `HauntLowPolyPrimitives` reutiliza malhas por instância e libera os recursos quando a sessão termina. Os modelos preservam sinais das classes e criaturas, com silhuetas e acessórios 3D. A nova cena usa `Resources/LowPolyPalette.mat` e o shader `Ecos/HauntLowPolyToon`; os sprites e materiais antigos permanecem para as cenas de referência.

| Arquivo | Responsabilidade |
| --- | --- |
| `Fps/HauntFpsGame.cs` e `.LowPoly.cs` | Perfil da nova cena, sessão, salas, comércio e progressão |
| `Fps/HauntFpsGame.UI.cs` e `.LowPolyUI.cs` | HUD, mapa, negociação e menus |
| `Fps/HauntFpsPlayer.cs` | Entrada, movimento, evasão, armas e feedback |
| `LowPoly/HauntLowPolyCamera.cs` | Câmera FPS/TPS, transição, mira e colisão de câmera |
| `LowPoly/HauntLowPolyPrimitives.cs` | Malhas facetadas e recursos compartilhados |
| `LowPoly/HauntLowPolyDoors.cs` | Portas e passagens da planta |
| `LowPoly/HauntLowPolyArsenal.cs` | Modelos do arsenal |
| `Fps/HauntFpsWorld.cs` | Temas, construção de salas, luzes e cenário |
| `Fps/HauntFpsHeroBody.cs`, `HauntFpsEnemy.Models.cs`, `HauntFpsBoss.cs` | Personagens e criaturas |
| `Editor/HauntLowPolyProject.cs` | Entrada e smoke check da nova cena |

Caminhos relativos a `Assets/_Game/Haunt`. O pipeline `FpsPipeline`/`FpsRenderer` é preparado pelo Editor e usa um renderer URP 3D. A escrita das opções de sombras utiliza a serialização do Editor; a propriedade pública de sombras do URP é somente leitura.

## Verificação no Unity

O Editor não está instalado neste ambiente. A compilação de helpers com tipos simulados verifica lógica e contratos selecionados, sem certificar física, renderização, HLSL, áudio reproduzido ou hardware. Os testes nativos foram preparados para execução no motor.

Com `UNITY_EDITOR_PATH` apontando para um Editor licenciado:

```bash
bash Tools/unity-check.sh import
bash Tools/unity-check.sh lowpoly-smoke
bash Tools/unity-check.sh smoke
bash Tools/unity-check.sh tests
bash Tools/unity-check.sh build
```

`lowpoly-smoke` confere recursos importados, perfil serializado e primeira cena do build. `smoke` acrescenta as duas cenas preservadas; esses comandos não entram em Play Mode.

No Play, teste primeiro a câmera e o tiro junto a paredes, pilares, portas e barris. Troque a perspectiva durante recarga e evasão; pause no meio da transição. Explore portas recíprocas, revisite salas limpas, confira a descoberta do mapa, encontre o chefe e teste comércio, munição e portal. Retire e devolva o foco da janela dentro e fora da loja. Confira colisão e mira dos mobs baixos. Teste cada alma, arma e tema e observe o Console. Depois confirme que as cenas anteriores continuam abrindo com seus perfis. Repita a entrada com controle físico e teste o executável em Windows após um build bem-sucedido.
