# Validação de Ecos da Meia-Noite

## Nova base low poly — estado da validação

- **34 testes Python passaram**, incluindo exigência da cena low poly como primeira entrada, preservação das duas cenas anteriores e recusa de pacotes sem fontes, shader, material ou metadados da nova base, ou com o perfil low poly desativado na entrada. Configurações inválidas conservam o ZIP anterior.
- O helper de geometria passou **1.177 asserções** sobre a fábrica real com armazenamento de meshes simulado: limites, faces, normais, orientação, fechamento das malhas, cache, colisores e descarte. Isso não executa renderização ou física do Unity.
- `lowpoly-smoke` confere a importação da cena, seu perfil low poly, o material, o shader e o renderer 3D. `smoke` inclui também as duas cenas preservadas. Esses comandos e os novos testes nativos precisam de execução em um Editor licenciado.
- Na rodada final, o helper integrado passou **851 verificações** compilando os módulos de produção FPS, LowPoly, Core e Art: 435 checks do perfil híbrido preservado e 416 do novo perfil. Cobriu câmera FPS/TPS, conservação do estado, consultas de cobertura, rotas pelos cinco andares, bloqueio e liberação das salas, chefes, compras atômicas, munição, 17 modelos e padrões de arma, colisão dos mobs baixos, orientação nas portas, reinício da descoberta do mapa, foco da loja, coleta de munição sem duplicação, barris nas revisitas e descarte. As consultas de física e as APIs Unity/URP/Input System são simuladas; isso não é compilação contra as DLLs do Unity nem execução em Play Mode.
- Helpers isolados registraram **21.829 verificações de salas e portas low poly** sobre 550 salas geradas, **259 de personagens/arsenal** e **58 de câmera/jogador**. São checks de lógica e geometria com armazenamento e APIs simulados; não certificam resultado visual, colisão real, áudio reproduzido ou controle físico.
- A revisão estática também corrigiu o mapa que mantinha descobertas do andar anterior, a chegada voltada à parede, a loja sobreposta à cobertura e a altura de colisão/mira dos mobs baixos. Os testes nativos de Editor incluem bootstrap, recursos e continuidade de câmera/salas; sua execução no motor está pendente.

## Histórico da expansão híbrida

- **32 testes Python passaram**, cobrindo versões, atalho, importação das folhas e entrega. O novo teste recusa um pacote sem componentes híbridos ou seus metadados e conserva o ZIP anterior. Naquela revisão, o build mantinha a campanha híbrida como primeira cena e o roguelike anterior como segunda, ambas habilitadas.
- O validador estático verifica sintaxe C#, referências, metadados, dependências e presença dos módulos de perspectiva, arte híbrida, campanha, catálogo, modelos e chefes. Ele não compila contra as DLLs do Unity.
- Um helper externo compilou todos os módulos FPS, Core e Art reais e passou **435 verificações integradas**: troca e continuidade de estado/projéteis, mira, cobertura, quatro corpos e equipamentos, armas e poderes, menus pelo controle, sprites dos 17 mobs e sete chefes, seus modelos 3D, cinco fases, portal, pausa, morte, reinício e descarte. Usa tipos Unity/URP/Input System e consultas de física simulados, com arquivos reais de atlas e catálogo; não é Play Mode.
- Helpers isolados passaram **4.007 verificações** de temas/catálogo/cenário, **319** de mobs/chefes e **29** de apresentação/descarte. Esses resultados verificam lógica com APIs simuladas, sem validar pixels renderizados, colisões reais ou áudio reproduzido.
- Testes nativos de Editor foram ampliados para a continuidade da troca, pausa durante a transição, preservação das colisões no corte de paredes e progressão por cinco temas com chefe obrigatório. Sua execução no motor está pendente.
- A validação no motor da troca de câmera, dos quatro personagens, cinco arenas, 17 mobs e sete chefes está pendente. Os roteiros abaixo especificam os cenários necessários; os resultados históricos da base FPS não certificam a expansão atual.

## Histórico da base FPS e da correção URP

