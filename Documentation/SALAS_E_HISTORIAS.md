# Salas irregulares, Halloween e histórias

## Formatos e física

`Core/RoomLayout.cs` produz L, T, H, octógono e alcovas a partir da semente, andar, sala e tipo. A máscara usa células de uma unidade em um limite de 29×17. Entrada e chefe usam octógono; salas comuns variam e podem receber abismos internos. Núcleo e aproximações cardinais ficam conectados, mantendo as posições das portas da árvore de andares. Por isso L/T podem ter corredores adicionais até as saídas.

A máscara não é uma decoração sobre uma arena quadrada. As áreas vazias são agrupadas em retângulos e inseridas nos mesmos obstáculos consultados por movimento, tiro, raio e linha de visão. Abismos têm sua própria camada: podem ser saltados, mas criaturas terrestres não os atravessam. O gerador testa conectividade segura antes de aceitar os buracos.

`HauntGame.Rooms.cs` instancia chão e contornos, faces e coroamentos de paredes, decoração e miniaturas do mapa. Spawn considera o raio do corpo e cobertura real. A navegação usa um próximo passo BFS com cache, ajustando destinos para células inteiras com espaço suficiente; uma busca local considera também pilares e outros obstáculos dinâmicos. Somente a Sombra ignora paredes deliberadamente.

## Barris, pilares e dano persistente

As posições dos objetos são escolhidas por uma sequência separada da composição das ondas. Rotas centrais e pontes ficam livres. Reservar os lugares dos objetos destruídos durante a reconstrução impede que sua ausência faça outro objeto surgir na revisita. O estado é da incursão e reinicia em um novo jogo.

| Objeto | Aparência e comportamento |
| --- | --- |
| Explosivo | Madeira abaulada, cintas, caveira e pavio animado; fusível de 0,55 s, explosão com dano, empurrão, cobertura respeitada e trauma |
| Ácido | Metal amassado e transbordamento neon; dois pontos de vida; poça verde por 8 s, primeiro pulso após 0,75 s e novos pulsos nessa cadência |
| Pilar | Sete pontos de vida e quatro estados de rachadura; a destruição remove sua caixa de colisão |
| Urna | Preserva a água com lentidão temporária da versão anterior |

Ácido atinge jogador e criaturas vulneráveis. A proteção após sofrer dano pode impedir pulsos consecutivos no jogador; saltar evita contato com o chão perigoso. Brasas do morcego afetam o jogador por um período curto. As áreas respeitam cobertura e não surgem sobre vazio/abismo. Há limite de 16 zonas; ácido tem prioridade sobre brasas para evitar perda do efeito ao quebrar um barril.

## Criaturas e heróis

| Tipo | Comportamento |
| --- | --- |
| Espantalho Amaldiçoado | Imóvel e disfarçado até distância menor que 4,5 ou receber dano; avança e lança três lâminas giratórias com ricochete |
| Gárgula de Pedra | Ciclo de 1,5 s imune em pedra e 1,5 s em voo; leque de fogo com aviso; pedra resiste também ao empurrão da luva |
| Sombra Perseguidora | Espírito translúcido com perseguição direta através de paredes e tiros sombrios |
| Morcego de Fogo | Zigue-zague rápido, brasas temporárias e quatro tiros anunciados ao morrer se o combate continuar |

`HauntGame.Bestiary.cs` concentra essas regras, enquanto o gerenciador de padrões mantém os avisos e o cancelamento por onda. O catálogo total tem 17 criaturas comuns, um miniboss e sete identidades de boss.

Os quatro heróis preservam poderes e armas iniciais, com arte aprimorada: capa animada, cajado e orbes, foice e arco de corte, frascos e fumaça. A arte é procedural e original, inspirada na referência de clima/silhuetas. `IRREGULAR_HALLOWEEN.png` mostra pixels reais do código, sem renderização do Unity.

## Estados e apresentação dos chefes

`HauntPixelArt.BossFrame(id, BossAnimationState, frame)` gera quatro quadros para cada um dos cinco estados dos sete chefes: Idle, Telegraph, Attack, Enrage e Death. O cache separa estado e quadro, sem substituir a API legada `Frame`. Aviso, execução e vida escolhem o estado; abaixo de 50% há aura e maior mobilidade/cadência. A morte mantém uma apresentação separada por 0,9 s, sem manter o inimigo vivo ou bloquear a recompensa.

Ao entrar em uma sala de chefe não purificada, `HauntGame.Conversations.cs` assume o controle por três segundos. O boss aparece imediatamente, a câmera desliza por 0,85 s e há tarjas, retrato, título, subtítulo e provocação. E revela/avança o texto; 1/2 ou controle escolhem desafiar ou perguntar sua história. As escolhas mudam a fala, sem abreviar os três segundos. Pausar congela esse relógio.

A simulação retorna antes de movimento, IA, poder, tiros e perigos enquanto a intro ou uma conversa estiverem ativos. Ao encerrar, input é consumido naquele frame e a câmera retorna ao acompanhamento sem posição herdada de outra sala.

## Atendente e música

Interagir a até 2,8 unidades abre a conversa com o Coletor de Almas, com retrato, letras graduais e blips procedurais. NEGOCIAR abre o catálogo; outras respostas oferecem dica, história e despedida. E completa o texto antes de avançar, 1/2 escolhem diretamente, direcional/X navega no controle, Esc/Círculo fecha. A mesma pressão não negocia e compra.

O áudio possui cinco temas ambientes a 112 BPM e um exclusivo de chefe a 144 BPM. Ao entrar, o ambiente desaparece em cerca de 0,18 s; impacto e fala iniciam a apresentação, e o tema do chefe entra em torno de 0,3 s com fade curto. Continua durante a luta e só retorna ao ambiente após a vitória. Morte apaga o tema sem reativar o ambiente até o menu. Vozes têm quatro timbres, alturas cacheadas e intervalo mínimo de 35 ms, respeitando mute, pausa e foco.

## Validação

Passaram 2.506.067 asserções do núcleo real C#, incluindo 645 arenas e 2.436 andares; 24 testes Python; sintaxe e metadados de 28 arquivos C#, 34 assets e 44 GUIDs. Um helper externo compilou todos os arquivos reais de runtime/core/art com tipos Unity simulados e passou 372 verificações dos fluxos. Helpers específicos conferiram 140 poses de bosses, áudio PCM, conversas, criaturas e navegação.

O Editor Unity não está instalado neste ambiente. Os resultados não substituem importação, compilação no Unity, shader/renderização, leitura visual, audição e teste de controle no PC. O roteiro completo fica em `VALIDATION.md`.
