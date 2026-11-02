# Validação realizada

## Resultados executados neste ambiente

| Verificação | Resultado |
| --- | --- |
| SDK .NET 8.0.427 instalado de fonte oficial via TLS | Concluído |
| Compilação e execução de `Tools/CoreChecks.csproj` | **25 verificações passaram** |
| Parser C# tree-sitter 0.26.0 / grammar 0.23.5 | **46 arquivos sem erro sintático** |
| Testes Python de versão e empacotamento | **17 passaram (8 + 9)** |
| Estrutura, versões diretas e evidências de manifests | Passou |
| Assets e metadados locais | **83 arquivos de assets; 103 GUIDs únicos** |
| Referências locais de GUID, cena/prefab e IDs internos | Passou |
| `bash -n Tools/setup-cloud.sh Tools/unity-check.sh` | Passou |
| Script completo `Tools/setup-cloud.sh` | Executado com sucesso; reutilizou SDK e executou o núcleo disponível naquela etapa; suíte atual ampliada para 25 |

As verificações .NET compilam os arquivos reais de produção `WorldMath.cs`, `InventoryState.cs`, `ProgressionState.cs`, `ProgressionRules.cs`, `CreatureCatalog.cs` e `PropDistribution.cs`, sem stubs Unity. Cobrem repetibilidade/seed, alturas finitas, seis biomas na área inicial de 1 km, continuidade do ruído, igualdade das alturas nas bordas, intervalo aleatório, empilhamento, atomicidade em overflow/remoção, entradas inválidas, custos completos da campanha, recompensas únicas, migração do progresso, distribuição densa, quatro variantes, habitats diurnos/noturnos, observação e recompensa do guia.

A checagem de GUIDs permite somente os shaders URP inspecionados e meshes nativos explícitos como referências externas. Ela verifica que as referências locais existem; não comprova que a Unity importou/renderizou esses assets.

A consulta Git HTTPS, fora do sandbox de comandos, funcionou e confirmou que o repositório remoto não anunciava refs/commits. A consulta inicial dentro do sandbox não alcançava o proxy. Nenhum token adicional foi necessário.

## Verificações não executadas

**Não há Unity Editor neste ambiente.** Portanto, não houve resolução UPM, compilação dos assemblies Unity, compilação de shader, importação nativa dos YAMLs, teste de cena em Play Mode, build Windows ou medição de FPS.

Vinte e nove testes do conjunto do Editor estão implementados em `Assets/_Game/Tests/Editor/WorldTests.cs`, `ProjectSetupTests.cs`, `PropDistributionTests.cs`, `PlayerVisualAnimatorTests.cs` e `VegetationTests.cs`, **todos ainda não executados aqui**. Um deles é uma coroutine que entra e sai do Play Mode:

1. Igualdade dos vértices, normais e cores de chunks vizinhos.
2. Cancelamento da geração.
3. Contagem/índices de triângulos válidos.
4. Atomicidade do inventário cheio.
5. Recompensa da missão uma única vez.
6. Round-trip JSON do progresso.
7. Autoridade rejeita distância indevida e coleta duplicada.
8. Fluxo de acampamento, lente e três restaurações pela autoridade até a recompensa.
9. Criação idempotente do pipeline preservando configurações válidas.
10. Reparo de renderer ausente preservando o GUID do pipeline.
11. Reparo de material preservando GUID e cor base.
12. Atualização explícita de recursos preservando GUIDs e valores básicos de qualidade.
13. Reutilização dos meshes visuais e faces orientadas para fora, sem triângulos degenerados.
14. Distribuição determinística e limites em coordenadas negativas.
15. Densidade do bosque inicial e clareza das trilhas.
16. Orçamento zero de decoração.
17. Cancelamento do planejamento de decoração.
18. Migração JsonUtility do save v1.
19. Round-trip v3 com campos herdados e guia de campo.
20. Rig sem drift ou escala na raiz física.
21. Inicialização idempotente do rig.
22. Abertura de cena durante Play retorna sem exceção nem substituição da cena ativa; validação dos comandos retorna falso.
23. Quatro variantes de cada família aparecem no bosque inicial.
24. Densidade zero não decora chunks negativos.
25. Rejeição de orçamento inválido e densidade não finita.
26. Migração de save v2 preserva a aventura e inicia o guia vazio.
27. Autoridade impede observar à distância, trocar o comando e repetir XP em outro animal da mesma espécie.
28. Quatro geometrias de árvore por bioma, meshes/materiais compartilhados e LOD limitado.
29. Quatro formas de detalhes por família e chapéus de cogumelo sem emissão.

