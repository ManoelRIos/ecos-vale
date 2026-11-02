# Validação realizada

## Resultados executados neste ambiente

| Verificação | Resultado |
| --- | --- |
| SDK .NET 8.0.427 instalado de fonte oficial via TLS | Concluído |
| Compilação e execução de `Tools/CoreChecks.csproj` | **12 verificações passaram** |
| Parser C# tree-sitter 0.26.0 / grammar 0.23.5 | **29 arquivos sem erro sintático** |
| Estrutura, versões diretas e evidências de manifests | Passou |
| Assets e metadados locais | **62 arquivos de assets; 82 GUIDs únicos** |
| Referências locais de GUID, cena/prefab e IDs internos | Passou |
| `bash -n Tools/setup-cloud.sh Tools/unity-check.sh` | Passou |
| Script completo `Tools/setup-cloud.sh` | Executado com sucesso; reutilizou SDK e repetiu as 12 verificações |

As verificações .NET compilam os arquivos reais de produção `WorldMath.cs` e `InventoryState.cs`, sem stubs Unity. Cobrem repetibilidade/seed, alturas finitas, seis biomas na área inicial de 1 km, continuidade do ruído, igualdade das alturas nas bordas, intervalo aleatório, empilhamento, atomicidade em overflow/remoção, entradas inválidas, orçamento de três cristais e chão inicial/limites.

A checagem de GUIDs permite somente os shaders URP inspecionados e meshes nativos explícitos como referências externas. Ela verifica que as referências locais existem; não comprova que a Unity importou/renderizou esses assets.

A consulta Git HTTPS, fora do sandbox de comandos, funcionou e confirmou que o repositório remoto não anunciava refs/commits. A consulta inicial dentro do sandbox não alcançava o proxy. Nenhum token adicional foi necessário.

## Verificações não executadas

**Não há Unity Editor neste ambiente.** Portanto, não houve resolução UPM, compilação dos assemblies Unity, compilação de shader, importação nativa dos YAMLs, teste de cena em Play Mode, build Windows ou medição de FPS.

Dez testes EditMode estão implementados em `Assets/_Game/Tests/Editor/WorldTests.cs` e `ProjectSetupTests.cs`, **todos ainda não executados**:

1. Igualdade dos vértices, normais e cores de chunks vizinhos.
2. Cancelamento da geração.
3. Contagem/índices de triângulos válidos.
4. Atomicidade do inventário cheio.
5. Recompensa da missão uma única vez.
6. Round-trip JSON do progresso.
7. Autoridade rejeita distância indevida e coleta duplicada.
8. Fluxo de três coletas/restaurações pela autoridade até a recompensa.
9. Criação idempotente do pipeline preservando configurações válidas.
10. Reparo de renderer ausente preservando o GUID do pipeline.

Não há teste de Play Mode executado ou executável Windows. A meta de 60 FPS não foi avaliada. Ausência de erros sintáticos não equivale a compilação de APIs Unity bem-sucedida.

## Próxima validação obrigatória no Editor

1. Aplicar as regras de rede do rascunho: `packages.unity.com` e `download.packages.unity.com`. Elas foram salvas; a tentativa no runtime atual retornou HTTP 403, antes de qualquer aplicação/publicação.
2. Disponibilizar Unity 6000.0.85f1 licenciado e resolver UPM. Usar `Tools/unity-check.sh import` e ler o log.
3. Executar `Tools/unity-check.sh tests`. Confirmar dez testes executados e nenhum falhando no XML atual.
4. Abrir Valley, iniciar Novo jogo, testar câmera/terreno, percorrer os três locais, coletar cristais, restaurar os três altares e confirmar um artefato. Verificar inventário cheio, descarte, mapa e ciclo ambiental.
5. Sair e usar Continuar; confirmar seed, posição, inventário e objetos consumidos preservados. Testar arquivo inválido sem sobrescrever o save válido de backup.
6. Fazer build com o módulo Windows e testar o executável em Windows. Medir CPU/GPU, alocações e frames na máquina alvo antes de avaliar a meta de performance.

## Configuração reutilizável

Foram salvos no rascunho do ambiente `install_script` (executa `Tools/setup-cloud.sh`) e `start_skill` (diretório, isolamento sem worktrees, verificações e inicialização Unity condicionada à presença do Editor). Também foram adicionados os dois domínios Unity à lista customizada, preservando o preset existente.

O rascunho foi persistido com sucesso. Salvar o rascunho **não aplica rede, não instala Unity e não publica** o ambiente. Revisar e salvar nas configurações do ambiente e publicar são ações posteriores do produto. A restauração em uma nova tarefa não foi testada.

## Atualização de versão e inicialização

- Pin atualizado de 6000.0.40f1 (afetado pela CVE-2025-59489) para 6000.0.85f1 (posterior ao patch corretivo 6000.0.58f2 da linha 6.0).
- A existência do patch foi conferida no repositório oficial UnityCsReference; o uso de URP 17.0.4 em um projeto oficial 6.0 também foi conferido. Evidência em UnityCompatibility.json.
- Oito testes Python de política de versão passaram, incluindo rejeição da versão vulnerável, limite 58f1/58f2, linha errada e patch inferior ao fixado.
- Inicialização URP revisada para usar a fábrica pública e recuperar configurações parciais. Isso não foi executado no Editor neste ambiente.
- O usuário confirmou Editor 6000.0.40f1 e a janela genérica de Safe Mode. As mensagens concretas do Console ainda são necessárias para atribuir o erro de compilação; o alerta de segurança é confirmado, os demais erros não foram reproduzidos.
