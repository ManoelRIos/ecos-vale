# Game feel, iluminação e padrões de tiro

Esta revisão mantém a geração procedural e a física 2D. Câmera, feedback, luz e padrões foram separados em módulos para permitir ajustes sem reescrever a IA de cada criatura.

## Responsabilidades

| Módulo | Responsabilidade |
| --- | --- |
| `Core/BulletPatternGenerator.cs` | Geometria em graus, interceptação preditiva, desaceleração e giro limitado, sem Unity |
| `Core/DodgeRules.cs` | Janela de proteção dos primeiros 60% do rolamento |
| `Runtime/HauntGame.Patterns.cs` | Avisos, sequências e propriedades dos ataques de mobs e chefes |
| `Runtime/HauntGame.Weapons.cs` | Movimento, primeira colisão, ricochete, divisão e fim dos projéteis |
| `Runtime/HauntGame.Feel.cs` | Câmera, trauma, recuo, impacto, afterimages e cápsulas |
| `Runtime/HauntGame.Lighting.cs` | Integração das luzes, atmosfera e vinheta |
| `Runtime/HauntFlickerLight.cs` | Luz radial aditiva com variação suave por Perlin Noise |
| `Runtime/HauntGame.Environment.cs` | Objetos, explosões, água e zonas de lentidão das teias |

## Câmera e impacto

O foco acompanha o jogador com suavização, adiantando-se 20% em direção à mira, limitado a duas unidades. No controle, a direção retida do analógico direito define o avanço. O foco respeita os limites da arena; entradas de sala reposicionam e limpam os deslocamentos transitórios.

Trauma é acumulado e limitado, decai com o tempo e controla ruído Perlin pela sua intensidade ao quadrado. Assim, golpes pequenos ficam discretos e explosões fortes produzem reação maior. O disparo aplica um impulso curto na direção oposta à mira. Não há alteração da posição física do jogador por causa da câmera.

Todo dano em criatura aciona um flash branco de 0,06 s pelo parâmetro `_HitFlash` do shader, preservando a transparência dos pixels. O estado é individual por renderer, sem clonar material para cada acerto. Há deformação breve e pequeno recuo por movimento que respeita colisões; chefes têm resistência maior. A luva conserva o empurrão próprio sem acumular dois empurrões fortes.

O muzzle flash usa mistura aditiva no cano. As cápsulas ejetadas têm movimento curto e pousam na camada do chão; uma quantidade limitada fica na sala. Os recursos são limpos ao reconstruir o cenário e ao iniciar outra incursão.

## Evasão espectral

O rolamento mantém duração de 0,24 s. Os primeiros 0,144 s têm proteção; os 0,096 s finais são recuperação vulnerável. Projéteis podem atravessar o jogador apenas durante a janela inicial. Proteções independentes, como a concedida depois de sofrer dano ou por um poder, continuam valendo.

A altura do salto e a travessia dos abismos usam a animação inteira; a proteção contra tiros é consultada separadamente em `DodgeRules`. O disparo permanece bloqueado durante o rolamento. Afterimages usam a pose, espelhamento e rotação do personagem selecionado, em cor espectral, desaparecendo em 0,2 s.

## Atmosfera e leitura

Pisos, paredes e decoração recebem tons de azul noturno/roxo profundo. Uma vinheta com centro transparente escurece as bordas antes de desenhar o HUD. A loja conserva uma iluminação mais quente. Luzes são sprites de gradiente radial aditivos, com intensidade e raio modulados por Perlin Noise; não dependem de Light2D nem de assets externos.

Candelabros, velas, abóboras e lanternas recebem luz quente; magia, cristais e poças recebem verdes/roxos. Os projéteis mantêm corpo contrastante e halo próprio, com um pequeno rastro também nas balas hostis. As luzes ambientes não alteram os raios de colisão. A neblina se desloca lentamente com alfa baixo abaixo dos atores; sombras ovais semitransparentes marcam os pés e as bases dos objetos.

## Catálogo dos mobs

| Criatura | Padrão |
| --- | --- |
| Esqueleto | Cone fechado de três ossos, alternado com osso giratório de um ricochete |
| Slime e Cogumelo Bruto | Anel de oito tiros que desaceleram; rajada de seis na morte enquanto o combate continua |
| Aranha | Leque em V com cinco tiros e mira por interceptação limitada da velocidade do jogador |
| Lanterna Errante | Dois fantasmas com giro teleguiado limitado |
| Sentinela | Dois anéis intercalados; o segundo é deslocado em 22,5° |

## Catálogo dos chefes

| Chefe | Padrões |
| --- | --- |
| Rei das Abóboras | Espiral dupla de sementes flamejantes e bombas que se abrem em cruz |
| Viúva / Matriarca | Leques largos alternados e teias lentas que deixam zonas de lentidão |
| Bispo | Conjuntos de tiros com deslocamentos de uma cruz e a mesma velocidade, preservando a formação |
| Sino | Anéis concêntricos de 24 posições com uma brecha que gira; as posições da brecha são omitidas |
| Carrasco de Ferro | Leque pesado de 180° e estilhaços |
| Oráculo do Brejo | Orbes grandes que se dividem em 12 tiros após percorrer a distância definida |

As emissões têm aviso antes do disparo; ataques longos usam uma sequência delimitada. Projetar uma cruz requer deslocar as origens, não apenas atirar quatro balas em direções cardinais. A previsão da aranha usa a velocidade física observada do jogador e limita a antecipação; tiros guiados também têm limite de tempo e giro.

## Cancelamento, limites e testes

Filas de padrões e projéteis especiais pertencem a uma geração do combate. Concluir uma onda, sair da sala ou encerrar a incursão cancela sequências e filhos pendentes. Purificar a sala elimina tiros hostis. A limpeza impede uma bomba cancelada de explodir ou uma esfera de gerar filhos na sala seguinte. O pulso do Caçador cancela os efeitos dos tiros que alcança, preservando ataques fora do seu raio.

As teias são efeitos de chão com tempo e área definidos, compartilham o limite de dez poças e são removidas ao encerrar a onda. Água de urna mantém sua duração própria. Lentidão não acumula e a esquiva conserva a velocidade própria.

O runtime mantém os limites globais de projéteis e partículas e reutiliza seus renderers. A geometria pura e as regras de evasão são executadas pelos checks C# do núcleo. Testes no Editor cobrem integrações; helpers externos com tipos Unity simulados servem para localizar erros de lógica e contrato, sem certificar shader, aparência, áudio ou hardware. Consulte `VALIDATION.md` para o roteiro do Unity.
