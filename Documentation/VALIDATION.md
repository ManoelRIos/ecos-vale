# Validação

O Editor Unity não está disponível neste ambiente. A limpeza do projeto deve ser validada em duas camadas: checks executáveis sem Unity e importação/execução com o Editor licenciado. O ZIP contém fontes; empacotar não gera nem valida um executável.

## Checks sem Unity

Nesta revisão passaram 24 testes Python, a análise de sintaxe de 13 arquivos C# e 118.243 asserções do núcleo C#, incluindo 1.624 andares gerados. O validador estrutural conferiu 19 assets e 29 GUIDs. A compilação Unity e os testes do Editor permanecem pendentes.

Execute na raiz do projeto:

```bash
python3 -m pip install -r Tools/requirements-validation.txt
python3 -m unittest discover -s Tools -p 'test_*.py'
python3 Tools/validate_project.py
dotnet run --project Tools/HauntChecks.csproj
bash -n Tools/setup-cloud.sh Tools/unity-check.sh
```

O núcleo C# verifica conectividade, determinismo e variação de mapas, posição do chefe, três candidatos por tema, reprodução da escolha do guardião pela semente, cobertura dos pools sem alterar mapas, compras atômicas, limites de melhorias, morte, passagem de andar e a ordem das colisões. A suíte produz contagens no terminal; um resultado de outra versão não substitui uma execução atual.

As verificações Python cobrem a estrutura do projeto, metadados/GUIDs, versões, arquivos necessários ao lançamento e conteúdo seguro do ZIP. O parser C# verifica sintaxe, sem substituir a resolução de tipos e a compilação Unity. Os testes estáticos do atalho não executam Windows ou o Editor.

## Checks no Unity

Com `UNITY_EDITOR_PATH` apontando para o executável licenciado de Unity 6000.0.85f1:

```bash
bash Tools/unity-check.sh import
bash Tools/unity-check.sh smoke
bash Tools/unity-check.sh tests
bash Tools/unity-check.sh build
```

A importação prepara os recursos URP. O smoke verifica bootstrap, recursos e plano inicial. Os testes do Editor verificam configuração gráfica, cena/build, sprites, estado da incursão, combate/recompensa, revisita, esquiva, reinício e abertura segura. A expansão acrescenta cobertura para poderes, cinco arquétipos de chefe, seleção por tema/semente, câmera próxima, orientação da saída, reciclagem dos halos e os novos sprites/criaturas; esses testes Unity foram escritos, mas não executados aqui. Eles não certificam a aparência renderizada.

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
13. Comprar grampeador, banana e pião, conferir cobrança única, alternar com 1–4, atravessar andares e reiniciar. Conferir pares de grampos, tiro curvo da banana e pião que atravessa até três inimigos distintos; paredes e cobertura devem bloquear os três tipos.
14. Explorar os cinco temas com sementes diferentes e conferir os três guardiões possíveis de cada tema. Repetir semente e andar para conferir o mesmo guardião. Conferir Matriarca/Sino quando sorteados, seus padrões, as criaturas comuns do tema, o minimapa e as partículas.
15. Mirar e atirar através dos textos do HUD, mapa pequeno e atlas ampliado, sem zonas invisíveis de bloqueio. Conferir bloqueio de tiros no menu, catálogo do atendente, pausa e morte. A loja é segura: o jogador caminha com o catálogo fechado, sem disparar.

A compilação completa, o Play Mode, o render e o executável desta limpeza permanecem pendentes até esses checks serem executados no Editor.

## Roteiro adicional: arenas, chefes e loja

