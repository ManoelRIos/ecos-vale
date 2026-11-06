# Ecos da Meia-Noite

Campanha de ação com alternância entre **visão 2D e primeira pessoa na mesma arena 3D**, estética macabra, texturas pixeladas e geração em C#, para **Unity 6.0 LTS 6000.0.85f1**. Os quatro personagens, os cinco temas e os sete chefes fazem parte desta expansão híbrida. Música e efeitos são sintetizados localmente; o jogo permanece solo e não exige serviços externos.

[**Projeto no GitHub**](https://github.com/ManoelRIos/ecos-vale/tree/codex/ecos-do-vale-vertical-slice) · [**Baixar projeto em ZIP**](https://github.com/ManoelRIos/ecos-vale/raw/refs/heads/codex/ecos-do-vale-vertical-slice/Distribution/EcosDaMeiaNoite.zip)

[**PNG com todos os personagens para referência 3D**](Documentation/TODOS_OS_PERSONAGENS.png): 30 entidades com identificação e poses atuais. Veja [orientações de modelagem e exportação](Documentation/REFERENCIA_3D.md).

## Abrir e jogar

1. Instale **Unity 6000.0.85f1** pelo Unity Hub; acrescente Windows Build Support para gerar um executável Windows.
2. Extraia todo o ZIP em uma **pasta nova e curta**, por exemplo `C:\Dev\EcosDaMeiaNoite`. Preserve suas alterações da cópia anterior. A raiz deve conter `Assets`, `Packages` e `ProjectSettings`.
3. No Hub, use **Add / Add project from disk** e selecione essa raiz. Aguarde os pacotes e a compilação. Com Play parado, use **Ecos → Abrir protótipo FPS** para abrir `Assets/_Game/Scenes/HauntFps.unity`.
4. Pressione **Play**, escolha um dos quatro personagens e inicie a campanha. **Tab / R1** alterna a perspectiva a qualquer momento. Explore as salas do andar, limpe os combates, compre no mercador e vença o chefe; **E / Quadrado** no portal aberto leva à próxima fase, até a quinta.
5. O menu também oferece **treino de chefe**, com seleção dos sete chefes e dos cinco temas. **Esc / Options** pausa e libera o cursor.

O atalho opcional `ABRIR_ECOS_DA_MEIA_NOITE.cmd` abre essa mesma cena. `/check` confere a pasta e o log fica em `Logs/MeiaNoite-Editor.log`. Abrir pelo Hub dispensa o atalho. O atalho não foi executado no Windows neste ambiente.

Para atualizar por ZIP, use uma extração nova: sobrepor pastas não remove arquivos antigos. Um clone Git atualizado também aplica as remoções rastreadas. Ainda não há um executável Windows validado desta revisão.

## Campanha híbrida 2D / FPS

**Masmorra de salas, no molde dos dungeon crawlers top-down.** Cada andar é uma árvore de 7 a 15 salas ligadas por corredores: entrada, salas de combate de tamanhos variados, um **mercador**, uma **câmara do tesouro** e a **sala do chefe**, sempre a mais distante. Ao entrar numa sala de combate as grades sobem e trancam as portas; duas ondas (três nas salas com mini-chefe) precisam ser vencidas para reabri-las. Criaturas soltam **almas** (a moeda) que voam até você, e salas limpas podem deixar corações. O mercador vende três itens do andar (**E** para comprar), o baú dá um item grátis e uma chuva de almas, e a sala do chefe abre com uma apresentação: barras de cinema, câmera indo até o guardião, cartão com o nome e controle travado por 3 s. O minimapa mostra as salas visitadas e vizinhas, com marcas para chefe, loja e tesouro.

**A troca de perspectiva preserva o encontro.** Vida, munição, recarga, poderes, inimigos e projéteis continuam no mesmo estado, e as duas visões consultam as mesmas colisões. A visão 2D usa uma câmera ortográfica inclinada a 58° em visual 2.5D: paredes de tijolo com base e cornija, topo escuro com borda clara, piso de ladrilhos com bisel e rachaduras, tochas e braseiros com luz tremulante, sombras para baixo e à direita, tapetes, estantes, estandartes, caixas, mesas, vasos e pilares. A parede do fundo e as laterais ficam em pé; só as da frente afundam. Balas inimigas são orbes rosa contornados e as do jogador usam o projétil de cada alma, sempre desenhados por cima de tudo.

**As criaturas são 3D de verdade na primeira pessoa.** Cada sprite é esculpido num corpo com volume: a silhueta é inflada (mais grossa no centro, fina nas bordas), a frente é pintada com a pose de frente e as costas com a pose de costas. Assim cada criatura mantém a forma, a paleta e os detalhes do desenho 2D, recebe a luz das tochas, projeta sombra e é animada quadro a quadro. As malhas são geradas no primeiro uso. `HauntFpsHybridArt.FirstPersonStyle` também oferece os sprites planos (`Sprite`) ou os modelos voxel antigos (`Voxel`).

**Pixel art de verdade.** A imagem é renderizada perto de 540 linhas (a altura da tela dividida por um número inteiro: 540 em 1080p, 480 em 1440p) e ampliada sem suavização. HUD e textos continuam em resolução cheia. Na visão 2D o enquadramento mostra cerca de 17 ladrilhos de altura, um pixel renderizado equivale a um pixel das criaturas (dois para os heróis e o cenário) e câmera e sprites andam pixel a pixel. `HauntFpsGame.PixelArtRendering = false` volta à resolução cheia.

**A transição** é um dolly zoom: a câmera sai dos olhos do personagem, recua enquanto a lente se fecha e chega à vista ortográfica sem salto. Os sprites trocam de lugar com o corpo 3D do herói no meio do movimento, as paredes da frente afundam junto com a câmera e o mundo desacelera para 35% durante os 0,4 s. Nesse intervalo os disparos ficam bloqueados e não há invencibilidade adicional.

Na primeira pessoa, o mouse e o analógico direito controlam o olhar. Na visão 2D, WASD segue os eixos da sala; o mouse aponta a posição da mira e o analógico direito mantém a direção do tiro. Mãos, armas, clarões, cápsulas e sprites acompanham o personagem selecionado.

| Personagem | Arma inicial | Carregador | Poder — Q / L1 |
| --- | --- | --- | --- |
| Caçador | Carabina da Aurora | 12 | Pulso de dano próximo |
| Médium | Varinha do Véu | 6 | Lamento que atinge criaturas e desfaz projéteis hostis |
| Ceifador | Ceifador de Ossos | 6 | Colheita de curto alcance |
| Alquimista | Bobina Voltaica | 30 | Elixir que recupera vida |

As armas iniciais têm reserva infinita e exigem **recarga manual com R / Triângulo**. A carabina dispara em linha, a varinha abre um leque, os ossos podem atravessar criaturas e a bobina pode encadear um alvo próximo. A esquiva protege nos primeiros 60% de sua duração e termina com recuperação vulnerável.

A campanha atravessa **Jardins da Colheita, Galeria dos Véus, Catedral das Cinzas, Ninho da Peste e Abismo dos Sinos**. Cada tema tem paleta de tijolo, piso, madeira e tecido, cor das tochas, neblina e luz próprias; cada sala de combate sorteia um entre cinco arranjos de cobertura. Os encontros distribuem **17 tipos de criatura**, variando o elenco de sala para sala, e o andar termina com um chefe sorteado do grupo daquele tema.

Os sete chefes possuem modelos 3D e apresentação em sprites na visão 2D: **Rei das Abóboras, Viúva do Véu, Bispo das Cinzas, Matriarca da Peste, Sino do Abismo, Carrasco de Ferro e Oráculo do Brejo**. Têm introdução, preparação de ataque, padrões próprios, fúria abaixo de metade da vida e morte. Barris explosivos, cobertura, efeitos, som e dano usam a mesma simulação nas duas perspectivas. O portal só abre depois do chefe; avançar conserva a progressão da incursão.

| Ação | Teclado / mouse | Controle PS4 reconhecido |
| --- | --- | --- |
| Mover / olhar ou mirar | WASD ou setas / mouse | Analógicos esquerdo / direito |
| Alternar 2D / primeira pessoa | Tab | R1 |
| Atirar | Botão esquerdo | R2 |
| Recarregar | R | Triângulo |
| Usar poder | Q | L1 |
| Esquivar | Alt esquerdo, Ctrl esquerdo ou botão direito | Círculo |
| Saltar | Espaço | X |
| Comprar / abrir baú / usar portal | E | Quadrado |
| Pausar / liberar cursor | Esc | Options |
| Confirmar início / reiniciar | Enter | X |
| Ligar / desligar som | M ou botão do menu | Botão do menu |

Os menus têm cursor livre. Depois de retomar ou alternar a perspectiva, solte o gatilho antes de voltar a disparar. O Input System faz o mapeamento; o funcionamento com o DualShock 4 físico e seus drivers ainda precisa ser testado no PC.

O catálogo completo de 17 armas do roguelike anterior ainda não foi levado à campanha híbrida, que usa a arma de cada alma. **Ecos → Abrir roguelike** e o botão do menu dão acesso à versão 2D preservada. O build começa em `HauntFps` e inclui `Haunt` como segunda cena.

Veja [PROTOTIPO_FPS.md](Documentation/PROTOTIPO_FPS.md) para módulos e roteiro de teste. A masmorra foi compilada e jogada no Editor Unity 6000.0.85f1 (salas, portas, ondas, loja, chefe com apresentação, as duas visões e as criaturas esculpidas). Build Windows, áudio e controle físico ainda precisam de validação.

## Roguelike 2D preservado

As seções seguintes descrevem `Assets/_Game/Scenes/Haunt.unity`. Nessa cena, Play abre a seleção das quatro almas. Uma semente numérica repete a planta, os encontros, o chefe e as ofertas daquela versão; vazia gera outra incursão.

### Personagens originais

As quatro almas são desenhos originais gerados por [`Tools/generate_original_heroes.py`](Tools/generate_original_heroes.py): proporções chibi (cabeça grande, corpo curto), contorno escuro e luz do alto à esquerda. O **Caçador** usa chapéu de aba larga, casaco turquesa e cachecol vermelho; a **Médium**, capuz violeta, trança prateada e amuleto dourado; o **Ceifador**, capuz escuro e máscara de osso; a **Alquimista**, óculos de latão, cabelo laranja e jaleco com avental azul. Cada uma tem frente, diagonal, perfil, costas, caminhada, rolamento em bola, queda e as demais poses do catálogo, sem nenhum pixel de terceiros. O arquivo `Assets/_Game/ArtSources/PilotOriginal.png` e o importador antigo ficaram apenas como histórico e não são usados pelo jogo; remova-os antes de publicar o projeto.

O catálogo distingue poses sem arma, com uma mão e com duas mãos, além do traje alternativo. Cada animação usa sua quantidade de quadros e seu tempo.

Os personagens ativos ficam em [CharacterSheets](Assets/_Game/Resources/CharacterSheets), com filtro Point e **16 pixels por unidade**, preservando o desenho nativo. A célula interna de 48×48 inclui espaço transparente e não representa um redimensionamento da arte. A arma equipada continua em um renderer separado; tiros, recarga e poderes usam a pose original adequada e os efeitos do combate.

As poses de mesa, baú, voo, fantasma e reviver estão disponíveis na biblioteca; sua presença na folha não adiciona essas mecânicas nem multiplayer ao jogo.

### Mobs, bosses, objetos e munição

Espantalho, Gárgula, Sombra, Morcego de Fogo, Esqueleto e Slime mantêm folhas de combate próprias. O Carrasco usa armadura azul e machado; o Mestre Abóbora tem capa roxa, foice e morcegos. Os demais cinco bosses mantêm o mesmo formato de animação.

As **24 folhas procedurais de referência, com 1.700 células**, continuam em [ReferenceSheets](Assets/_Game/Resources/ReferenceSheets): seis mobs em **48×48**, bosses em **96×96**, barris/baú em **32×32** e os protótipos de personagens usados como fallback. Elas trabalham em **32 pixels por unidade**. As 17 armas, relíquias e objetos de cenário também recebem detalhes procedurais de metal, madeira, empunhaduras, mecanismos e partes luminosas. Veja [SPRITESHEETS.md](Documentation/SPRITESHEETS.md) e a [prévia dos equipamentos](Documentation/EQUIPMENT_DETAIL.png).

**R recarrega a arma; no controle, Triângulo.** Cada arma mantém seu próprio carregador e reserva. A arma inicial de cada alma e o revólver têm reserva infinita, mas exigem recarga manual. Armas compradas têm reserva limitada e podem esgotar. Uma carga paga o disparo completo dos leques; feixes consomem por pulso de dano. A recarga bloqueia tiros, movimenta a arma e tem efeitos sonoros próprios. Trocar de arma cancela a recarga sem perder munição; pausa e conversas congelam seu tempo.

Após adquirir uma arma com reserva finita, inimigos têm **12% de chance** de deixar uma caixa de munição. Aproximar-se repõe uma quantidade equivalente a um carregador na reserva da arma equipada, ou de outra arma possuída que precise. Se todas estiverem cheias, a caixa fica no chão. Caixas não coletadas persistem nas revisitas ao mesmo andar. Veja os valores e padrões em [MUNICAO_E_TERROR.md](Documentation/MUNICAO_E_TERROR.md).

A [prévia das folhas procedurais](Documentation/REFERENCE_SPRITESHEETS.png) inclui os protótipos anteriores dos jogadores. Os personagens ativos aparecem na prévia do modelo original acima. As duas montagens usam os pixels dos arquivos, sem renderização no Unity.

### Salas irregulares e expansão de Halloween

As salas usam cinco formatos: **L, T, H, octógono e alcovas**, com recortes, contornos elevados e abismos internos. Quatro acessos cardinais e um núcleo seguro preservam as portas; os formatos L/T recebem corredores de acesso quando necessário. A sala H liga duas áreas pela passagem central. O minimapa agora mostra a silhueta real de cada sala. A colisão dos recortes é compartilhada por movimento, tiros e raios, e a navegação dos inimigos contorna paredes e cobertura.

**Barris explosivos** têm madeira abaulada, cintas, caveira e pavio com faíscas. A explosão anunciada causa dano, empurrão e trauma. **Barris de ácido** têm metal amassado, transbordamento e gotas verdes; deixam uma poça luminosa que causa dano em pulsos por oito segundos. **Pilares destrutíveis** recebem rachaduras antes de perder a colisão. Urnas de água continuam disponíveis. Lápides, grades e velas ossuárias decoram as bordas sem fechar passagens.

Os quatro heróis usam o catálogo de poses do modelo original e mantêm seus poderes e equipamentos. Quatro criaturas ampliam o catálogo para **17 mobs comuns**: Espantalho Amaldiçoado, Gárgula de Pedra, Sombra Perseguidora e Morcego de Fogo. A gárgula alterna 1,5 s de pedra imune e 1,5 s de voo; a sombra cruza paredes; o morcego deixa brasas e dispara quatro tiros ao morrer enquanto o combate continua.

Os sete bosses possuem **Idle, Telegraph, Attack, Enrage e Death**, com quatro poses em cada estado. A fúria aparece abaixo de 50% de vida e aumenta a mobilidade/cadência. Entrar em uma sala de chefe inicia uma **intro obrigatória de três segundos**, com pan de câmera, tarjas, título, subtítulo e provocação. É possível responder ao desafio dentro dessa janela com E ou 1/2; a resposta não encurta o tempo da apresentação.

O atendente oferece diálogo com retrato, letras graduais e voz procedural. E avança/completa o texto; 1/2 escolhem respostas; **NEGOCIAR** abre o catálogo. Há dicas e história além do comércio. Controle usa direcional e X, Círculo fecha. A trilha ambiente desaparece ao entrar no chefe; uma faixa exclusiva a **144 BPM** começa na intro e continua até a vitória, quando retorna o tema do andar.

A prévia a seguir é produzida pelos desenhos C# reais, **sem renderização no Editor Unity**. Veja [SALAS_E_HISTORIAS.md](Documentation/SALAS_E_HISTORIAS.md) para módulos e validação.

A prévia anterior permanece em [IRREGULAR_HALLOWEEN.png](Documentation/IRREGULAR_HALLOWEEN.png); os desenhos atuais estão acima.

### Câmera, impacto e evasão

A câmera antecipa a mira em **20%**, com limite de duas unidades, suavização e recuo oposto ao disparo. O screen shake usa **trauma e Perlin Noise**, com reações fortes a explosões e golpes de chefes. Acertos geram flash branco de **0,06 s**, deformação breve e recuo com colisão; chefes resistem mais ao empurrão. O cano recebe clarão luminoso e ejeta cápsulas que pousam e ficam no chão da sala, com limite de quantidade.

O rolamento concede proteção nos **primeiros 60%** e tem **40% de recuperação vulnerável**. As cópias espectrais usam a pose do personagem escolhido e desaparecem em **0,2 s**. O salto sobre abismos continua usando a duração completa da esquiva.

Os inimigos agora usam um **gerador modular de padrões**: cones, anéis que desaceleram, mira preditiva, projéteis guiados limitados, ondas intercaladas e ataques de morte. Chefes têm espirais, bombas em cruz, teias de lentidão, cruzes que viajam em formação, ondas com brecha rotativa, leques de 180° e orbes que se dividem em 12 tiros. As emissões são anunciadas antes de ocorrerem. Veja o catálogo e os módulos em [GAME_FEEL.md](Documentation/GAME_FEEL.md).

### Combate com mais variedade

Cada sala de combate mantém **três ondas**, mas recebe um perfil próprio: **enxame**, **emboscada**, **fogo cruzado** ou **elite**. Formações e combinações variam, os inimigos são mais ativos e a vida cresce de modo mais moderado. As criaturas emergem de fendas durante 0,9 s; nesse preparo não atacam nem recebem dano. A recompensa e as saídas são liberadas após a última onda. Minibosses aparecem junto aos grupos em salas determinadas pela semente.

Há dezessete criaturas comuns, além do miniboss e dos chefes. O catálogo também preserva **Lanterna Errante**, que orbita e atira; **Cogumelo Bruto**, que pressiona com avanços e ataques marcados; e **Diabrete de Corda**, que alterna movimento e rajadas. A separação entre criaturas reduz os amontoados. O runtime limita os tiros hostis a 140 e reserva capacidade para os disparos do jogador.

As salas têm **barris explosivos** e **urnas de água** atingíveis por tiros de ambos os lados e por raios. Barris anunciam a explosão por 0,55 s, ferem criaturas e jogador e podem provocar reações em cadeia; cobertura bloqueia a explosão. Urnas deixam poças por até 14 s, reduzindo a velocidade de quem anda sobre elas a 60%; esquivar permite atravessar sem essa redução. Objetos quebrados permanecem quebrados ao revisitar a sala durante a incursão.

Andares começam com 7 salas, recebem duas a cada avanço e chegam ao limite de 27. O limite das arenas é 29 × 17 unidades, com área ocupada variando por formato, com câmera próxima que acompanha o personagem. Algumas salas têm abismos atravessáveis por esquiva/salto; cair custa uma vida e retorna ao último chão seguro. Uma seta aponta à saída mais próxima após a purificação e prioriza o portal após o chefe.

### Pixel art, projéteis e profundidade

Os tiros agora têm **corpo maior, contorno contrastante e núcleo claro**, com cápsulas, pétalas, ossos, discos e energia de formas distintas. Um halo curto acompanha todo projétil na mesma altura, acompanhado de clarão e rastro colorido; balas hostis também deixam um rastro curto. Os disparos hostis preservam borda rosa/vermelha. A relíquia de tamanho também aumenta o raio de colisão, mantendo a melhoria funcional.

A apresentação dessa cena reforça o aspecto **2.5D por camadas**: paredes têm faces elevadas e coroamentos, os objetos projetam sombras, atores e cobertura são ordenados pela posição dos pés e as paredes próximas ficam translúcidas quando cobririam o personagem. A posição elevada dos sprites não altera a colisão no chão. A câmera e a simulação do roguelike preservado continuam no plano 2D. Há neblina baixa animada em temas e salas específicos, sem passar sobre os projéteis.

Os cinco temas mantêm pisos e paredes próprios: Jardins da Colheita, Galeria dos Véus, Catedral das Cinzas, Ninho da Peste e Abismo dos Sinos. A atmosfera usa azul noturno e roxo profundo, com vinheta escura de centro transparente. Velas, candelabros, abóboras e lanternas têm luz quente com intensidade e raio modulados por Perlin Noise; cristais, magia e poças recebem luz verde/roxa. Sombras ovais e neblina baixa em movimento reforçam a profundidade. A interface usa letras bitmap 5 × 7; o HUD mantém corações e textos sem tarjas.

A prévia abaixo mostra os pixels gerados pelos métodos C# com tipos auxiliares fora do Editor. **Não é captura da renderização Unity.**

[Prévia anterior do arsenal e dos encontros](Documentation/ARSENAL_AND_ENCOUNTERS.png).

### Personagens, arsenal e relíquias

| Alma | Arma inicial | Poder |
| --- | --- | --- |
| Caçador | Carabina da Aurora, cápsula turquesa | Limpa tiros próximos e concede proteção breve |
| Médium do Véu | Varinha do Véu, pétalas em leque | Rajada espectral ampla |
| Ceifador Errante | Ceifador de Ossos, dano duplo | Disparo radial e proteção breve |
| Alquimista de Cinzas | Bobina Voltaica, raio contínuo | Recupera vida |

A loja sorteia **uma ou duas armas por andar**, entre as ainda não adquiridas. A seleção fica estável durante a visita, inclusive depois de comprar; cada produto aparece em uma vitrine. O catálogo também vende cura, dano, cadência, vida máxima e três relíquias:

- **Balas Gigantes:** +30 pontos percentuais de tamanho por compra, até 160%, com aumento correspondente da colisão.
- **Botas Ligeiras:** +12,5 pontos percentuais de velocidade por compra, até 125%.
- **Relíquia da alma:** Olho da Aurora ou Dente Lunar concedem +1 de dano aos tiros; Fita do Véu reduz a recarga do poder em 30%; Coração Voltaico aumenta a cura para 3 e reduz sua recarga em 15%.

As três novas armas são o **Guarda-Chuva**, com cinco pétalas e dois ricochetes; a **Colmeia de Bolso**, com três ferrões de perseguição suave; e a **Luva de Boxe**, com tiro pesado que empurra criaturas sem atravessar obstáculos. O catálogo inclui ainda Grampeador, Banana, Pião do Caos, Disco Espelho, Lanterna de Raios, Flauta de Ossos, Bacamarte, Repetidora e Canhão das Almas. Armas têm cadências e padrões próprios; cobertura e paredes bloqueiam projéteis e raios.

Caminhe até o atendente e use E/X ou clique nele a até 2,8 unidades para conversar; escolha NEGOCIAR para abrir o catálogo. Apenas a conversa bloqueia o movimento. Itens usam ícones, preço e uma característica curta, com três cartões por página. Feche a conversa e caminhe até o portal para descer. Compras recusadas não consomem almas; equipamentos e melhorias duram até o fim da incursão.

### Sete chefes

Todos têm animação, ataques anunciados e barra de vida em pixel art na parte inferior. **Carrasco de Ferro** e **Oráculo do Brejo** juntam-se aos cinco guardiões anteriores, com três padrões próprios e mudança de intensidade quando feridos. O tema sorteia um destes três candidatos a cada andar:

| Tema | Candidatos |
| --- | --- |
| Jardins da Colheita | Rei das Abóboras, Viúva do Véu, Carrasco de Ferro |
| Galeria dos Véus | Viúva do Véu, Bispo das Cinzas, Oráculo do Brejo |
| Catedral das Cinzas | Bispo das Cinzas, Sino do Abismo, Carrasco de Ferro |
| Ninho da Peste | Matriarca da Peste, Oráculo do Brejo, Rei das Abóboras |
| Abismo dos Sinos | Sino do Abismo, Carrasco de Ferro, Oráculo do Brejo |

### Teclado, mouse e controle

O suporte inicial usa o **Unity Input System**. Um **DualShock 4 de PS4** reconhecido pelo sistema pode ser usado por USB ou Bluetooth; o funcionamento no hardware/driver específico ainda precisa ser testado. Mira mantém a última direção do analógico direito. Menus, seleção de personagem, loja, pausa e reinício podem ser operados sem mouse.

| Ação | Teclado / mouse | Controle PS4 |
| --- | --- | --- |
| Mover | WASD / setas | Analógico esquerdo |
| Mirar | Mouse | Analógico direito |
| Atirar | Botão esquerdo | R2 |
| Recarregar | R | Triângulo, fora do catálogo |
| Esquivar / saltar | Espaço / botão direito | Círculo / L2 |
| Usar poder | Q | Quadrado |
| Conversar / usar portal | E | X |
| Trocar arma | Tab / roda do mouse; 1–0 para atalhos | L1 / R1 |
| Mapa | M | Share / clique no touchpad |
| Pausar | Esc | Options |
| Navegar e confirmar menu | Mouse | Direcional / analógico esquerdo e X |
| Fechar conversa | Esc / Fechar | Círculo |
| Abas / páginas da loja | Botões do catálogo | L1/R1 e Triângulo |

Atalhos: 1 arma inicial; 2 Grampeador; 3 Banana; 4 Pião; 5 Disco; 6 Lanterna; 7 Flauta; 8 Guarda-Chuva; 9 Colmeia; 0 Luva. Tab, roda e L1/R1 alcançam todo o inventário. O mesmo X que abre uma tela não compra um item.

M abre um atlas de 1060 × 580 na referência 1280 × 720, com desenhos a 95% e fundo escuro a 78%. O minimapa mantém 45% de opacidade. Ambos são passivos: o combate continua.

### Música e efeitos

Cinco trilhas ambientes e uma faixa exclusiva de chefe, todas sintetizadas e originais, usam melodia, harmonia, baixo e percussão. O mercado recebe uma camada calma; o combate e os chefes aumentam a intensidade, com transições suaves. Disparos, impactos, recargas, gatilho vazio, coleta de munição, poderes, explosões, água, compras e mortes têm efeitos curtos. O áudio tem limite de vozes, pausa com o jogo, respeita perda de foco e pode ser desligado em **SOM** no menu de pausa. Não são necessários arquivos de áudio externos.

## Desenvolvimento e limites da validação

```bash
python3 -m pip install -r Tools/requirements-validation.txt
python3 Tools/validate_project.py
python3 -m unittest discover -s Tools -p 'test_*.py'
dotnet run --project Tools/HauntChecks.csproj
```

Passaram **31 testes Python** e **2.507.249 asserções do núcleo real C#**, incluindo as regras independentes do protótipo FPS e os 2.436 andares do roguelike 2D. As verificações de sintaxe e metadados podem ser reproduzidas com os comandos acima. Helpers anteriores com tipos Unity simulados verificaram lógica do roguelike 2D; esses resultados não certificam o runtime FPS.

**O Editor Unity não está instalado neste ambiente.** Importação, compilação no Unity, aparência renderizada, reprodução sonora, build e controle PS4 físico ainda precisam ser validados no Editor/PC. O jogo continua solo e sem save entre sessões. Consulte [VALIDATION.md](Documentation/VALIDATION.md), [ARCHITECTURE.md](Documentation/ARCHITECTURE.md) e [UNITY_TROUBLESHOOTING.md](Documentation/UNITY_TROUBLESHOOTING.md).

Com Editor licenciado, execute o Test Runner ou `bash Tools/unity-check.sh import`, `fps-smoke`, `smoke`, `tests` e `build`, com `UNITY_EDITOR_PATH` definido. O build usa `Builds/Windows/EcosDaMeiaNoite.exe`, com FPS na primeira cena e o roguelike 2D na segunda. Para gerar o ZIP das fontes, inclua arquivos novos no índice Git e execute `python3 Tools/package_project.py`; o pacote tem raiz `EcosDaMeiaNoite/` e não inclui builds ou caches.
