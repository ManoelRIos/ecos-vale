# Editor, segurança e erros de abertura

## Alerta confirmado e correção

A primeira entrega fixava **6000.0.40f1**, que está na faixa afetada pela **CVE-2025-59489**. O registro CVE informa que, na linha 6.0 LTS, a correção começa em **6000.0.58f2**. O alerta reportado é consistente com essa versão antiga; não deve ser ignorado.

O projeto agora fixa **6000.0.85f1**, cuja existência foi conferida na tag oficial `Unity-Technologies/UnityCsReference/6000.0.85f1`. Isso trata essa vulnerabilidade conhecida; não é uma certificação de ausência de outras falhas. Alterar ProjectVersion.txt não atualiza o Editor instalado e não corrige executáveis já produzidos: esses exigem rebuild com um Editor corrigido.

Fontes verificadas:

- [Registro CVE](https://github.com/CVEProject/cvelistV5/blob/main/cves/2025/59xxx/CVE-2025-59489.json)
- [Referência oficial do Editor 6000.0.85f1](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.0.85f1/README.md)
- [Aviso do fornecedor](https://unity.com/security/sept-2025-01), referenciado pelo registro CVE. O acesso direto a esse domínio foi bloqueado pela política de rede deste ambiente; não foi usado para simular uma consulta bem-sucedida.

## Como abrir a versão corrigida

1. Feche o Unity e baixe novamente o ZIP da branch `codex/ecos-do-vale-vertical-slice`.
2. Extraia em uma **pasta nova**, preservando sua cópia anterior caso tenha feito alterações. Isso evita reutilizar caches de uma importação feita com outra versão.
3. No Unity Hub, instale **6000.0.85f1**, da linha **Unity 6.0 LTS**. Se necessário, use o [arquivo oficial de releases](https://unity.com/releases/editor/archive). Instale Windows Build Support para gerar o executável.
4. Em Projects, adicione a pasta que contém `Assets`, `Packages` e `ProjectSettings`; selecione explicitamente o Editor 6000.0.85f1. Não selecione a pasta externa que apenas contém o projeto extraído.
5. Aguarde a resolução dos pacotes/importação. Em seguida, use **Ecos → Abrir cena principal**, abra `Valley.unity` e entre em Play Mode.

Não é necessário apagar `Assets`, alterar DLLs, desabilitar validação TLS ou forçar versões aleatórias dos pacotes.

## O que foi corrigido adicionalmente

A inicialização URP usava criação genérica de ScriptableObject e alterava a lista interna do renderer. Ela agora usa a fábrica pública `UniversalRenderPipelineAsset.Create(renderer)`, que também inicializa recursos do pacote. A configuração automática foi reforçada para reagendar durante compilação/importação, reparar assets faltantes/configuração parcial e criar os dois prefabs independentemente.

O URP permanece em **17.0.4**. Além do manifest-fonte, foi conferido um projeto oficial concreto: `Unity-Technologies/arfoundation-samples`, branch 6.0, usa URP 17.0.4 com Editor 6000.0.70f1. Essa é evidência adicional de uso na linha 6.0, mas não substitui a resolução UPM e os testes deste projeto. As referências estão em `UnityCompatibility.json`.

O script `Tools/unity-check.sh` verifica o Editor selecionado antes de importar/testar/buildar. A validação estática também rejeita um pin na faixa afetada da CVE ou fora da linha suportada.

## Erro confirmado pelo log: Physics 2D ausente

O log enviado pelo usuário confirma Editor **6000.0.85f1**, resolução de pacotes concluída e URP **17.0.4** registrado. A versão do Editor já está atualizada, mas o Play Mode continua bloqueado por três erros `CS1069` do **Netcode for GameObjects 2.0.0**, em `NetworkRigidBodyBase.cs`:

- Linha 53: tipo `Rigidbody2D` ausente.
- Linha 89: tipo `Rigidbody2D` ausente.
- Linha 691: tipo `FixedJoint2D` ausente.

A própria mensagem da Unity recomenda habilitar o pacote nativo **Physics 2D**. Embora o jogo seja 3D, essa versão do Netcode compila uma classe que referencia a física 2D e 3D quando qualquer um desses módulos está habilitado. A correção adiciona `"com.unity.modules.physics2d": "1.0.0"` ao manifest, junto ao módulo de física 3D já existente. Não é necessário editar arquivos em `Library/PackageCache` ou trocar de versão do Netcode para esse erro.

A versão corrigida pode ser baixada novamente da branch. Para corrigir a cópia atual, habilite **Physics 2D** na lista de pacotes **Built-in** do Package Manager e aguarde a resolução/recompilação. Depois abra `Assets/_Game/Scenes/Valley.unity` e tente Play novamente.

As falhas posteriores de carregamento de `Unity.Collections.CodeGen`, `SplineComponent` e `InputActionAsset` aparecem após a compilação interrompida. Devem ser reavaliadas depois da correção principal, sem desabilitar validação de referências nem trocar pacotes arbitrariamente. Não foi confirmado que sejam problemas independentes.

Há também aviso sobre extensão `WindowsStandalone`; o suporte de build Windows deve ser conferido antes de gerar um executável. Esse aviso não explica os erros `CS1069` de Physics 2D.

O arquivo original de log contém dados locais e não é versionado no repositório. Esta seção guarda apenas o diagnóstico técnico necessário. Ainda não foi recebido um log após a adição de Physics 2D, portanto a compilação final e o Play Mode permanecem sem confirmação.

Se ocorrer outro erro após recompilar, o log no Windows fica em `%LOCALAPPDATA%\Unity\Editor\Editor.log`. A primeira mensagem vermelha do Console, incluindo código e arquivo, ajuda a identificar a próxima causa.

## Segundo log: o estado aberto ainda não inclui a correção

O segundo log repete os mesmos três `CS1069`. A lista de pacotes resolvidos tem 29 entradas e inclui a física 3D, mas não `com.unity.modules.physics2d`. A invocação do compilador também não contém `COM_UNITY_MODULES_PHYSICS2D` nem a referência `UnityEngine.Physics2DModule.dll`. Isso confirma que o módulo continuava ausente no estado carregado pela sessão; não demonstra que o manifest corrigido foi aplicado e falhou.

O log não diferencia cópia antiga, checkout/manifest não sincronizado e estado persistido de pacotes. Por isso, o novo pacote inclui `ABRIR_ECOS_DO_VALE.cmd`, que confere os arquivos da própria pasta e a abre explicitamente. Use uma extração nova e mantenha a cópia anterior se houver alterações suas. Nenhum cache ou arquivo do usuário é removido pelo atalho.

A revisão completa dos 29 scripts e das referências de assembly não identificou outro erro C# independente. Isso é revisão de fonte e análise sintática, não compilação Unity. Os avisos de Collections, exemplos HDRP opcionais e suporte de build Windows não devem ser tratados como novas causas C# sem resolver o primeiro bloqueio.

## Terceiro log: importação incompleta em caminhos longos

Este log registra 30 pacotes, incluindo `com.unity.modules.physics2d@1.0.0`, e não contém `error CS...`. O bloqueio anterior de módulo foi superado. O erro atual é distinto: `DirectoryNotFoundException` em 27 arquivos de pacote.

A raiz do projeto tem 130 caracteres por conter duas pastas aninhadas com o nome longo do ZIP. Todos os caminhos ausentes têm 260–288caracteres. Exemplos, sem dados pessoais:

| Arquivo | Tamanho total do caminho |
| --- | --- |
| Unity.Collections.LowLevel.ILSupport.dll | 264 caracteres |
| InputAssetEditorDark.uss | 263 caracteres |
| ArnoldStandardSurfaceTransparent.shadergraph | 286 caracteres |
| PhysicalMaterial3DsMaxTransparent.shadergraph | 288 caracteres |

Não há `PathTooLongException` explícita nem reprodução Windows neste ambiente, mas esse padrão é forte evidência do limite tradicional de caminhos. O Burst falha ao resolver a mesma DLL ausente de Collections; o URP falha ao carregar os mesmos shaders já marcados como ausentes. Os stack traces de UIElements são parte dessas falhas, sem outra exceção independente identificada.

**Procedimento de recuperação:**

1. Feche o Unity Editor.
2. Coloque a pasta real do projeto em **C:\Dev\EcosDoVale** (ela deve conter Assets, Packages e ProjectSettings).
3. Somente depois de movê-la, renomeie `Library` para `Library_backup`. O cache incompleto fica preservado; os arquivos do projeto não são apagados.
4. No Hub, adicione esse caminho e abra com 6000.0.85f1. Aguarde a resolução/importação reconstruir o cache.
5. Verifique o Console e tente a cena novamente. Se houver nova falha, use o novo log dessa abertura.

A entrega agora usa um ZIP curto, com raiz `EcosDoVale`, e o atalho recusa caminhos de projeto maiores que 60 caracteres. Isso é uma prevenção conservadora para os pacotes atuais; não é alteração do limite global do Windows. A verificação recomendada é reimportar o projeto no caminho curto.

## Limite da verificação

Foram executados testes de política de versão, análise sintática dos scripts e testes do núcleo C#. Os testes EditMode, a importação de assets, shaders, Play Mode e o build continuam sem execução aqui. Consulte `VALIDATION.md`; não considere o projeto compilado somente porque os checks estáticos passaram.