- Em cada um dos cinco temas, conferir as quatro variantes de piso, tom/rotação, paredes, relevos com sombra, candelabros de quatro quadros e miniatura própria do mapa. Repetir a semente para conferir os mesmos ladrilhos. Testar saídas nos quatro lados da arena ampliada e a chegada livre de obstáculos.
- Conferir poses de conjuração, aura e os dois padrões de cada chefe; sair de um aviso de chão antes de 1,1 s, ficar em outro para verificar dano, e purificar/sair com uma marca pendente para verificar cancelamento.
- Conferir rastros e impactos em inimigo, parede e cobertura com as quatro armas, sem encobrir a leitura das balas hostis.
- Entrar no mercado e caminhar entre balcão, vitrines e prateleiras, conferindo colisões e câmera. Clicar no atendente de longe não deve abrir o catálogo; aproximar-se a até 2,8 unidades e clicar ou usar E deve abri-lo. Movimento só fica bloqueado durante a conversa. Comprar, recusar compra sem saldo e equipar uma arma adquirida; Esc deve fechar o catálogo antes de pausar. Fechar a conversa, caminhar até o portal e usar E para descer. Nenhum botão do catálogo deve pular o portal.
- Conferir a câmera ortográfica de tamanho 6,3 em 1280×720 e 860×500: seguir o personagem suavemente, respeitar limites nas bordas e reposicionar imediatamente ao trocar de sala. Testar mira pelo mouse com a câmera deslocada. Conferir HUD, atlas e cartões nas duas resoluções. Não há coop nesta revisão.
- Durante o combate, conferir ausência da seta de saída. Purificar a sala e caminhar entre duas portas para conferir a indicação da mais próxima. Após vencer o chefe, conferir que a seta aponta ao portal mesmo perto de uma porta; na loja/pausa, a orientação deve ficar oculta.
- Para cada tema, testar sementes até observar os três candidatos documentados no README. Repetir uma semente e andar para conferir mapa e chefe iguais. Conferir que o piso, as paredes e os inimigos comuns permanecem associados ao tema, mesmo quando muda o guardião sorteado.
- Disparar repetidamente revólver, grampeador, banana e pião contra paredes, cobertura e inimigos; conferir halo seguindo cada tiro, transparência e mistura aditiva das chamas/clarões sem apagar balas hostis. Expirar ou colidir os tiros e verificar que seus halos desaparecem junto. Reiniciar e voltar ao menu várias vezes; confirmar que o pool reaproveita o par projétil/halo e que a contagem de objetos não cresce sem limite.
- Destruir o objeto `HauntGame` ou sair do Play Mode e conferir liberação do material aditivo `Haunt / additive light`, materiais e texturas da biblioteca de arte. Inspecionar o Console para referências destruídas ou halos órfãos; o pool e sua limpeza não são certificados pelo parser C#.

Os testes Unity foram ampliados para geometria, portas/chegadas, câmera, orientação da saída, seleção e identidades dos chefes, alternância de ataques, avisos de chão, poderes novos e reciclagem do halo com o projétil; permanecem não executados. A prévia PNG apenas mostra os pixels do desenho procedural, sem renderização Unity.

## Roteiro: HUD sem tarjas

- Em 1280×720 e 860×500, conferir corações no canto superior esquerdo, textos sem retângulos e legibilidade sobre pisos claros/escuros. Testar vida máxima acima de 10 para conferir a segunda linha de corações.
- Conferir ausência de fundos em notificações, rótulos das portas, portal e legenda da seta; dados da sala devem desaparecer após o breve aviso de entrada. A seta deve continuar visível ao ampliar o mapa depois de purificar a sala.
- Conferir mapa sem fundo em 45% e atlas em 80% de opacidade, incluindo corredores, miniaturas e retratos. Deve ser possível mirar e atirar em qualquer ponto das sobreposições.
- Conferir que catálogo do atendente, menu, pausa e morte continuam com botões funcionais e sem disparos por trás das telas. Com o catálogo fechado, deve ser possível caminhar pelo mercado.

Esta revisão de interface recebeu análise de sintaxe e checks estáticos. A aparência em jogo permanece pendente de execução no Editor Unity.

## Roteiro: barra de chefe e texto bitmap

- Enfrentar cada um dos cinco arquétipos e conferir barra de vida centralizada na parte inferior, preenchimento proporcional e moldura exclusiva em pixel art. Testar dano, morte, pausa e mudança de sala sem barra órfã ou informações duplicadas no topo.
- Conferir glifos bitmap em HUD, menus, botões, preços, rótulos e avisos, sem rótulos nativos sobrepostos. Digitar e apagar a semente, usar valores negativos e reiniciar para conferir entrada e sobreposição bitmap.
- No catálogo, conferir ícones das três armas, nome, preço e característica curta; as armas convencionais antigas não devem aparecer à venda. Conferir que compra fechada ou sem saldo não muda vida, arma, estoque ou moedas.
- Com o pião, alinhar quatro inimigos: ele deve atingir no máximo três distintos e não ferir repetidamente o mesmo alvo. Colocar cobertura antes de outro inimigo para conferir que não atravessa a parede. Conferir duração de 2,8 s e reaproveitamento de projétil/halo.
- Criar e destruir várias incursões e encerrar Play Mode para conferir que o cache de glifos, materiais e sprites é liberado. O parser não valida os recursos de IMGUI nem a compilação do Unity.

Os checks sem Unity desta revisão passaram; os testes visuais, a integração do mercado, os projéteis e a compilação completa permanecem pendentes de execução no Editor licenciado.
