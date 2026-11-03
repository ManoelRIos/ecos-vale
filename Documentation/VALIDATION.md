# Validação

O Editor Unity não está disponível neste ambiente. A limpeza do projeto deve ser validada em duas camadas: checks executáveis sem Unity e importação/execução com o Editor licenciado. O ZIP contém fontes; empacotar não gera nem valida um executável.

## Checks sem Unity

Nesta revisão passaram 24 testes Python, a análise de sintaxe de 12 arquivos C# e 100.030 asserções do núcleo C#, incluindo 1.624 andares gerados. A compilação Unity e os testes do Editor permanecem pendentes.

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

A importação prepara os recursos URP. O smoke verifica bootstrap, recursos e plano inicial. Os testes do Editor verificam configuração gráfica, cena/build, sprites, estado da incursão, combate/recompensa, revisita, esquiva, reinício e abertura segura. A expansão acrescenta cobertura para poderes, cinco encontros de chefe e os novos sprites/criaturas; esses testes Unity foram escritos, mas não executados aqui. Eles não certificam a aparência renderizada.

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
8. Legibilidade e mira em janelas de tamanhos diferentes, incluindo 860 × 500.
9. Sprite do jogador e de cada inimigo visíveis nas duas direções; cores preservadas após dano e esquiva.
10. Ciclos de quatro quadros de movimento, asas e velas; portas aberta/selada/guardião; rastro, recuo e partículas.
11. Selecionar as quatro almas, conferir sprite e usar Q: pulso remove balas próximas e bloqueia dano brevemente; lamento produz nove tiros. Repetir Q durante recarga não dispara novamente; pausa congela recarga.
12. Conferir Ceifador (12 tiros radiais/proteção) e Alquimista (cura 2; vida cheia não gasta recarga).
13. Comprar cada arma, conferir cobrança única, alternar com 1–4, atravessar andares e reiniciar. Conferir leque de cinco tiros, repetidora rápida e dano alto do canhão.
14. Chegar aos andares 4 e 5, conferir Matriarca/Sino, seus padrões e novas criaturas. Conferir minimapa e partículas em cada tema.
15. Ausência de tiros ao clicar no HUD ou no minimapa, pausa com cursor visível e restauração após reinício.

A compilação completa, o Play Mode, o render e o executável desta limpeza permanecem pendentes até esses checks serem executados no Editor.

## Roteiro adicional: arenas, chefes e loja

- Em cada um dos cinco temas, conferir piso, parede, decoração e miniatura própria do mapa; testar saídas nos quatro lados da arena ampliada e a chegada livre de obstáculos.
- Conferir poses de conjuração, aura e os dois padrões de cada chefe; sair de um aviso de chão antes de 1,1 s, ficar em outro para verificar dano, e purificar/sair com uma marca pendente para verificar cancelamento.
- Conferir rastros e impactos em inimigo, parede e cobertura com as quatro armas, sem encobrir a leitura das balas hostis.
- Na loja, trocar abas, comprar, recusar compra sem saldo, equipar arma adquirida, pausar/retomar e descer pelo botão. Teclas de movimento e E não devem comprar nem mover o jogador atrás do painel; M não deve abrir um mapa oculto.
- Conferir a câmera, HUD, atlas e cards em 1280×720 e 860×500. Não há coop nesta revisão.

Os testes Unity foram ampliados para geometria, portas/chegadas, identidades dos chefes, alternância de ataques, avisos de chão e poderes novos; permanecem não executados. A prévia PNG apenas mostra os pixels do desenho procedural, sem renderização Unity.
