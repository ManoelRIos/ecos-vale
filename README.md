# Ecos do Vale

Vertical slice em C# para **Unity 6 LTS 6000.0.85f1**, Windows PC, URP e Input System. Todo o conteúdo visual é feito de primitivas e meshes próprias; nenhum asset pago é necessário.

**Estado real:** projeto e assets criados; testes do núcleo C# e verificações estáticas executados. Este ambiente não possui Unity Editor. A importação dos assets, compilação dos componentes Unity, experiência em Play Mode, shaders e build Windows **ainda não foram executados**. Não há executável Windows validado nesta entrega.

## Abrir e jogar

1. Instale Unity **6000.0.85f1** pelo Unity Hub, com Windows Build Support, e ative uma licença válida. Unity Personal é uma opção quando aplicável.
2. Adicione esta pasta como projeto existente. Aguarde a resolução dos pacotes fixados em `Packages/manifest.json`.
3. Na primeira importação, `ProjectSetup` cria o pipeline URP e o renderer usando as APIs do Editor, e registra a cena no Build Settings. Os dados dos seis biomas, itens, materiais, prefabs visuais e a cena inicial já estão no projeto.
4. Use **Ecos → Abrir cena principal**, ou abra `Assets/_Game/Scenes/Valley.unity`. Pressione Play e **Novo jogo**.
5. Caso a configuração automática não ocorra, execute **Ecos → Criar assets e atualizar cena**. A operação preserva os assets existentes. Confira o Console antes de jogar.

A cena contém um ponto de entrada; o terreno, jogador e ambiente são construídos ao iniciar a sessão. Os prefabs em `Prefabs/` são referências visuais editáveis; a fábrica procedural é usada pelo runtime. Não é necessário montar objetos no Inspector.

## Atualização de segurança e abertura

A versão anterior 6000.0.40f1 é afetada pela CVE-2025-59489. Use o Editor 6000.0.85f1 e uma extração nova do ZIP atualizado. Confira o [guia de correção e diagnóstico](Documentation/UNITY_TROUBLESHOOTING.md) se o Hub mostrar Security Alert ou o projeto abrir com erros. Alterar o pin não instala o Editor automaticamente.

O log de abertura também identificou um módulo nativo ausente: **Physics 2D**, exigido pelos componentes do Netcode 2.0.0 mesmo em um jogo 3D. O manifest atual já inclui essa dependência. Se estiver usando a cópia anterior, habilite Physics 2D nos pacotes Built-in do Package Manager e aguarde recompilar.

## Abrir no Windows com caminho curto

