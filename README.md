# Ecos do Vale

Protótipo de exploração em terceira pessoa para **Unity 6.0 LTS 6000.0.85f1**, C#, URP e Input System. Esta revisão dá vida ao vale de **1024 × 1024 m** com árvores de formas diferentes, seis espécies de criaturas, guia de campo e uma rota de descobertas com cenários próprios.

A versão anterior foi executada pelo usuário no Unity. **Esta revisão foi verificada com testes do núcleo C# e checks estáticos; animações, interface, JsonUtility, importação e desempenho ainda precisam de validação no Editor.** Não há build Windows validado desta revisão.

## Abrir

1. Instale Unity **6000.0.85f1** no Hub. Para build, inclua Windows Build Support.
2. Baixe o [ZIP de nome curto](https://github.com/ManoelRIos/ecos-vale/raw/refs/heads/codex/ecos-do-vale-vertical-slice/Distribution/EcosDoVale.zip) e extraia em **C:\Dev**, preservando a cópia anterior se houver alterações próprias.
3. Execute `C:\Dev\EcosDoVale\ABRIR_ECOS_DO_VALE.cmd`, ou adicione essa pasta pelo Hub. Ela deve conter Assets, Packages e ProjectSettings.
4. Aguarde a importação. Abra **Ecos → Abrir cena principal**, pressione Play e escolha **Nova jornada**.

Abra a cena com o Play parado. Os comandos de abrir cena, criar assets e reparar renderização ficam desabilitados durante Play, compilação ou importação. Uma chamada direta de abertura durante Play também retorna sem trocar a cena, evitando a exceção `This cannot be used during play mode`.

O atalho confere caminho curto, Editor fixado e Physics 2D; ele recebeu revisão estática, sem execução Windows aqui. Caso use GitHub Desktop, selecione a branch `codex/ecos-do-vale-vertical-slice`, faça Fetch/Pull e abra a mesma pasta do clone na Unity. Vincular GitHub não substitui esse passo.

Se reaparecer magenta, pare Play e use **Ecos → Reparar renderização URP**. O reparo restaura recursos gerados do renderer e pode remover personalizações dessas renderer features. **Ecos → Diagnosticar renderização** mostra pipelines e shaders atuais. O projeto usa materiais procedurais, sem depender de texturas externas.

## A aventura

O acampamento fica a poucos passos do início. Os recursos aparecem em pequenos grupos próximos; cada altar tem sua própria necessidade.

| Etapa | Ação / custo |
| --- | --- |
| Refúgio | Acender a fogueira com 4 madeiras e 3 pedras |
| Ferramenta | Na bancada, criar a lente com 2 cristais, 3 ervas e 2 pedras |
| Exploração | Usar Q para revelar até 18 ecos num raio de 42 m por 5 s; recarga de 14 s |
| Torre da Vigília | Restaurar com 1 cristal e 2 pedras |
| Cemitério das Lanternas | Restaurar com 1 cristal e 2 ervas |
| Cabana do Último Eco | Restaurar com 1 cristal e 2 madeiras |
| Conclusão | Receber o Artefato do Vale e experiência, uma única vez |

Além da missão, há **seis baús selados**, **seis fragmentos de história**, dezesseis localidades e seis biomas. Marcos de pedra e setas ligam uma carroça abandonada, um arco de raízes, um jardim lunar, um mirante, um observatório partido e o refúgio da ponte velha. Cada lugar guarda uma pista ou tesouro e cinco recursos próximos; há 124 coletas fixas no total. A lente abre os baús. Descobrir, ler e restaurar concede XP; a cada 40 XP o personagem sobe um nível e amplia a mochila, até 20 espaços. Receitas e recompensas são atômicas: falhar por falta de recursos/espaço não entrega nem consome parcialmente.

O mapa marca os locais descobertos. Os três santuários preservam as coordenadas anteriores: `(28,20)`, `(-65,80)` e `(105,-60)`. A fogueira está em `(-12,-9)` e a bancada em `(-6,-14)`.

## Criaturas e guia de campo

Coelhos saltam e fogem, cervos pastam, raposas se aproximam por curiosidade e javalis farejam e ficam alertas. À noite, morcegos voam baixo e fagulhas mágicas surgem entre as árvores; a lente atrai sua curiosidade. Os modelos e animações são procedurais, sem assets pagos.

Aproxime-se com **Ctrl** e use **E** para observar. Cada espécie concede **12 XP uma vez**, mesmo que outro animal da mesma espécie seja encontrado depois. Em **I → Guia de campo**, os registros revelam hábitos e descrições. Registrar as seis espécies concede um artefato e 25 XP, uma única vez; se a mochila estiver cheia, a recompensa aguarda espaço.

Os habitats são definidos pela seed, com moradores preferenciais por bioma. Até 18 criaturas ficam ativas por padrão, próximas ao jogador, com criação/retirada gradual; o limite configurável é 24 em `Resources/WildlifeSettings.asset`. Movimento, decisões e encontros são locais/offline. O guia é persistente; posições e estados transitórios dos animais não são salvos.

## Controles

| Entrada | Ação |
| --- | --- |
| WASD / mouse | Mover / olhar |
| Shift / Espaço / Ctrl | Correr / pular / agachar |
| Roda do mouse | Zoom da câmera |
| E / F | Interagir ou observar criatura / alternar lanterna |
| Q | Pulso da lente, após criá-la |
| I / M / Esc | Mochila e guia de campo / mapa / pausa |
| F3, apenas Editor/Development | Contadores de desenvolvimento |

A animação procedural tem respiração, passos baseados no deslocamento real, joelhos/cotovelos, corrida, curvas, salto e aterrissagem. Agachar altera a cápsula sem escalar a raiz física; o personagem verifica espaço para levantar. Ainda exige teste de colisão e apresentação no Editor.

## Mundo e apresentação

- Mundo lógico de 1 km por lado, 64 chunks de 128 m; seed configurável e seis biomas.
- Até 280 candidatos de decoração por chunk, filtrados por relevo, trilhas e clareiras. Cada bioma tem quatro silhuetas de árvore (24 no total). Pequenos bosques misturam famílias locais, com alturas e ramificações diferentes; detalhes formam manchas de flores, samambaias, caniços, arbustos, tocos, troncos e cogumelos.
- Na seed padrão, os nove chunks iniciais planejam 1150 árvores e 1118 detalhes, antes das clareiras dos POIs e corredores da rota. As quatro famílias de árvore aparecem em 262/312/250/326 candidatos, com distância média da árvore mais próxima de 9,35 m. Isso é medição de dados, não benchmark de renderização.
- Dois workers de dados, cache limitado, cancelamento e criação na thread principal. Decoração processada em até 8 candidatos/frame, com orçamento flexível de 2 ms; uma criação individual pode ultrapassá-lo.
- Padrão: 25 meshes de terreno e nove chunks com decoração; um anel extra de terreno protege o horizonte sem povoar tudo. Raio de decoração ajustável de 1 a 2. Pool limitado a 256 objetos e 48 por tipo.
- Cores mais saturadas, cogumelos sem emissão no chapéu, brilho discreto na parte inferior e iluminação menos propensa a estourar cores.
- Menu com cenário 3D próprio, viajante, cabana, lanternas, câmera suave, cards e configurações. HUD apresenta etapas, recursos, nível/XP e lente; inventário/mapa usam a mesma identidade visual.
- Dia/noite, céu gradual, partículas climáticas e névoa; áudio ainda é infraestrutura sem trilha/efeitos finais.

## Saves

O arquivo continua em `Application.persistentDataPath/ecos-slot1.json`, com backup `.bak`. O formato passou para **v3**, acrescentando espécies observadas e recompensa do guia à fogueira, lente, XP, baús, fragmentos e progresso anterior.

Saves v1 e v2 são migrados sem descartar inventário, santuários ou descobertas. Quem já restaurou um santuário em v1 mantém acesso ao acampamento/lente. Posições fora dos limites retornam à área inicial. **Continuar também mostra a nova vegetação, os animais e os novos lugares**; não é necessário apagar o progresso. Nova jornada reinicia a sequência com confirmação antes de substituir a viagem no próximo save.

## Testes e ferramentas

```bash
python3 -m pip install -r Tools/requirements-validation.txt
python3 Tools/validate_project.py
python3 -m unittest discover -s Tools -p 'test_*.py'
dotnet run --project Tools/CoreChecks.csproj
```

O teste .NET compila os arquivos reais de ruído, inventário, progressão, catálogo de criaturas e distribuição de props. Ele não usa stubs Unity nem valida renderização. Os testes em `Assets/_Game/Tests/Editor` precisam do Editor.

Com `UNITY_EDITOR_PATH` apontando para o executável licenciado:

```bash
bash Tools/unity-check.sh import
bash Tools/unity-check.sh tests
bash Tools/unity-check.sh build
```

Confira logs e XML atuais. O build Windows depende do módulo apropriado; saída padrão `Builds/Windows/EcosDoVale.exe`. O UPM gera o lockfile transitivo real. Não foi fabricado um packages-lock.json sem resolução do Editor.

Para recriar o ZIP num clone Git, adicione arquivos novos ao índice e execute `python3 Tools/package_project.py`. A distribuição usa raiz curta e exclui caches/backups e o próprio ZIP.

## Limites atuais

Não há conexão host/client jogável, lobby, Relay ou matchmaking. `IWorldAuthority` e o estado NGO são preparação; o jogo atual é offline. Também não há combate, inimigos, água/lava navegáveis, vilarejos completos, música final ou animações humanoides importadas.

A UI usa IMGUI; a vegetação decorativa não possui colisores. O terreno usa resolução uniforme e a decoração possui LOD. Densidade é configuração de nova sessão; o raio de visão pode mudar ao vivo. A meta de 1080p/60 FPS ainda não foi medida. Os locais novos ampliam a exploração até cerca de 280 m do acampamento; o restante é floresta procedural. As criaturas usam direção local e verificações simples de terreno/obstáculos, sem NavMesh, combate ou ecossistema persistente.

Veja [LIVING_VALLEY.md](Documentation/LIVING_VALLEY.md), [VALIDATION.md](Documentation/VALIDATION.md) e [ARCHITECTURE.md](Documentation/ARCHITECTURE.md). [COMPACT_ADVENTURE.md](Documentation/COMPACT_ADVENTURE.md) registra a etapa anterior.
