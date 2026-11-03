# Validação

O Editor Unity não está disponível neste ambiente. A limpeza do projeto deve ser validada em duas camadas: checks executáveis sem Unity e importação/execução com o Editor licenciado. O ZIP contém fontes; empacotar não gera nem valida um executável.

## Checks sem Unity

Nesta revisão passaram 24 testes Python, a análise de sintaxe de 11 arquivos C# e 99.935 asserções do núcleo C#, incluindo 1.624 andares gerados. A compilação Unity e os testes do Editor permanecem pendentes.

Execute na raiz do projeto:

```bash
python3 -m pip install -r Tools/requirements-validation.txt
python3 -m unittest discover -s Tools -p 'test_*.py'
python3 Tools/validate_project.py
dotnet run --project Tools/HauntChecks.csproj
bash -n Tools/setup-cloud.sh Tools/unity-check.sh
```

O núcleo C# verifica conectividade, determinismo e variação de mapas, posição do chefe, compras atômicas, limites de melhorias, morte, passagem de andar e a ordem das colisões. A suíte produz contagens no terminal; um resultado de outra versão não substitui uma execução atual.

As verificações Python cobrem a estrutura do projeto, metadados/GUIDs, versões, arquivos necessários ao lançamento e conteúdo seguro do ZIP. O parser C# verifica sintaxe, sem substituir a resolução de tipos e a compilação Unity. Os testes estáticos do atalho não executam Windows ou o Editor.

## Checks no Unity

Com `UNITY_EDITOR_PATH` apontando para o executável licenciado de Unity 6000.0.85f1:

```bash
bash Tools/unity-check.sh import
bash Tools/unity-check.sh smoke
bash Tools/unity-check.sh tests
bash Tools/unity-check.sh build
```

A importação prepara os recursos URP. O smoke verifica bootstrap, recursos e plano inicial. Os testes do Editor verificam configuração gráfica, cena/build, sprites, estado da incursão, combate/recompensa, revisita, esquiva, reinício e abertura segura. Eles não certificam a aparência renderizada.

Os testes de runtime isolam uma cena vazia e restauram a configuração anterior. Se houver edição de cena não salva, esse grupo pode ser pulado para preservar o trabalho; registre esse resultado como pulado, sem tratá-lo como aprovado. Confira os logs e o XML da execução atual em `/tmp`.

O build Windows exige o módulo Windows Build Support. A saída padrão é `Builds/Windows/EcosDaMeiaNoite.exe`.

## Verificação funcional

No Editor, abra **Ecos → Abrir roguelike**, pressione Play e confira:

1. Menu, início com semente aleatória e repetição de uma semente.
2. Movimento, mira, disparos e colisão com paredes e colunas.
3. Cada criatura, seus avisos de ataque e a esquiva atravessando projéteis.
4. Fechamento das portas durante o combate e abertura após a última ameaça.
5. Chefe, vitória, portal e chegada segura ao Mercado das Almas.
6. Compras, saldo insuficiente, limite por visita e preservação das melhorias no próximo andar.
7. Pausa, morte e reinício.
8. Legibilidade e mira em janelas de tamanhos diferentes.

A compilação completa, o Play Mode, o render e o executável desta limpeza permanecem pendentes até esses checks serem executados no Editor.