Não há teste de Play Mode executado ou executável Windows. A meta de 60 FPS não foi avaliada. Ausência de erros sintáticos não equivale a compilação de APIs Unity bem-sucedida.

## Próxima validação obrigatória no Editor

1. Aplicar as regras de rede do rascunho: `packages.unity.com` e `download.packages.unity.com`. Elas foram salvas; a tentativa no runtime atual retornou HTTP 403, antes de qualquer aplicação/publicação.
2. Disponibilizar Unity 6000.0.85f1 licenciado e resolver UPM. Usar `Tools/unity-check.sh import` e ler o log.
3. Executar `Tools/unity-check.sh tests`. Confirmar vinte e nove testes executados e nenhum falhando no XML atual.
4. Abrir Valley, iniciar Novo jogo, testar câmera/terreno, percorrer os três locais, construir o acampamento, criar a lente, coletar os materiais e restaurar os três altares e confirmar um artefato. Verificar inventário cheio, descarte, mapa e ciclo ambiental.
5. Percorrer a rota dos seis novos locais, verificar colisões estruturais, observar as seis espécies (incluindo noite), completar o guia e conferir que não há repetição de XP. Sair e usar Continuar; confirmar seed, posição, inventário, observações e objetos consumidos preservados. Testar arquivo inválido sem sobrescrever o save válido de backup.
6. Fazer build com o módulo Windows e testar o executável em Windows. Medir CPU/GPU, alocações e frames na máquina alvo antes de avaliar a meta de performance.

## Configuração reutilizável

Foram salvos no rascunho do ambiente `install_script` (executa `Tools/setup-cloud.sh`) e `start_skill` (diretório, isolamento sem worktrees, verificações e inicialização Unity condicionada à presença do Editor). Também foram adicionados os dois domínios Unity à lista customizada, preservando o preset existente.

O rascunho foi persistido com sucesso. Salvar o rascunho **não aplica rede, não instala Unity e não publica** o ambiente. Revisar e salvar nas configurações do ambiente e publicar são ações posteriores do produto. A restauração em uma nova tarefa não foi testada.

## Atualização de versão e inicialização

- Pin atualizado de 6000.0.40f1 (afetado pela CVE-2025-59489) para 6000.0.85f1 (posterior ao patch corretivo 6000.0.58f2 da linha 6.0).
- A existência do patch foi conferida no repositório oficial UnityCsReference; o uso de URP 17.0.4 em um projeto oficial 6.0 também foi conferido. Evidência em UnityCompatibility.json.
- Oito testes Python de política de versão passaram, incluindo rejeição da versão vulnerável, limite 58f1/58f2, linha errada e patch inferior ao fixado.
- Inicialização URP revisada para usar a fábrica pública e recuperar configurações parciais. Isso não foi executado no Editor neste ambiente.
- A versão antiga foi substituída; o log posteriormente enviado pelo usuário confirma Editor 6000.0.85f1. O diagnóstico de compilação agora está identificado abaixo.

## Diagnóstico confirmado pelo log do usuário

O log do Editor recebido nesta etapa mostra resolução UPM concluída com Netcode 2.0.0, Input System 1.11.2, Cinemachine 3.1.0, Splines 2.9.1 e URP 17.0.4, no Unity 6000.0.85f1. Essa é evidência da máquina do usuário, não execução do Editor no ambiente cloud.

Foram identificados três erros C# distintos `CS1069` em `NetworkRigidBodyBase.cs` (linhas 53, 89, 691), repetidos pelo compilador/Console: faltam `Rigidbody2D` e `FixedJoint2D`. A causa é a ausência de `com.unity.modules.physics2d` no manifest. Esse módulo foi adicionado na versão 1.0.0 e o validador passou a exigir os dois módulos de física para NGO 2.0.0.