Antes desta expansão, a correção do log do usuário removeu a atribuição à propriedade de escrita interna `supportsMainLightShadows` do URP 17. A preparação usa `SerializedObject`. Um helper externo reproduziu o `CS0200` original e a fonte corrigida passou 19 verificações com contratos simulados. O teste Editor nativo de reparo e preservação dos assets permanece pendente no motor.

A base FPS anterior registrou:

- 2.507.249 asserções compilando o núcleo C# real, incluindo regras FPS de três ondas, esquiva e dano radial e os checks do roguelike 2D.
- 52 verificações integradas de bootstrap, pipeline, tiro/cover, recarga, pausa, esquiva, três ondas, portal, explosão, limites, morte, áudio de conclusão, reinício e descarte, com Unity/URP/Input System e consultas de física simulados.
- Helpers isolados com 114 verificações de jogador/arma, 28 de cenário e 27 de inimigos/barris. O áudio passou 847.897 verificações de PCM finito, determinismo, limites e ciclo de vida.

Esses helpers temporários não executam importação Unity, HLSL, Play Mode, audição ou controle físico. Os testes nativos adicionados à base também precisam ser executados no Editor.

## Verificações registradas do roguelike 2D

Os resultados de helpers e folhas abaixo pertencem à implementação 2D preservada. Eles não executam os novos componentes FPS no Unity.

- Passaram 23.098 verificações de integração em um helper temporário que compilou todos os módulos reais com tipos Unity/Input System simulados: avisos, ondas, sete chefes, i-frames/recuperação, divisão, cancelamento, colisões, feedback, controle, objetos e descarte. A expansão cobre carregadores dos quatro heróis e das doze armas compráveis, custo por salva/pulso, último disparo do feixe, limite de projéteis sem perda de munição, R/Triângulo, pausa, troca, poder sem custo, revólver de apoio, drop por morte e revisita/coleta sem duplicação.
- Áudio em helper externo: 19 efeitos, 12 loops musicais e 12 vozes curtas com PCM finito, determinístico e ganho limitado. Isso não substitui audição.
- Folhas: 24 PNGs transparentes e 1.700 células em 48/96/32 px, com PPU 32 e metadados estáveis. `python3 Tools/export_sprite_sheets.py --check` passou sem diferenças. O helper integrado carregou os PNGs reais, conferiu retângulos/pivôs/cache e comparou os pixels de cada célula ao gerador C#, além de validar recorte dos ícones/retratos. A prévia atual `REFERENCE_SPRITESHEETS.png` usa esses pixels, sem renderização do motor.
- A integração inclui os estados reais do herói, seis mobs, mortes e barris; confirma tamanhos no mundo, PPU 32, detalhes nativos além de blocos 2×2 e quatro poses distintas por estado/direção. O teste compara 31 itens ao tamanho anterior. Os testes Editor de folhas, escala, detalhe e descarte aguardam execução no Unity.

- Modelo nativo: 696 recortes em 135 clips, compartilhados por quatro personagens, com posturas, direções e traje alternativo. Os testes Python comparam cada pixel visível do Pilot com a fonte e confirmam que nenhum pixel fica fora do retângulo renderizado; também verificam a sombra e os fundos por clip, variantes, hash e reprodução dos arquivos. O PNG original preserva SHA256 `cd640095da82003f8f44902950382692518393bfa57e61bc9bba1f3abec8f469`.
- O helper integrado carregou os quatro atlas reais e comparou todos os recortes, pivôs, contagens, FPS, loops, clamps e caches ao catálogo; conferiu renderização lógica sem squash e morte de 19 quadros. Sete testes Editor nativos foram adicionados, sem execução no motor.

**O Editor Unity não está instalado neste ambiente.** Esses resultados não certificam importação, compilação no Unity, shaders, Play Mode, áudio reproduzido, hardware PS4 ou executável Windows. Não há build Windows validado desta revisão. Os testes Editor foram ampliados e precisam ser executados no motor.

## Reproduzir checks sem Editor

```bash
python3 -m pip install -r Tools/requirements-validation.txt
python3 Tools/validate_project.py
python3 -m unittest discover -s Tools -p 'test_*.py'
dotnet run --project Tools/HauntChecks.csproj
python3 Tools/export_sprite_sheets.py --check
python3 Tools/import_pilot_sheet.py --check
```

