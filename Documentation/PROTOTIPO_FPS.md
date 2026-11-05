# Campanha híbrida 2D / primeira pessoa

`Assets/_Game/Scenes/HauntFps.unity` é a entrada padrão de **Ecos da Meia-Noite**, em Unity 6000.0.85f1. A campanha permite trocar entre câmera ortográfica e primeira pessoa durante o mesmo combate, em arenas 3D geradas em C#. Os personagens e as criaturas recebem apresentação em sprites na visão 2D e modelos procedurais na primeira pessoa.

## Abrir e completar a campanha

1. Extraia o projeto em uma pasta nova e curta e abra sua raiz pelo Unity Hub. Aguarde pacotes e compilação.
2. Com Play parado, use **Ecos → Abrir protótipo FPS** e pressione **Play**. O atalho `ABRIR_ECOS_DA_MEIA_NOITE.cmd` solicita essa mesma cena.
3. Escolha **Caçador, Médium, Ceifador ou Alquimista** e inicie. Cada personagem tem arma inicial e poder próprios.
4. Use **Tab / R1** para alternar entre as perspectivas. Vida, carregador, recarga, poderes, inimigos e projéteis continuam no mesmo encontro.
5. Vença três ondas e o chefe da fase. O portal abre após o chefe; aproxime-se e use **E / Quadrado** para avançar. Complete as cinco fases para terminar a campanha.
6. O menu oferece **treino de chefe**, com seleção dos sete chefes e dos cinco temas. **Enter / X** reinicia após morte ou vitória. **Esc / Options** pausa e libera o cursor.

## Controles

| Ação | Teclado / mouse | Controle PS4 reconhecido pelo Input System |
| --- | --- | --- |
| Mover | WASD / setas | Analógico esquerdo |
| Olhar no FPS / apontar no 2D | Mouse | Analógico direito |
| Alternar 2D / primeira pessoa | Tab | R1 |
| Atirar | Botão esquerdo | R2 |
| Recarregar | R | Triângulo |
| Usar poder | Q | L1 |
| Esquivar | Alt esquerdo / Ctrl esquerdo / botão direito | Círculo |
| Saltar | Espaço | X |
| Usar portal | E | Quadrado |
| Pausar e retomar | Esc | Options |
| Iniciar / reiniciar | Enter | X |
| Alternar áudio | M / botão do menu | Botão do menu |

Na visão 2D, o movimento segue os eixos da sala e a mira do mouse aponta uma posição no plano de combate; com controle, o analógico direito conserva a direção do tiro. Na primeira pessoa, movimento e mira acompanham a orientação do jogador. Os menus têm cursor livre. Após retomar ou trocar a perspectiva, solte o gatilho antes de voltar a disparar.

No menu inicial, **L1/R1** escolhem o personagem, **Triângulo** percorre campanha e chefes de treino, **Share** troca o cenário do treino e **X** inicia. Durante a pausa, Triângulo retorna à seleção. O ouvinte de áudio permanece junto ao jogador quando a câmera sobe, mantendo a distância e a orientação dos sons do combate.

## Troca de perspectiva

A troca não carrega outra cena e não cria outra cópia do jogador ou da sala. As duas visões usam o mesmo `CharacterController`, vida, munição e atores. A câmera elevada é ortográfica; a apresentação 2D mostra os personagens em sprites e reduz visualmente paredes e pilares para que não ocultem a ação. As colisões originais continuam presentes. Ao voltar à primeira pessoa, os modelos e as paredes completas reaparecem.

A transição dura **0,4 s**, com intervalo mínimo entre trocas. Ela bloqueia o disparo do jogador durante o efeito e não concede i-frames extras. O combate continua; pausa e perda de foco congelam a simulação. A esquiva permanece com **0,22 s**, protegendo nos primeiros 60% e deixando os 40% finais vulneráveis.

## Personagens e armas

| Personagem | Arma inicial | Balas no carregador | Comportamento e poder |
| --- | --- | --- | --- |
| Caçador | Carabina da Aurora | 12 | Tiro em linha; pulso próximo de dano |
| Médium | Varinha do Véu | 6 | Leque de tiros; lamento que atinge inimigos e desfaz projéteis hostis |
| Ceifador | Ceifador de Ossos | 6 | Tiro atravessa criaturas; colheita de curto alcance |
| Alquimista | Bobina Voltaica | 30 | Choque pode encadear outro alvo; elixir recupera vida |

As quatro armas têm reserva infinita e **recarga manual**. O custo do leque é uma bala por disparo aceito. Cada herói tem corpo procedural 3D, mãos e equipamento próprios: capa e carabina, capuz e cajado com orbes, foice e máscara óssea, ou frascos e mochila. Em primeira pessoa, pernas e sombra corporal continuam visíveis; a cabeça não obstrui a câmera. Os sprites 2D usam o catálogo do personagem correspondente. Clarão, rastro, recuo, cápsulas e feedback de impacto acompanham a arma. A proteção e a recarga em andamento continuam ao trocar de câmera.

## Fases, criaturas e chefes

A campanha usa cinco arenas de **28 × 24 unidades**, com cobertura e decoração próprias:

1. **Jardins da Colheita:** pedra, madeira, colheita e abóboras.
2. **Galeria dos Véus:** atmosfera espectral e luzes frias.
3. **Catedral das Cinzas:** colunas, cinzas e ornamentação ritual.
4. **Ninho da Peste:** tons de contaminação e criaturas do brejo.
5. **Abismo dos Sinos:** cenário escuro com materiais e luzes do abismo.