A fonte oficial da tag `ngo/2.0.0` confirma que a classe `NetworkRigidbodyBase` referencia os dois módulos quando qualquer um deles está habilitado. As falhas posteriores de carregamento de Collections/Splines/InputSystem são candidatas a efeitos em cascata; não foram tratadas como incompatibilidades independentes sem nova recompilação.

A validação estática do projeto corrigido passou. Um teste negativo em uma cópia temporária sem Physics 2D confirmou que o validador rejeita a configuração original. Não houve recompilação Unity nem confirmação de Play Mode após esta correção; isso precisa ser confirmado na máquina do usuário. O log original não foi incluído no Git.

## Segundo log e pacote de abertura

A segunda sessão enviada pelo usuário ainda registrava 29 pacotes, sem Physics 2D, sem `COM_UNITY_MODULES_PHYSICS2D` e sem `UnityEngine.Physics2DModule.dll` nos argumentos do compilador. Os três erros C# de NGO são os mesmos; o log não confirma aplicação do manifest corrigido. Não foi identificado outro erro C# independente na revisão completa dos 29 scripts e das referências de assembly.

O pacote passou a incluir um atalho Windows que confere a própria pasta e inicia o Editor com `-projectPath` explícito, além de instruções de extração nova. A estrutura do atalho, seus rótulos e verificações anteriores ao lançamento receberam revisão estática. **O atalho não foi executado no Windows.** A tentativa de instalar um PowerShell auxiliar de teste foi rejeitada pela revisão automática por não ser necessária ao diagnóstico; a restrição foi respeitada e nenhuma instalação alternativa foi feita.

A validação estática, os testes de política de versão e os testes do núcleo continuam separados da validação Unity. Não há comprovação de compilação ou Play Mode após a correção na máquina do usuário.

## Terceiro log e correção da entrega para caminhos curtos

A sessão mais recente já contém Physics 2D e 30 pacotes resolvidos, sem erros C# `CS...`. Foram identificados 27 arquivos distintos ausentes, repetidos 58 vezes no log, todos em caminhos de 260–288 caracteres. A raiz aninhada do projeto tem 130 caracteres. Não houve reprodução do limite em Windows; a hipótese é fortemente sustentada por esses comprimentos e pela coincidência dos arquivos usados nos erros Burst/URP.

A distribuição passou a usar `Distribution/EcosDoVale.zip` com uma raiz curta `EcosDoVale`, sem Library, caches ou distribuição recursiva. O gerador `Tools/package_project.py` usa apenas arquivos rastreados no índice Git e valida Editor/Physics 2D antes de empacotar. O atalho recusa raiz acima de 60 caracteres antes de abrir o Editor. A recuperação documentada preserva o cache anterior renomeando Library somente após mover o projeto para um caminho curto.

Os testes do empacotador verificam arquivos necessários, ausência de cache/ZIP recursivo, integridade e reprodução determinística. Isso não equivale à validação do importador Windows nem do Play Mode, que continuam pendentes após esta alteração. O log original do usuário não foi incluído no Git.

## Revisão de renderização e arte

A screenshot do usuário mostra magenta e terreno não legível. O usuário informou ausência de erros vermelhos; não foi recebido um diagnóstico do pipeline ativo. O código agora verifica seleção URP/renderer, repara shaders de materiais existentes, prepara o Editor antes de Play e reafirma a seleção em runtime/qualidade. Novos comandos de reparo e diagnóstico ajudam a distinguir configuração de falha de shader.

Foram implementados céu gradual, iluminação/névoa revisadas, novo personagem e vegetação facetada por bioma, LOD de copas preservado e melhorias de legibilidade da UI. A verificação numérica de Cone/Trunk/Crown/MushroomCap passou com 14/24/42/48 triângulos, orientação externa e área positiva. Esses checks não renderizam os meshes nem compilam HLSL.