O ambiente configurado por `bash Tools/setup-cloud.sh` executa a instalação e esses checks. Para gerar o ZIP, rastreie arquivos novos e use `python3 Tools/package_project.py`. O pacote exige as três cenas na ordem LowPoly/Fps/Haunt, os módulos de `Haunt/Fps` e `Haunt/LowPoly`, a configuração Editor, os dois shaders e materiais 3D, com seus metadados, além dos arquivos obrigatórios dos perfis anteriores, recursos e versões. Os pipelines são gerados pelo Editor na preparação.

## Verificação necessária no Unity

Abra uma extração nova pelo Hub com Unity 6000.0.85f1. Aguarde os pacotes URP 17.0.4, Input System 1.11.2, Test Framework 1.4.5 e o módulo Physics 1.0.0. Com Play parado, use **Ecos → Abrir jogo low poly**. Execute os testes de Editor pelo Test Runner e procure erros de compilação, shader ou referências no Console. Abra também a cena 2D por **Ecos → Abrir roguelike**.

Com Editor licenciado e `UNITY_EDITOR_PATH` definido, também é possível usar:

```bash
bash Tools/unity-check.sh import
bash Tools/unity-check.sh lowpoly-smoke
bash Tools/unity-check.sh fps-smoke
bash Tools/unity-check.sh smoke
bash Tools/unity-check.sh tests
bash Tools/unity-check.sh build
```

`lowpoly-smoke` confere a nova cena e seu perfil/material/renderer. `fps-smoke` confere o híbrido preservado; `smoke` reúne as três cenas. Eles verificam recursos sem entrar em Play Mode. O build solicitado fica em `Builds/Windows/EcosDaMeiaNoite.exe`, iniciando em `HauntLowPoly` e incluindo `HauntFps` e `Haunt` em seguida. Teste a execução em Windows; o atalho opcional também precisa de validação nesse sistema.

## Roteiro pendente da campanha híbrida preservada

1. Inicie com cada um dos quatro personagens e confira arma, mãos/modelo, sprite e poder. Esvazie os carregadores de 12/6/6/30 tiros e confirme R/Triângulo manual com reserva infinita. Pause durante a recarga; alternar perspectiva não pode completá-la, cancelá-la ou cobrar outra bala.
2. Alterne Tab/R1 durante movimento, tiro, esquiva, dano, recarga e projéteis próximos. Vida, munição, posições, inimigos, chefe, tiros e cooldowns devem continuar. A transição de 0,4 s bloqueia o disparo e não acrescenta invencibilidade. Pressões repetidas respeitam o intervalo; pausa e perda de foco congelam a transição.
3. Na visão 2D, confira movimento nos eixos da sala, mira pelo mouse/analógico, retículo, sprites e corte visual das paredes. Os colisores de chão, parede, pilar e barril permanecem iguais. No FPS, modelos e paredes devem voltar; olhar vertical e orientação precisam continuar estáveis após várias trocas.
4. Teste a proteção dos primeiros 60% da esquiva e a recuperação vulnerável dos 40% finais nas duas visões. Balas devem atravessar o jogador durante a proteção, sem ser consumidas, e acertar na recuperação. Trocar perspectiva não deve reiniciar o relógio da esquiva.
5. Confira as cinco arenas: Jardins da Colheita, Galeria dos Véus, Catedral das Cinzas, Ninho da Peste e Abismo dos Sinos. Compare materiais, cobertura, decoração, luzes e neblina. Todas as combinações de chegada e luta precisam manter caminho ao portal, spawn seguro e leitura dos projéteis.
6. Observe os 17 tipos de criatura nos rosters e confira modelos, sprites, preparação, ataques, feedback e morte. Acerte barris em espaço aberto e atrás de cobertura; confira pavio, explosão, dano, empurrão, trauma e reação em cadeia. Objetos em corte não podem perder colisão.
7. No treino, teste os sete chefes em todos os temas. Confira introdução de três segundos invulnerável, título/barra de vida, preparação, padrões, fúria abaixo de 50% e morte. Alterne visão durante cada estado. Bombas em cruz e orbes que dividem devem respeitar primeiro contato, pausa, limite e cancelamento ao encerrar a luta.
8. Complete três ondas e o chefe de cada fase. O portal permanece fechado até a vitória; E/Quadrado avança uma única vez. Vida, arma, carregador, moedas e poder persistem para a próxima fase. A quinta termina a campanha; o treino termina seu encontro. Confira morte e reinício restaurando a incursão inicial.
9. Ouça música e efeitos, alterne M/som, pause e retire/devolva foco. Use o DualShock 4 físico por USB e Bluetooth, verificando os dois analógicos, R1, R2, L1, Triângulo, Círculo, X, Quadrado e Options. Os testes sintéticos não garantem compatibilidade do driver.
10. Reinicie várias vezes e encerre Play. Confira descarte de modelos, sprites, texturas, materiais, clipes, vozes, cápsulas, efeitos e câmera de arma; não devem restar atores ou colisores antigos. Ambiente, pipeline e cursor precisam ser restaurados. Abra também `Haunt.unity` e confira a versão 2D anterior.

