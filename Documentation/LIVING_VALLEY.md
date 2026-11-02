# Vale vivo: variedade, criaturas e caminhos

Esta entrega mantém o vale compacto e o progresso da aventura, ampliando o que se encontra durante a caminhada. A versão anterior foi executada pelo usuário; esta revisão foi verificada por código e dados, sem execução do Unity neste ambiente.

## Flora e composição

`VegetationFactory` cria quatro formas de árvore para cada um dos seis biomas: 24 silhuetas com alturas, inclinações, ramificações e copas diferentes. Entre elas estão carvalhos largos, bétulas, brotos, salgueiros, coníferas e árvores mortas ou carbonizadas. Não são apenas recolorações do mesmo modelo.

Cada família de detalhe também tem quatro variações: pedras, tocos, troncos caídos, pequenos menires, colônias de cogumelos, abóboras, flores, samambaias, caniços e arbustos. Os chapéus dos cogumelos permanecem sem emissão; o brilho fica nos detalhes pequenos.

`PropDistribution` acrescenta uma variante determinística. Microregiões deformadas favorecem famílias locais, com mistura entre indivíduos; detalhes se agrupam em pequenas manchas ao redor dos troncos. O pool distingue 96 combinações de bioma/tipo/variante, mantendo os limites de 256 objetos totais e 48 por combinação. A criação continua na thread principal sob orçamento por frame.

Na seed 71319, os nove chunks iniciais planejam 1150 árvores e 1118 detalhes antes das clareiras e corredores da rota. As quatro famílias de árvore têm 262/312/250/326 candidatos e a distância média ao tronco mais próximo é 9,35 m. Esses dados não medem FPS nem a quantidade final após os filtros de localidades. Os modelos reutilizam meshes/materiais e possuem dois níveis de LOD; o orçamento de até 12 renderers por prop tem teste Unity escrito, ainda não executado.

## Fauna

| Espécie | Comportamento inicial |
| --- | --- |
| Coelho das flores | Salta, passeia, descansa e foge da aproximação |
| Raposa de musgo | Passeia, move a cauda e investiga viajantes próximos |
| Cervo das lanternas | Passeia, descansa e foge de passos rápidos |
| Javali das pedras | Fareja, fica alerta e recua diante de uma corrida |
| Morcego violeta | Entra no mix à noite e voa baixo com asas animadas |
| Fagulha do vale | Surge à noite e se aproxima por curiosidade, com alcance maior após criar a lente |

`WildlifeDirector` mantém uma população local: 18 criaturas por padrão, máximo configurável de 24. Habitats de 28 m são escolhidos pela seed e pelo bioma em até 100 m do jogador; criação e retirada ocorrem gradualmente. Só surgem sobre terreno carregado e adequado. Movimento usa decisões por intervalo, altura amostrada e verificações simples de obstáculos na thread principal. Sem NavMesh, uma criatura pode encontrar uma passagem difícil; a validação de física ainda precisa do Editor.

Os animais tímidos percebem menos a aproximação agachada. Com Ctrl e E, registre cada espécie a menos de quatro metros. `OfflineWorldAuthority` verifica alvo, comando, distância e espécie antes de conceder 12 XP uma única vez. O scanner também revela espécies próximas ainda pendentes. O guia em I guarda descrições e hábitos depois da primeira observação.

Completar as seis espécies concede um artefato e 25 XP uma vez. A recompensa espera espaço na mochila sem consumir progresso. As criaturas não atacam, não recebem dano e não concedem loot: esta etapa é de fauna e observação. Não há sons finais de animais nem sincronização online da IA.

## Caminhos de exploração

Os dez locais anteriores permanecem. Seis novos conjuntos formam uma rota a partir do acampamento, marcada por pedras, lanternas e setas:

| Lugar | Coordenadas x/z | Conteúdo |
| --- | --- | --- |
| Carroça das Andorinhas | -112 / -35 | Fragmento de história e provisões |
| Arco das Raízes | -162 / 35 | Baú selado e cristais |
| Jardim da Lua | -185 / 118 | Fragmento, flores e cogumelos |
| Mirante do Peregrino | -70 / 212 | Baú, colunas e banco |
| Observatório Partido | 94 / 245 | Fragmento, ruínas e telescópio |
| Refúgio da Ponte Velha | 232 / 126 | Baú e restos da expedição |

O total é de 16 localidades, seis baús, seis fragmentos e 124 coletas fixas, além dos alvos temporários de fauna. Os 17 alvos fixos de interação incluem acampamento, bancada e santuários. Os novos livros indicam a próxima parte da rota; cada conjunto contém cinco recursos próximos. A ponte é uma ruína, sem sistema novo de rios ou travessia sobre água.

`ExplorationDressing` amostra a altura por peça, compartilha materiais/meshes e reduz partes distantes com LOD. Volumes estruturais usam colisores simples aproximados; folhas, plantas, marcos e lanternas ficam livres. As clareiras e o corredor da rota reservam espaço na distribuição da decoração. Colisão, transitabilidade e composição visual precisam de teste no Editor.

## Saves e arquitetura

Save v3 acrescenta `observedSpecies` e `guideRewarded`. Saves v1/v2 são migrados preservando itens, XP, baús, fragmentos e missão. Continuar recria a nova flora e os novos lugares e habilita os encontros; não exige Nova jornada. Os IDs anteriores permanecem consumidos/restaurados. Habitats e elementos estáticos são determinísticos; posições e estados transitórios dos animais não são persistidos.

O catálogo de espécies e as regras de progressão são C# puro. O diretor e a apresentação são componentes Unity. Uma futura sessão cooperativa deverá executar a IA e validar observações no host, replicando atores com estado; eles não pertencem à decoração estática reconstruída por seed. Não foi entregue host/client nesta revisão.

## Validação

Passaram 25 verificações .NET que compilam seis arquivos reais de produção, incluindo catálogo de habitats, observações, recompensas atômicas e distribuição de variantes; e 17 testes Python de versão/empacotamento. Sintaxe C#, metadados e referências locais também passaram.

Há 29 testes no conjunto do Editor, todos sem execução aqui. Incluem geometrias distintas, LOD, emissão dos cogumelos, migração v2/v3 e observação pela autoridade. Não houve importação, compilação Unity, Play Mode, medição de FPS, validação visual ou build Windows desta revisão.

Na validação manual, use Continuar, percorra a rota, observe criaturas agachado, aguarde a noite, complete o guia, salve e retome. Confira também colisões das estruturas, estabilidade da população ao atravessar chunks e o primeiro erro do Console, caso apareça.