O total de testes EditMode passou para 13, ainda todos sem execução aqui. A compilação completa dos novos scripts, shaders, renderização e desempenho no Editor não foram confirmados. Detalhes em VISUAL_UPDATE.md.

## Revisão compacta e progressão

A suíte .NET atual compila cinco arquivos reais de produção (WorldMath, InventoryState, ProgressionState, ProgressionRules e PropDistribution), além do runner sem stubs Unity. Vinte verificações passaram: inclui campanha completa com orçamento exato, bloqueio da lente antes do acampamento, custos/recompensas atômicos, baús e XP únicos, IDs de santuário, migração de dados, planos determinísticos/densos, trilhas, cancelamento e limites.

Na seed padrão, nove chunks planejaram 1150 árvores e 1108 detalhes, com distância média do tronco mais próximo de 9,35 m antes das clareiras. Essa métrica de dados não comprova FPS. A criação da cena e a reserva de 2 ms são verificações/limites de código, não um benchmark Unity.

A aparência branca dos cogumelos foi tratada reduzindo emissão e retirando brilho dos chapéus. Menu 3D, HUD, rig animado, scanner e JsonUtility foram revisados por fonte e possuem testes de Editor pertinentes. **Não foram executados nesta máquina.** Os 21 testes Unity continuam pendentes e a nova revisão precisa de validação visual/funcional no Editor. A versão anterior foi confirmada funcionando pelo usuário.

## Exceção ao abrir a cena durante Play

O log posterior à atualização compacta registra compilação e pós-processamento de `Ecos.Runtime`, `Ecos.Editor` e `Ecos.Tests` no Editor do usuário, sem mensagens `error CS...`. Isso é evidência dessa sessão, não execução cloud nem validação da correção abaixo.

A única exceção C# identificada é `InvalidOperationException: This cannot be used during play mode`, com stack em `ProjectSetup.Open()` → `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()`. O comando de abrir a cena estava disponível durante Play e não verificava o estado do Editor. Agora os três comandos que editam o projeto possuem validação de menu; a abertura também verifica o estado antes de chamar qualquer API de cena. Chamadas diretas durante Play retornam com um aviso sem alterar a cena ou parar o jogo.

Foi adicionado um teste Unity de regressão que entra no Play, chama a abertura e verifica ausência de exceção e preservação da cena. O total passa para 22 testes do Editor, ainda sem execução aqui. A validação estática cobre a sintaxe e as referências; não substitui a execução desse teste.

O mesmo log contém uma assertion `device.IsInsideFrame()` e um aviso de GUI antes de concluir o carregamento do Editor e antes do primeiro reload para Play. Não há stack associada ao `GameUI` nem evidência suficiente para atribuir essa assertion à interface do jogo. Ela não foi reproduzida ou declarada corrigida. Avisos de importação de shaders Terrain/SpeedTree não utilizados pelo jogo também aparecem, sem falha de shader do jogo identificada.

## Vale vivo e variedade

A suíte atual passou com 25 verificações .NET. O catálogo de habitats real foi amostrado quanto a seed, repetibilidade, quatro espécies diurnas e seis no mix noturno. A observação e o guia completo têm recompensas únicas e atômicas verificadas, incluindo falha com inventário cheio e tentativa posterior. Isso não executa as decisões de IA, Physics ou modelos Unity.

Os planos dos nove chunks iniciais mantêm 1150 árvores, agora divididas em quatro famílias (262/312/250/326), e 1118 detalhes. A distância média ao tronco mais próximo continua 9,35 m antes dos filtros de clareiras/corredores. Os testes .NET verificam limites e cobertura das variantes; a distinção geométrica das 24 árvores tem teste Unity escrito e não executado.

Foram revisados por fonte o diretor de fauna, animação dos animais, transições de alerta/curiosidade, registro/remoção de alvos, integração do scanner/autoridade e migração v3. Novos conjuntos têm colisores estruturais aproximados. Nenhuma dessas verificações estáticas comprova física, aparência, compilação de APIs Unity ou desempenho. O total de 29 testes do Editor continua sem execução neste ambiente; detalhes do conteúdo em LIVING_VALLEY.md.
