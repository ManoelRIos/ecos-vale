# Ecos da Meia-Noite — low poly

Nova base de ação roguelike em **3D low poly**, com alternância entre **primeira e terceira pessoa**, para **Unity 6.0 LTS 6000.0.85f1**. Mantém as ideias de combate frenético, quatro almas jogáveis, armas incomuns e cinco temas macabros, com salas conectadas, cobertura, chefes e Mercado das Almas. O jogo permanece solo.

[**Projeto no GitHub**](https://github.com/ManoelRIos/ecos-vale/tree/codex/ecos-lowpoly-fps-tps) · [**Baixar projeto em ZIP**](https://github.com/ManoelRIos/ecos-vale/raw/refs/heads/codex/ecos-lowpoly-fps-tps/Distribution/EcosDaMeiaNoite.zip)

## Abrir e jogar

1. Instale **Unity 6000.0.85f1** pelo Unity Hub. Para gerar um executável Windows, acrescente Windows Build Support.
2. Extraia o ZIP em uma pasta nova e curta, como `C:\Dev\EcosDaMeiaNoite`. Preserve a cópia anterior e suas alterações próprias.
3. No Hub, selecione a raiz que contém `Assets`, `Packages` e `ProjectSettings`. Aguarde a resolução dos pacotes e a compilação.
4. Com Play parado, use **Ecos → Abrir jogo low poly** para abrir `Assets/_Game/Scenes/HauntLowPoly.unity`.
5. Pressione **Play**, escolha uma alma e inicie. **V / R1** alterna FPS e terceira pessoa. Explore as salas, elimine as ondas e encontre o chefe. **E / Quadrado** usa as portas e as interações próximas.

O atalho opcional `ABRIR_ECOS_DA_MEIA_NOITE.cmd` abre a mesma cena. `/check` verifica a pasta; o log fica em `Logs/MeiaNoite-Editor.log`. A abertura pelo Hub dispensa o atalho. Não há executável Windows validado desta revisão; o ZIP contém o projeto Unity.

## Controles

| Ação | Teclado / mouse | Controle reconhecido pelo Input System |
| --- | --- | --- |
| Mover | WASD / setas | Analógico esquerdo |
| Olhar e mirar | Mouse | Analógico direito |
| Primeira / terceira pessoa | V | R1 |
| Atirar | Botão esquerdo | R2 |
| Recarregar | R | Triângulo |
| Poder da alma | Q | L1 |
| Esquivar | Botão direito / Alt / Ctrl | Círculo |
| Saltar | Espaço | X |
| Porta / atendente / portal | E | Quadrado |
| Trocar arma | Roda do mouse | Direcional |
| Mapa | Tab | Share |
| Pausar | Esc | Options |
| Iniciar / reiniciar | Enter | X |
| Som | M / menu | Menu |

O cursor fica capturado nas duas perspectivas de combate. A terceira pessoa usa câmera sobre o ombro, com proteção contra paredes e cobertura. A mira calcula o alvo pelo centro da tela e o disparo parte da arma no mundo, de modo que a cobertura junto ao personagem continua bloqueando o tiro. Trocar a câmera mantém vida, arma, munição, recarga, poderes, inimigos e projéteis; a transição bloqueia o disparo e não concede invencibilidade extra.

## Conteúdo desta base

Os **quatro personagens** mantêm armas iniciais e poderes próprios: Caçador, Médium, Ceifador e Alquimista. As armas iniciais têm reserva infinita, com carregadores finitos e recarga manual; armas adquiridas usam sua própria munição. O catálogo reaproveita as 17 armas do núcleo, incluindo objetos incomuns. Inimigos podem deixar munição para armas com reserva finita.

A campanha percorre **cinco temas**: Jardins da Colheita, Galeria dos Véus, Catedral das Cinzas, Ninho da Peste e Abismo dos Sinos. A planta de cada andar usa salas conectadas geradas pela semente. Uma sala é construída por vez; entrada, salas de combate e chefe conservam seu estado nas revisitas. As portas ficam bloqueadas durante o encontro. O mapa ajuda a localizar a progressão e o chefe.

O catálogo inclui **17 mobs e sete chefes** com modelos procedurais, preparação de ataques, feedback de dano e padrões de projéteis. O chefe libera o Mercado das Almas e o portal para o próximo andar. A loja oferece armas e melhorias usando as moedas da incursão. Barris, pilares e outras peças dão volume e cobertura ao cenário.

A arte da nova cena usa **malhas facetadas, materiais de cor e iluminação toon**, com corpos e armas 3D. A cena low poly não alterna para sprites ou visão ortográfica. Sons e música são sintetizados localmente. Coop, assets modelados externamente e acabamento artístico continuam como expansões futuras.

## Versões preservadas

A nova direção está na branch [`codex/ecos-lowpoly-fps-tps`](https://github.com/ManoelRIos/ecos-vale/tree/codex/ecos-lowpoly-fps-tps). A branch [`codex/ecos-do-vale-vertical-slice`](https://github.com/ManoelRIos/ecos-vale/tree/codex/ecos-do-vale-vertical-slice) conserva o protótipo anterior.

O projeto também mantém `HauntFps.unity` como referência da campanha híbrida 2D/FPS e `Haunt.unity` como referência do roguelike 2D original. Use **Ecos → Abrir protótipo FPS** ou **Ecos → Abrir roguelike**, com Play parado. O build inicia em `HauntLowPoly`, com as duas cenas anteriores habilitadas depois dela.

## Desenvolvimento e verificação

**O Editor Unity não está instalado no ambiente desta revisão.** Checks de fonte, regras e integração com APIs simuladas não certificam compilação no Unity, shaders, renderização, física, áudio reproduzido ou controle físico. A execução no motor e o build Windows continuam pendentes.

Veja [LOW_POLY_REBOOT.md](Documentation/LOW_POLY_REBOOT.md) para escopo e módulos, [ARCHITECTURE.md](Documentation/ARCHITECTURE.md) para a separação entre os perfis e [VALIDATION.md](Documentation/VALIDATION.md) para resultados e roteiro de verificação. O [PNG de referência dos personagens](Documentation/TODOS_OS_PERSONAGENS.png) permanece disponível para modelagem externa; ele registra a arte 2D anterior.