Essas etapas, incluindo importação, compilação Unity, renderização, física, audição e hardware, **ainda não foram executadas neste ambiente**. A campanha usa cinco arenas temáticas; a rede completa de salas, a loja e o arsenal anterior continuam na cena 2D. Detalhes estão em [PROTOTIPO_FPS.md](PROTOTIPO_FPS.md).

Os roteiros seguintes referem-se ao roguelike **2D** preservado.

## Combate, arsenal e cenário

1. Compare salas de enxame, emboscada, fogo cruzado e elite na mesma incursão. Confira combinações, formações e mobilidade diferentes; todas precisam de três ondas, uma recompensa e portas fechadas até a última. Emergência de 0,9 s não deve atacar nem receber dano. Miniboss não deve abrir portal de chefe.
2. Confira os dezessete mobs comuns e seus sinais de ataque, incluindo lanterna orbital, cogumelo bruto e diabrete de corda. Verifique que cobertura impede tiros, criaturas não ficam presas nos objetos novos e grupos mantêm rotas de aproximação. Limites de tiros devem preservar o disparo do jogador em salas densas.
3. Teste as quatro armas iniciais, nove armas curiosas e três antigas. Confira corpo arredondado, núcleo claro e halo curto co-localizado em pisos claros/escuros, com balas hostis distintas. Rastros e clarões devem reforçar o movimento sem esconder ataques. Troque armas após colisões e confira a restauração de escala, cor e orientação do pool.
4. Guarda-Chuva: cinco pétalas, dois ricochetes e término no contato sólido seguinte. Colmeia: três ferrões com perseguição suave, sem atravessar cobertura ou procurar inimigos emergindo. Luva: dano pesado e empurrão sem lançar o alvo para paredes/abismos; o chefe recebe apenas um pequeno recuo por impacto. Pião: até três inimigos distintos, sem dano repetido no mesmo alvo.
5. Raios acompanham a mira, respeitam a cadência e param no primeiro inimigo, parede ou objeto. Soltar, trocar arma, esquivar, pausar, morrer ou abrir catálogo oculta corpo, brilho e loop sonoro.
6. Destrua um barril e saia do marcador de 0,55 s; teste dano no jogador e em inimigos, cobertura, pares próximos e reação em cadeia. A explosão do último alvo deve avançar só uma onda, sem ferir os recém-criados. Revisitar a sala deve manter os destroços.
7. Quebre uma urna, caminhe sobre a poça e saia dela. A lentidão deve corresponder à elipse visível e não acumular. Esquive para atravessar; criaturas terrestres também devem ser afetadas. Espere até 14 s para a água desaparecer; revisitar não deve restaurar a urna quebrada.
8. Teste abismos nas salas indicadas do segundo andar em diante: esquiva atravessa, aterrissar no buraco perde uma vida e volta ao chão seguro. Inimigos flutuantes cruzam; terrestres evitam.

## Chefes, progressão e loja

