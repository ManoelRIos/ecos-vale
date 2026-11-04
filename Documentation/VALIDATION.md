# Validação de Ecos da Meia-Noite

## O que foi executado nesta revisão

- 24 testes Python de versões, atalho e empacotamento.
- Análise de sintaxe dos 23 arquivos C#, referências e metadados: 29 assets e 39 GUIDs.
- 493.738 asserções compilando o núcleo C# real, incluindo 2.436 andares, estoque determinístico limitado, compras atômicas, relíquias, progressão, inventário e geometria varrida.
- Passaram 207 verificações de integração em um helper temporário que compilou todos os módulos reais com tipos Unity/Input System simulados: avisos, ondas, sete chefes, i-frames/recuperação, divisão, cancelamento, colisões, feedback, controle, objetos e descarte. A matemática e os fluxos passam por essas verificações; o comportamento do motor permanece dependente do Editor. A prévia `ARSENAL_AND_ENCOUNTERS.png` usa os pixels reais da arte procedural, sem renderização do motor.

**O Editor Unity não está instalado neste ambiente.** Esses resultados não certificam importação, compilação no Unity, shaders, Play Mode, áudio reproduzido, hardware PS4 ou executável Windows. Não há build Windows validado desta revisão. Os testes Editor foram ampliados e precisam ser executados no motor.

## Reproduzir checks sem Editor

```bash
python3 -m pip install -r Tools/requirements-validation.txt
python3 Tools/validate_project.py
python3 -m unittest discover -s Tools -p 'test_*.py'
dotnet run --project Tools/HauntChecks.csproj
```

O ambiente configurado por `bash Tools/setup-cloud.sh` executa a instalação e esses checks. Para gerar o ZIP, rastreie arquivos novos e use `python3 Tools/package_project.py`. O pacote deve conter os parciais Input, Environment, Feel, Patterns e Lighting, o componente HauntFlickerLight, o gerador BulletPatternGenerator e DodgeRules com `.meta`, além da cena, recursos, versões e demais fontes obrigatórias.

## Verificação necessária no Unity

Abra uma extração nova pelo Hub com Unity 6000.0.85f1. Aguarde os pacotes URP 17.0.4, Input System 1.11.2 e Test Framework 1.4.5. Com Play parado, use Ecos → Abrir roguelike. Execute os testes de Editor pelo Test Runner e procure erros de compilação, shader ou referências no Console.

Com Editor licenciado e `UNITY_EDITOR_PATH` definido, também é possível usar:

```bash
bash Tools/unity-check.sh import
bash Tools/unity-check.sh smoke
bash Tools/unity-check.sh tests
bash Tools/unity-check.sh build
```

O build solicitado fica em `Builds/Windows/EcosDaMeiaNoite.exe`. Teste a execução em Windows; o atalho opcional também precisa de validação nesse sistema.

## Combate, arsenal e cenário

1. Compare salas de enxame, emboscada, fogo cruzado e elite na mesma incursão. Confira combinações, formações e mobilidade diferentes; todas precisam de três ondas, uma recompensa e portas fechadas até a última. Emergência de 0,9 s não deve atacar nem receber dano. Miniboss não deve abrir portal de chefe.
2. Confira os treze mobs comuns e seus sinais de ataque, incluindo lanterna orbital, cogumelo bruto e diabrete de corda. Verifique que cobertura impede tiros, criaturas não ficam presas nos objetos novos e grupos mantêm rotas de aproximação. Limites de tiros devem preservar o disparo do jogador em salas densas.
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