Baixe o [ZIP de nome curto](https://github.com/ManoelRIos/ecos-vale/raw/refs/heads/codex/ecos-do-vale-vertical-slice/Distribution/EcosDoVale.zip).

Use o ZIP de distribuição **EcosDoVale.zip**, que contém apenas a pasta raiz curta `EcosDoVale`. Extraia em **C:\Dev**, para que o projeto fique em **C:\Dev\EcosDoVale**. Evite os dois níveis de pastas longas com hash gerados ao extrair o ZIP automático do GitHub.

Feche o Editor e execute **ABRIR_ECOS_DO_VALE.cmd** dentro dessa pasta. Ele recusa caminhos acima de 60 caracteres (uma margem preventiva para os caminhos internos dos pacotes), verifica Physics 2D e o pin 6000.0.85f1, e inicia exatamente essa pasta. Não baixa ferramentas, não altera o Registro e não apaga caches. Se o Editor estiver fora do caminho padrão do Hub, use **Add project from disk** e escolha `C:\Dev\EcosDoVale`. O log dessa abertura fica em `Logs/Ecos-Editor.log`.

Para aproveitar sua cópia existente: feche o Unity, mova a pasta real do projeto para esse caminho curto e **depois renomeie `Library` para `Library_backup`**. Ao reabrir, o Unity recria o cache. Preserve arquivos e alterações próprios. O atalho não faz essa operação automaticamente.

O log mais recente já registra Physics 2D e não contém erros `CS...`, mas mostra 27 arquivos de pacotes ausentes, todos com caminhos de 260 a 288 caracteres. Isso é forte evidência do limite tradicional de caminhos do Windows; as falhas Burst/URP subsequentes apontam para esses mesmos arquivos ausentes. Ainda não foi confirmada uma abertura sem erros após encurtar o caminho.

O atalho recebeu revisão estática; **não foi executado no Windows** neste ambiente. Ele não certifica compilação ou Play Mode. A opção `/check` verifica os arquivos e o tamanho do caminho sem iniciar o Editor.

## Pequena aventura

Você encontra os **três Ecos do Vale**, recolhe cristais e restaura um altar em cada local. Cada restauração consome um cristal. A terceira concede um **Artefato do Vale**, uma única vez. Há dezoito cristais na área inicial, além de madeira e pedra.

Os três locais estão próximos ao início: Torre da Vigília `(28, 20)`, Cemitério das Lanternas `(-65, 80)` e Cabana do Último Eco `(105, -60)`. A aproximação registra a descoberta no mapa. Os cristais aparecem como objetos azuis luminosos. A missão está ativa desde o início; o HUD informa o progresso.

| Entrada | Ação |
| --- | --- |
| WASD / mouse | Mover / olhar |
| Shift / Espaço / Ctrl | Correr / pular / agachar |
| Roda do mouse | Zoom da câmera |
| E / F | Interagir / alternar lanterna |
| I / M / Esc | Inventário / mapa / menu |
| F3, apenas Editor/Development | Contadores de chunks, props e tempo de upload |

## Implementado

- Personagem com corpo, cabeça, chapéu, mochila e membros animados proceduralmente; câmera com suavização e teste de colisão.
- Mundo determinístico, seed configurável, FBM de value noise, domain warping, montanhas, vales, canal de rio e trilhas coloridas.
- Grade lógica de **8192 × 8192 m** (aproximadamente 8 km; escolhida para alinhar 64 chunks de 128 m por eixo). Extensão, resolução, densidade e raio configuráveis.
- Streaming: até dois workers de dados, cancelamento, cache limitado a 96 chunks, criação/descarte na thread principal e normais contínuas nas bordas. API de união de interesses de múltiplos observadores, utilizada inicialmente com um jogador.
- Seis biomas com ScriptableObjects, cores, folhagem, densidade, névoa e clima: assombrada, planícies, montanhas, pântano, encantada e vulcânica.
- Árvores, rochas, abóboras e cogumelos feitos de primitivas/mesh facetada; materiais compartilhados, decoração com pool, LOD de props e frustum culling da Unity.
- Dia/noite, sol, lua, estrelas estilizadas, lanterna e névoa exponencial. Chuva/neve/cinzas com partículas e transição de intensidade por bioma.
- Três pontos de interesse distintos, descoberta, coleta validada por distância/ID, inventário com pilhas, descrição e descarte permanente.
- Missão de três restaurações, recompensa única, menu, HUD, bússola e mapa de biomas/altitude com zoom.
- Save JSON versionado: seed, posição, inventário, coletas, descobertas, restaurações e hora. Autosave a cada 30 segundos e ao sair; escrita temporária, backup `.bak` e substituição do arquivo existente.
- Fronteira de comandos `IWorldAuthority`, implementação offline e componente NGO `SessionState` com seed/hora de escrita exclusiva do servidor.
- Infraestrutura de áudio categorizada, clips e AudioMixer atribuíveis. **Nenhum som é reproduzido sem clips; não há trilha, efeitos ou mixer roteado entregue.**

## Escopo posterior e limitações

Esta é a primeira implementação, não o jogo completo descrito na visão. A validação Unity é o próximo passo obrigatório.

- Multiplayer de fato: não há host/client jogável, replicação de personagens, RPCs de interação, lobbies, códigos de convite ou Relay. Os pacotes e a fronteira de autoridade estão preparados; não há serviço pago.
- Cinemachine está fixado como dependência disponível, mas a câmera funcional é própria. A UI inicial usa IMGUI; migração para UI Toolkit/TMP, navegação por controle e remapeamento de ações ficam para a próxima etapa.
- O layout climático inicial usa regiões angulares deformadas por ruído para expor os seis biomas perto do início; temperatura/umidade completas, relevo por asset e blending de vegetação ficam pendentes.
- Água, lagos navegáveis, lava, cavernas exploráveis, pontes e vilarejos completos ainda não existem. O canal de rio é geometria de terreno, sem superfície de água.
- Há três locais autorais na região inicial. O restante do mundo tem relevo/decoração procedural, mas precisa de distribuição adicional de locais, segredos e recompensas.
- Névoa não é volumétrica. Clima possui enum para oito tipos; tempestades, relâmpagos e nevascas avançadas não têm efeitos próprios completos.
- LOD atual é de decoração; não há LOD variável do mesh de terreno nem occlusion culling pré-calculado. Decoração não tem colisores; terreno e estruturas principais têm.
- Inventário permite descartar destruindo permanentemente a unidade; não gera um saco no chão. Ervas/chaves/itens de missão possuem definições, mas não ciclos de uso/coleta próprios.
- O mapa é centrado no mundo, com três níveis de zoom; não há pan. O agachamento é simples e requer testes de teto/colisão no Editor.
- Não há combate, inimigos, sobrevivência ou áudio final. A meta de 1080p/60 FPS **não foi medida**.

## Validação e builds

Consulte [VALIDATION.md](Documentation/VALIDATION.md) para os resultados realmente obtidos e [ARCHITECTURE.md](Documentation/ARCHITECTURE.md) para os módulos e limites de rede.

```bash
python3 -m pip install -r Tools/requirements-validation.txt
python3 Tools/validate_project.py
dotnet run --project Tools/CoreChecks.csproj
```

Os testes .NET compilam os próprios arquivos de produção `WorldMath.cs` e `InventoryState.cs`. Eles não simulam a Unity nem validam renderização. SDK usado: .NET 8.0.427.

Com um Editor licenciado, execute a importação, os testes EditMode e o build separadamente:

```bash
"$UNITY_EDITOR_PATH" -batchmode -nographics -projectPath "$PWD" \
  -executeMethod Ecos.Editor.ProjectSetup.CreateDefaults -quit -logFile /tmp/ecos-import.log
"$UNITY_EDITOR_PATH" -batchmode -nographics -projectPath "$PWD" \
  -runTests -testPlatform EditMode -testResults /tmp/ecos-tests.xml -logFile /tmp/ecos-tests.log
"$UNITY_EDITOR_PATH" -batchmode -nographics -projectPath "$PWD" \
  -executeMethod Ecos.Editor.ProjectSetup.BuildWindows -quit -logFile /tmp/ecos-build.log
```

Não use `-quit` no comando do Test Runner. Confira os resultados XML e os logs, além do código de saída. O build depende do módulo Windows; saída padrão `Builds/Windows/EcosDoVale.exe`, substituível por `ECOS_BUILD_OUTPUT`.

O gerenciador de pacotes criará `packages-lock.json` na primeira resolução real. Não foi fabricado um lockfile transitivo sem UPM. O acesso a `packages.unity.com` e `download.packages.unity.com` foi salvo no rascunho de rede do ambiente, mas precisa ser aplicado nas configurações/publicação.

Save: `Application.persistentDataPath/ecos-slot1.json`. **Novo jogo** substitui o slot no próximo salvamento. Preserve o arquivo e seu `.bak` antes de testar com progresso importante.

Para reproduzir o ZIP em um clone Git, use `python3 Tools/package_project.py`. O comando usa arquivos presentes no índice Git; adicione arquivos novos antes de empacotar. A pasta Distribution é excluída do conteúdo para evitar ZIPs recursivos.