1. Confira os sete arquétipos, barra inferior exclusiva e preenchimento proporcional. Carrasco e Oráculo devem alternar os três padrões e intensificar quando feridos; os avisos de chão duram 1,1 s e são cancelados ao purificar a sala. Repetir a semente e o andar reproduz o sorteio.
2. Verifique os cinco temas, pisos, sombras, faces/coroamentos de paredes, cobertura e neblina baixa. A ordenação deve permitir passar atrás dos objetos; paredes próximas ficam translúcidas quando cobrem o jogador. Projéteis permanecem legíveis. Esta apresentação usa camadas 2D, sem malhas 3D.
3. No mercado, confira uma ou duas vitrines com as mesmas armas do catálogo. Reabrir ou comprar não muda a outra oferta. A próxima visita sorteia entre armas não adquiridas; após obter todas, estoque vazio deve ser tratado sem erro. Não é possível comprar produto fora do estoque.
4. Compre Balas Gigantes até o limite de 160% e Botas até 125%; mudanças devem persistir entre andares e sumir em uma incursão nova. Confira cada relíquia de alma, seu benefício, preço e rejeição de compra repetida. Compra recusada não consome moedas.
5. Caminhe pelo balcão/vitrines. Abrir conversa longe deve falhar; perto, E/X/clique abre e só então bloqueia movimento. Esc/Círculo fecha antes de pausar. Descer exige conversa fechada e interação no portal.
6. Confira 7/9/13/27 salas nos andares 1/2/4/11, minibosses e aumento moderado de vida. Nova incursão restaura arma inicial, vida, passivos e mapa; morte encerra a atual.

## Controle e interface

1. Conecte DualShock 4 por USB e depois Bluetooth no PC, verificando se o Input System reconhece o dispositivo. Teste desconectar/reconectar e alternar mouse/teclado sem mira presa ou disparo involuntário. O teste sintético não comprova compatibilidade do hardware/driver.
2. Use somente controle: escolha personagem, inicie, mova/mire com analógicos, dispare R2, esquive Círculo/L2, use Quadrado, troque armas L1/R1, abra mapa Share/touchpad e pause Options. A mira deve conservar a última direção após soltar o analógico direito.
3. Navegue loja, abas L1/R1, páginas Triângulo, compra X e fechamento Círculo. Segurar X ao abrir não pode comprar; só uma nova pressão aceita a ação. Teste pausa, som e as três escolhas da tela de morte sem mouse.
4. Confira Tab/roda para todo o inventário e atalhos 1–0. Em 1280×720 e 860×500, teste foco visível, glifos bitmap, preços, corações sem tarjas e menus sem sobreposição. O mapa ampliado tem fundo escuro, permanece passivo e não deve capturar disparos.
5. Após purificar, siga a seta à porta mais próxima; após chefe, ao portal. Durante combate ou conversa, ela deve desaparecer.

## Áudio, desempenho e descarte

1. Ouça os cinco temas, os trechos de loja e a intensidade de combate/chefe. Confira transições, repetição de loops, percussão e inteligibilidade dos efeitos em salas densas. O volume calculado não substitui ouvir em caixas/fones reais.
2. Teste SOM ligado/desligado, pausa, perda de foco, morte e retorno ao menu. Não deve permanecer raio sonoro ou iniciar música enquanto suspenso. Reconectar foco não deve ressuscitar efeitos já encerrados.
3. Jogue várias salas densas e alterne armas. Confira limites de 420 projéteis/140 hostis, 320 partículas, oito objetos e dez poças. Inspecione profiler para custos de geração inicial dos clipes e materiais e estabilidade após aquecer caches.
4. Reinicie várias vezes, retorne ao menu e encerre Play Mode. Confira descarte dos materiais de luz, texturas/sprites, fonte bitmap, clipes/fontes de áudio e ausência de halos ou objetos órfãos.

## Game feel e iluminação desta revisão

