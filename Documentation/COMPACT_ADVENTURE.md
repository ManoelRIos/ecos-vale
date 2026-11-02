# Vale compacto: movimento, cores e exploração

## Pedidos tratados

O usuário confirmou a renderização funcionando e pediu menos espaços vazios, mapa menor, animação melhor, mais profundidade, um menu mais elaborado e cores mais vivas. Alguns cogumelos apareciam brancos.

A revisão mantém a tecnologia e o fluxo offline, com mudanças concretas:

- O mundo lógico passa de 8192 para 1024 metros por lado. Os santuários e seus IDs permanecem no mesmo lugar, concentrados perto do início.
- A decoração usa uma grade com variação para troncos, agrupamentos de sub-bosque, trilhas e clareiras. Na seed padrão, nove chunks planejam 1150 árvores e 1108 detalhes antes das clareiras; a distância média do tronco mais próximo é 9,35 m. Esses são dados gerados, não uma contagem de renderizadores nem medição de FPS.
- A criação/retirada ocorre em lotes com orçamento por frame. O anel distante tem terreno, sem a mesma densidade de props. Pool e cache têm limites.
- O personagem tem joelhos/cotovelos, respiração, transições de passada/corrida/agachamento, curvas, salto e aterrissagem. A cápsula não é escalada; o movimento visual segue o deslocamento real. A correção dos pés usa os quatro cantos inferiores das botas, em vez de acumular deslocamentos.
- A emissão passou de 2,5 para 0,55 e saiu dos chapéus dos cogumelos. Somente detalhes pequenos brilham. A intensidade diurna foi moderada e as paletas dos seis biomas ficaram mais saturadas. Essa alteração trata uma causa concreta de saturação dos canais; o resultado visual ainda precisa de comparação no Editor.
- O menu tem um cenário 3D próprio, lanternas, viajante, movimento de câmera, título em duas linhas, cards, configurações e confirmação de nova jornada. O cenário fica em layer 30 e é desativado antes da sessão; ele não altera RenderSettings do mundo.

## Ciclo jogável

1. Reunir 4 madeiras e 3 pedras e acender a fogueira em `(-12,-9)`.
2. Usar a bancada `(-6,-14)` com 2 cristais, 3 ervas e 2 pedras para criar a lente.
3. Usar Q para marcar recursos/interações pendentes até 42 m por 5 s, com recarga de 14 s. O efeito visual é local; as recompensas passam pela autoridade.
4. Encontrar baús e fragmentos, descobrir os locais e restaurar os três ecos. Cada altar exige 1 cristal e 2 recursos temáticos: pedra, erva ou madeira.
5. Receber a recompensa única. Continuar explorando baús/fragmentos/biomas aumenta a experiência.

Há 94 coletas iniciais, onze alvos de interação fixos (incluindo os três santuários, acampamento/bancada, três baús e três livros), além de dez localidades de descoberta e seis biomas. Os recursos principais estão próximos; não é preciso cruzar o mundo inteiro para completar as receitas.

A cada 40 XP, o personagem sobe um nível e amplia a capacidade da mochila até 20 espaços. Custos e recompensas usam uma simulação da troca antes de confirmar: inventário cheio ou recursos insuficientes não geram mutações parciais. Baús, livros, locais e missão concedem suas recompensas uma única vez. Reler livros mantém a história disponível sem repetir XP.

## Persistência

`ProgressionState` e `ProgressionRules` são classes C# independentes da Unity, reutilizáveis no modelo de autoridade. `SaveData` acrescenta seed, posição, tempo e versão. O save v2 mantém fogueira, lente, XP, baús e livros; a migração de v1 preserva itens e progresso anterior. Posições que ficaram fora da área compacta retornam ao hub. O parse e a serialização real com JsonUtility têm testes EditMode escritos, ainda não executados aqui.

## Integração e escopo

O controlador cria um rig visual separado da raiz física e compartilha meshes/materiais. A lanterna real acompanha a geometria segurada pelo braço. `GameBootstrap` compõe a sessão, encaminha comandos tipados e atualiza as apresentações; não decide custos de receita. `Landmarks` cuida da distribuição/apresentação do conteúdo inicial. `ExplorationScanner` somente revela alvos pendentes e respeita recarga.

O multiplayer continua preparatório. Ainda não há combate, inimigos, sobrevivência, trilha sonora final ou matchmaking. A UI permanece em IMGUI. A decoração não ganhou colisores de árvores nesta revisão.

## Verificação

O teste .NET compila os arquivos reais de inventário, progressão e distribuição, incluindo a sequência completa até a recompensa, falhas atômicas, idempotência, migração de dados e densidade determinística. Os checks estáticos cobrem sintaxe, GUIDs e pacote de entrega.

Não foram executados Unity Editor, testes EditMode, JsonUtility, shaders, animação, interação de menu ou medição de FPS desta revisão. O usuário confirmou apenas a execução da versão anterior. Para avaliar esta, importe a atualização, escolha Nova jornada se quiser experimentar todas as etapas, complete fogueira/lente/santuários e verifique salve/continue, scanner e movimento.