Cada fase reúne três ondas e um chefe. O catálogo contém **17 tipos de criatura**, distribuídos pelos temas: Esqueleto, Lanterna, Abóbora, Fantasma, Cultista, Morcego, Larva, Sentinela, Acólito, Slime, Aranha, Cogumelo Bruto, Diabrete de Corda, Espantalho, Gárgula, Sombra Perseguidora e Morcego de Fogo. Modelos, preparação de ataques, feedback e animações compartilham o combate das duas visões.

Os **sete chefes** são Rei das Abóboras, Viúva do Véu, Bispo das Cinzas, Matriarca da Peste, Sino do Abismo, Carrasco de Ferro e Oráculo do Brejo. Cada um tem silhueta 3D, apresentação em sprite no 2D, introdução de três segundos, preparação de ataque, padrões próprios, fúria abaixo de 50% e morte. A introdução protege o chefe até terminar. Bombas em cruz, leques, espirais, formações em cruz, anéis com brecha e orbes que se dividem usam o mesmo espaço 3D.

A seleção normal de chefe segue os grupos temáticos e a semente de `DungeonFloor`. O treino permite escolher diretamente. Derrotar o chefe libera o portal e recupera dois pontos de vida, até o máximo. Avançar conserva vida, arma, munição, moedas e recarga dos poderes; uma nova incursão restaura a progressão inicial.

Barris explosivos possuem volume, pavio, dano radial, empurrão, trauma e reação em cadeia; cobertura bloqueia a explosão. Sons e música são sintetizados pelo jogo, com controle de áudio, pausa e foco da janela.

## Módulos

| Arquivo | Responsabilidade |
| --- | --- |
| `Fps/HauntFpsGame.cs` e `.UI.cs` | Sessão, combate, projéteis, HUD e menus |
| `Fps/HauntFpsGame.Campaign.cs` | Progressão, armas, poderes e chefes |
| `Fps/HauntFpsPlayer.cs` | Entrada, movimento, esquiva, quatro equipamentos e cápsulas |
| `Fps/HauntFpsHeroBody.cs` | Quatro corpos 3D, acessórios, animação e sombras do jogador |
| `Fps/HauntFpsPerspective.cs` | Câmeras, transição, orientação e mira nas duas perspectivas |
| `Fps/HauntFpsHybridArt.cs` | Sprites, modelos e corte visual de paredes sem trocar colisões |
| `Fps/HauntFpsWorld.cs` | Cinco temas, geometria, materiais, luzes, neblina e portal |
| `Fps/HauntFpsCampaign.cs` | Catálogo de temas, rosters e escolha de chefe |
| `Fps/HauntFpsEnemy.cs` e `.Models.cs` | Catálogo dos 17 mobs, comportamento e modelos |
| `Fps/HauntFpsBoss.cs` | Sete modelos, animações, introdução e padrões dos chefes |
| `Fps/HauntFpsBarrel.cs` | Barris destrutíveis e reação em cadeia |
| `Fps/HauntFpsAudio.cs` | Clipes originais, vozes e música |
| `Fps/HauntFpsLit.shader` | Material pixelado com iluminação |
| `Core/FpsPrototypeRules.cs` | Regras de ondas, esquiva, alcance e dano radial |
| `Editor/HauntFpsProject.cs` | Menu, preparação do renderer e smoke check de importação |

Os caminhos são relativos a `Assets/_Game/Haunt`. `Resources/FpsPalette.mat` conserva a referência do shader. O Editor gera `Resources/FpsPipeline.asset` e `Resources/FpsRenderer.asset`; a execução aplica o pipeline e restaura o anterior ao encerrar.

## Convivência com o roguelike 2D anterior

O build contém `HauntFps.unity` primeiro e `Haunt.unity` em seguida. **Ecos → Abrir roguelike** abre a versão anterior com Play parado. Ela conserva sua rede de salas procedurais, loja, inventário completo e folhas de animação.

A campanha híbrida usa cinco arenas temáticas com três ondas e chefe. A rede completa de salas, todas as armas e o Mercado das Almas ainda não foram todos migrados para ela. Alternar perspectiva nesta campanha difere de carregar a cena antiga pelo menu. O jogo continua solo; esta implementação não adiciona coop.

## Verificar no Editor

**Este ambiente não possui Editor Unity.** Os checks Python e C# independentes verificam fontes, regras e entrega; não executam a física, os shaders, o áudio ou a entrada do motor.

Com um Editor licenciado e `UNITY_EDITOR_PATH` definido:

```bash
bash Tools/unity-check.sh import
bash Tools/unity-check.sh fps-smoke
bash Tools/unity-check.sh smoke
bash Tools/unity-check.sh tests
bash Tools/unity-check.sh build
```

`fps-smoke` verifica cena, bootstrap, material e renderer importados; `smoke` acrescenta a cena 2D anterior. Nenhum dos dois entra em Play Mode. A importação, a compilação Unity, o resultado renderizado e o build continuam pendentes até serem executados no motor.

No Play, teste cada personagem e todas as combinações de chefe/tema no treino. Alterne a câmera enquanto atira, recarrega, esquiva e recebe projéteis; confira a continuidade dos valores e das posições, o corte das paredes e o retorno dos modelos. Pause no meio da transição e da introdução. Complete a campanha, perca vida, reinicie e encerre o Play, verificando descarte e restauração de ambiente/pipeline/cursor. Repita com controle físico. Veja o roteiro completo em [VALIDATION.md](VALIDATION.md).
