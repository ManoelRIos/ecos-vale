# Arquitetura

## Módulos

`Ecos.Runtime` abrange os módulos de jogo; `Ecos.Editor` depende do runtime e URP, apenas no Editor. `Ecos.Tests` depende do runtime e Unity Test Framework, apenas no Editor. Os módulos abaixo são pastas dentro do assembly runtime; não são assemblies independentes, evitando dependências circulares prematuras.

| Pasta | Responsabilidade |
| --- | --- |
| Core | Composição de sessão, configurações e persistência |
| World / Biomes | Ruído determinístico, mesh, streaming, pool e configurações ambientais |
| Art | Materiais compartilhados, primitivas e octaedro facetado |
| Player | Motor CharacterController e câmera Input System |
| Environment | Hora, iluminação, partículas climáticas e estrelas |
| Inventory | Definições estáveis, catálogo e dados serializáveis de pilhas |
| Gameplay / Quests | Alvos interativos e missão demonstrativa |
| Multiplayer | Contrato de autoridade offline e estado global NGO preparatório |
| UI / Audio | Interface inicial e canais de áudio configuráveis |
| Editor | Geração idempotente de assets ausentes, URP e build |

`GameBootstrap` compõe a sessão e encaminha entradas de UI/interação. Não gera meshes nem decide regras de inventário. `WorldSettings` e os assets de bioma/item são configuração; `SaveData` é persistência; componentes, tarefas, partículas e cache são estado temporário.

## Streaming

1. O observador local informa a célula atual. `SetObservers` permite a união dos raios de múltiplos jogadores, sem presumir uma única câmera no contrato de interesse.
2. O gerenciador cancela requisições obsoletas e devolve props de chunks descarregados ao pool.
3. `TerrainMeshGenerator` calcula arrays em até dois workers. Usa matemática, estruturas de valor e um token de cancelamento; não cria objetos Unity nem consulta Transform/Physics nos workers.
4. O resultado é instalado na thread principal. No máximo um upload de chunk por frame. O cache de arrays mantém 32 entradas; meshes fora do raio são destruídas.
5. Coordenadas globais alimentam o ruído e as diferenças centrais das normais. Vizinhos compartilham altura/cor/normal na borda. A resolução do terreno é uniforme para evitar rachaduras entre LODs diferentes.

`lastGenerationMs` mede **upload de mesh na thread principal**. `LastWorkerMs` mede a geração do worker e `LastPropFrameMs` mede a etapa de decoração no frame. Pool e cache pertencem à sessão e são descartados ao iniciar outra. Mudanças de seed devem iniciar uma nova sessão. O interesse de múltiplos observadores é uma capacidade de dados; não significa que personagens remotos já existem.

## Interações e multiplayer futuro

O cliente local seleciona um ID e envia `WorldCommand` à autoridade offline. A autoridade resolve o ID no registro, valida distância, idempotência e inventário antes de consumir/conceder algo. IDs de coletas e santuários são estáveis na sessão.

Na integração NGO, implementar um gateway de RPCs que obtenha o jogador pela identidade de conexão e sua posição autoritativa. **Nunca aceitar a posição enviada pelo cliente como prova de alcance**, nem permitir ao cliente escolher quantidade/recompensa. O gateway deve aplicar rate limiting, validar propriedade de inventário e publicar apenas mudanças confirmadas.

`SessionState` é um NetworkBehaviour preparatório: seed e hora têm escrita exclusiva do servidor. Não está anexado a um NetworkObject na cena e não inicia rede. Um próximo marco deve configurar NetworkManager + UnityTransport, prefab de jogador com ownership, snapshots/clock interpolado e ciclo de conexão/reconexão.

Árvores/rochas decorativas são reconstruídas localmente pela seed. Coletas, missões e mudanças de estruturas precisam de replicação por ID. O save atual é um agregado offline; uma sessão cooperativa deverá separar estado global da sessão e inventário/posição por identidade de jogador. Não há sincronização de objetos decorativos.

## Persistência e recursos

O save v2 (com migração de v1) guarda o estado independente dos assets. O arquivo anterior é copiado para `.bak`; um temporário é substituído sobre o arquivo principal. Versões desconhecidas e estruturas inválidas falham com mensagem, sem sobrescrever o arquivo ao carregar. A recuperação do `.bak` é manual nesta versão.

A fábrica usa meshes próprios e primitivas nativas, materiais URP compartilhados e luzes sem assets externos. Os prefabs de referência estão serializados no projeto. O pipeline URP é criado pelo Editor para usar a serialização oficial da versão instalada, sem inventar referências internas do renderer. Sons e mixer podem ser atribuídos ao sistema de áudio, mas nenhum conteúdo sonoro foi entregue.

## Preparação de renderização

`RenderingSetup` aplica o `ValleyURP` dos Resources antes da sessão e após mudanças de qualidade. O Editor também valida pipeline/renderer e shaders dos materiais antes de Play. O reparo explícito renova somente os recursos gerados mantendo suas identidades de asset e valores básicos de qualidade; o diagnóstico expõe o estado e mensagens de shader. `WorldClock` mantém uma instância do material do céu durante a sessão, atualizada pelo horário e descartada ao encerrar. As novas meshes de props são compartilhadas e preservadas ao salvar prefabs pelo Editor.

## Vale compacto e progressão

O padrão atual é 1024 m com um anel extra de terreno. `PropDistribution` produz dados puros determinísticos; `ChunkManager` cria até 8 candidatos/frame sob orçamento flexível de 2 ms e retira objetos progressivamente. O pool tem limite global 256 e 48 por tipo. A malha distante permanece sem povoar todos os chunks.

`ProgressionState` contém inventário e progresso serializáveis; `SaveData` herda esses campos e acrescenta dados do mundo. `ProgressionRules` executa receitas atômicas, XP e recompensas únicas sem Unity API. `OfflineWorldAuthority` valida tipo, distância e alvo antes de chamar essas regras. Descobertas offline também usam a mesma regra pura; uma futura sessão online deve encaminhar a descoberta validada pelo host.

`PlayerVisualAnimator` mantém um rig rígido de duas partes por membro sob `VisualRoot`, separado do CharacterController. `MenuBackdrop` tem câmera/luzes em layer própria e é desativado antes do jogo. O scanner é uma apresentação local, sem conceder loot diretamente.