1. Em mouse e controle, confira o avanço de foco de 20% na mira, limitado a duas unidades, suavização e limites da sala. Ao entrar/sair, nenhuma posição antiga, trauma ou recuo deve arrastar a câmera. Atire em quatro direções: o kick deve apontar para o lado oposto. Compare tiros comuns, explosões e golpes de chefe; pausa deve congelar o efeito.
2. Acerte todos os tipos de criatura, incluindo chefes, com tiro e raio. Confira o flash branco por 0,06 s preservando a silhueta transparente, deformação e pequeno empurrão que não cruza cobertura/abismos. Dois alvos precisam manter estados independentes. Teste a luva para evitar empurrão duplicado. Ao terminar o flash ou reutilizar um renderer, a cor deve voltar ao normal.
3. Execute o rolamento: uma bala que cruza nos primeiros 60% deve passar; nos 40% finais deve causar dano, salvo outra proteção independente. Teste contato, aviso de chão e explosão na recuperação. Afterimages devem usar a alma selecionada, pose/escala/espelhamento e sumir em 0,2 s. Salto de abismos continua funcionando.
4. Confira clarão aditivo no cano, cápsulas que saltam e pousam, limite de quantidade e limpeza ao trocar de sala. O raio deve produzir feedback na cadência do dano, sem criar cápsulas continuamente nem ruído visual ilimitado.
5. Compare pisos claros e escuros: tons noturnos no cenário, centro da vinheta transparente, bordas escuras e HUD legível. Luzes quentes em chamas/abóboras/lanternas; luzes verdes/roxas em magia, cristais e poças. Verifique variação suave por Perlin em intensidade e tamanho, congelamento ao pausar e descarte ao sair da sala. Os halos dos projéteis devem permanecer legíveis nas bordas, sem cobrir seus núcleos.
6. Observe sombras ovais nos pés de cada ator e na base de barris/urnas. Neblina deve deslizar com alfa baixo abaixo de atores e projéteis, sem ocultar a mira ou o chão perigoso.

## Padrões, cancelamento e divisão

1. Esqueleto: cone de três ossos e osso giratório com um ricochete. Slime/cogumelo: oito tiros rápidos que desaceleram e rajada de seis na morte enquanto restam adversários. Aranha: cinco tiros em V mirando à frente do movimento, sem antecipação ilimitada. Lanterna: dois fantasmas com tempo/giro limitados. Sentinela: segundo anel deslocado 22,5° e atrasado em relação ao primeiro.
2. Abóbora: espiral dupla com emissões sucessivas e bombas que se abrem em cruz. Viúva/Matriarca: alternância de leques amplos e teias que deixam lentidão temporária. Bispo: centros diferentes e velocidade comum dos tiros preservam uma cruz em movimento. Sino: anéis de 24 posições com uma brecha rotativa realmente vazia. Carrasco: leque de 180° e estilhaços. Oráculo: orbe se divide em 12 após a distância definida.
3. Antes de cada ataque, confira aviso visível e nenhum disparo antecipado. Matar um inimigo durante a preparação deve cancelar a sequência correspondente. Complete uma onda com bombas, teias ou orbes ainda ativos; não devem gerar filhos ou zonas novas depois da purificação ou na sala seguinte.
4. Teste orbes atravessando a distância de divisão no mesmo frame em que uma parede ou jogador estaria no caminho. O primeiro contato precisa vencer; filhos não podem aparecer além da cobertura nem ser atualizados duas vezes no mesmo tick. Desviar de um orbe nos i-frames não deve consumir a bala; o contato na recuperação continua válido.
5. Use o pulso do Caçador sobre bombas/orbes: os tiros cancelados não podem explodir nem dividir. Tiros fora do raio e sequências fora dele devem continuar. Confira limites de projéteis e filas em sala densa, pausa, morte, nova incursão e descarte.

## Salas, criaturas e narrativa de Halloween

- Gere os cinco formatos e teste as quatro chegadas com mouse e controle. Confira contorno do chão, paredes dos recortes, sombra e decoração, sem piso invisível ou caminhos fechados. A silhueta no mapa deve corresponder ao cenário. Dispare de uma área a outra através de uma concavidade: o recorte deve bloquear tiros e raios.
- Em salas H, atravesse a ponte com jogador e miniboss, desvie de cobertura destruída e atraia criaturas de uma área à outra. Inimigos terrestres devem contornar paredes/abismos, os flutuantes podem cruzar buracos, e somente a Sombra ignora paredes. Teste spawn e chegada nos cinco formatos, além da limpeza de colisores ao trocar de sala/loja.
- Destrua barris de madeira e confira faíscas, fusível, dano, empurrão e trauma. Destrua ácido, fique 0,75 s na área, saia, retorne e salte: dano em pulsos respeita a proteção pós-hit e não ocorre durante o salto. Ácido também atinge criaturas vulneráveis. Pilares devem rachar, deixar destroços e remover a cobertura. Revisitando, compare todas as posições/chaves dos objetos sobreviventes e ausência de colisores invisíveis.
- Espantalho fica disfarçado até aproximação/dano, então lança lâminas giratórias. Gárgula é imune nos primeiros 1,5 s e vulnerável em voo; luva e explosão não devem empurrar a estátua. Sombra é translúcida e atravessa paredes. Morcego percorre zigue-zague, deixa brasas temporárias e anuncia quatro tiros ao morrer, cancelados se a onda termina.
- Observe heróis e cinco estados dos sete bosses, incluindo preparação visível, execução, fúria abaixo de 50% e morte de 0,9 s. Confira as quatro poses de cada estado, os orbes da Médium, a fumaça do Alquimista e o arco do Ceifador, sem crescimento de objetos ao reiniciar.
- Entre na sala de cada boss e tente andar, disparar, usar poder e abrir mapa durante os três segundos: combate deve permanecer congelado. A câmera deve focar o boss, com tarjas, nome/subtítulo e provocação legíveis. E/1/2 mudam a fala sem encurtar o tempo. Pausa deve congelar o relógio e retomar sem saltar direto para a luta.
- Converse com o atendente por teclado, mouse e controle. Abrir diálogo não abre nem compra catálogo. E revela letras antes de avançar; 1/2 e direcional/X escolhem negociação, dicas e história. A mesma pressão não pode negociar e comprar. Esc/Círculo fecha sem deixar controles presos; sair da loja exige encerrar diálogo e catálogo.
- Ouça a queda do ambiente, impacto da intro e faixa exclusiva de chefe a 144 BPM entrando por volta de 0,3 s. A faixa continua no combate e só retorna ao ambiente após vitória. Vozes devem ser curtas e limitadas, respeitando SOM, pausa e perda de foco.

Os checks de fontes e helpers com tipos simulados passaram; esta seção ainda exige validação no Editor Unity e audição real.

## Horror, munição e recarga desta revisão

1. Compare os quatro heróis, todos os mobs, o miniboss e os sete bosses à prévia `REFERENCE_SPRITESHEETS.png`. Confira silhuetas, contorno de um pixel, olhos/mandíbulas e animações em repouso, movimento, ataque e estados do boss. Compare topo/lateral/base de barris, urnas, pilares e decorações em movimento pela sala; confira profundidade, sombras, colisão e visibilidade dos tiros.
2. Esvazie cada arma inicial e o revólver de apoio (Tab/roda/L1/R1). Segurar o gatilho não pode recarregar nem causar dano. Pressione R/Triângulo: o HUD mostra o tempo, a arma inclina e o som marca início/fim. A reserva fica infinita, o carregador enche apenas ao terminar.
3. Compre cada arma finita e esgote carregador/reserva. Não pode disparar vazia, durante recarga ou sem munição. Leques pagam uma unidade por salva completa e feixes uma por pulso de dano, sem cobrança nos frames de mira. O último pulso ainda deve causar dano. Teste Balas Gigantes e aumento de cadência sem cobrança extra por projétil.
4. Recarregue parcialmente, aperte R novamente, troque arma e retorne. Não deve reiniciar relógio, descartar munição ou preencher gratuitamente. Andar/esquivar e Q continuam disponíveis durante a recarga. Pausa, conversa e intro congelam seu tempo. Triângulo no catálogo muda página sem iniciar recarga.
5. Após comprar uma arma finita, mate vários mobs e procure caixas de munição (12% por morte, sem garantia em uma sala). Colete perto, sem cobertura e fora da esquiva. Cheia, a caixa deve ficar; com arma inicial equipada, deve reabastecer outra arma que precise. Recolher acrescenta até um carregador à reserva, respeitando a capacidade.
6. Deixe uma caixa, saia e retorne: a caixa deve estar na mesma posição. Colete e revisite: ela não pode reaparecer. Trocar de andar/nova incursão limpa os registros; armas e munição persistem entre andares, enquanto uma incursão nova restaura os valores iniciais.

Estas verificações visuais, de áudio e de interação precisam do Editor/PC; os helpers externos não reproduzem o motor.
