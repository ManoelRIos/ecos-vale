# Pesadelos e munição

Os jogadores usam a folha original do Pilot enviada pelo usuário e três variantes no mesmo modelo. O arquivo fonte é preservado; os recortes do Pilot mantêm seus pixels RGB, sem ampliação ou redesenho. As folhas ativas usam 16 pixels por unidade e animações com quantidades variáveis de quadros. Veja [CHARACTER_MODEL.md](CHARACTER_MODEL.md) e a [prévia dos personagens](CHARACTER_MODEL.png).

## Personagens e cenário

- Piloto: aparência da folha original, com poses sem arma, de uma mão e de duas mãos. Mantém a carabina e o pulso de proteção do primeiro slot.
- Médium: variante com paleta roxa e acessórios arcanos, seguindo as mesmas poses.
- Ceifador: variante de capuz vermelho e rosto pálido no mesmo modelo.
- Alquimista: variante verde/roxa com acessórios de alquimia, conservando a estrutura de animação.
- O catálogo preserva dezessete criaturas e miniboss. Seis mobs usam folhas de estados em 48×48; os sete bosses usam 96×96 e cinco estados de quatro quadros, com 32 pixels por unidade.
- Barris, urnas, pilares, lápides e cenários temáticos têm topo, lateral sombreada e base para volume aparente. Barris/baú usam folhas de 32×32. O cenário permanece em 2D com ordenação pela posição dos pés e sombras.

Armas, relíquias, caixa de munição e decoração têm desenhos procedurais com resolução dobrada e materiais mais definidos. As regras de munição e os padrões dos projéteis continuam descritos abaixo. Veja [SPRITESHEETS.md](SPRITESHEETS.md) para os dois conjuntos de arte.

## Regras de munição

**R** ou **Triângulo** inicia uma recarga manual. O catálogo mantém Triângulo para páginas. Não há recarga automática ao esvaziar o carregador. O HUD mostra o carregador, a reserva, o tempo restante ou o aviso para recarregar/trocar a arma.

A arma inicial da alma e o revólver têm **reserva infinita** e carregador finito. O revólver de apoio pode ser selecionado por Tab, roda ou L1/R1. Armas compradas chegam com o carregador cheio e três carregadores na reserva; comportam até quatro carregadores na reserva. Cada arma guarda sua própria munição ao ser desequipada.

Uma unidade paga o conjunto completo de projéteis: os cinco tiros do bacamarte/guarda-chuva, três da varinha/flauta/colmeia e dois do grampeador. Feixes pagam uma unidade por pulso de dano; acompanhar a mira entre pulsos não gasta outra unidade. O último pulso carregado ainda causa dano. Os poderes de Q/Quadrado continuam usando seu próprio cooldown, sem custo de munição.

Durante a recarga, o personagem pode andar e esquivar, mas a arma não dispara. A munição só é transferida ao completar o tempo. Trocar para outra arma cancela a recarga sem descartar cartuchos; apertar R novamente não reinicia o relógio. Pausa e diálogos congelam esse tempo. Andares preservam munição; uma incursão nova reinicia o arsenal.

| Arma | Carregador | Recarga | Padrão |
| --- | ---: | ---: | --- |
| Revólver | 8 | 0,95 s | Bala simples |
| Carabina da Aurora | 12 | 0,85 s | Cápsula turquesa |
| Varinha do Véu | 6 | 1,05 s | Três pétalas em leque |
| Ceifador de Ossos | 6 | 1,25 s | Osso pesado |
| Bobina Voltaica | 30 | 1,50 s | Feixe elétrico contínuo |
| Bacamarte | 5 | 1,35 s | Cinco balas pequenas em cone |
| Repetidora | 24 | 1,25 s | Ferrão veloz |
| Canhão das Almas | 3 | 1,80 s | Orbe pesado |
| Grampeador | 10 | 1,05 s | Dois tiros paralelos |
| Banana | 6 | 1,10 s | Disparo curvo |
| Pião do Caos | 4 | 1,40 s | Disco giratório que atravessa alvos |
| Disco Espelho | 8 | 1,15 s | Disco com ricochetes |
| Lanterna de Raios | 40 | 1,80 s | Feixe contínuo violeta |
| Flauta de Ossos | 6 | 1,35 s | Três ossos em leque |
| Guarda-Chuva | 5 | 1,25 s | Cinco pétalas com ricochetes |
| Colmeia de Bolso | 9 | 1,15 s | Três ferrões de perseguição limitada |
| Luva de Boxe | 4 | 1,45 s | Impacto pesado e empurrão |

## Caixas de munição

Depois de obter uma arma de reserva finita, cada morte válida tem chance determinística de **12%** de deixar uma caixa, sem alterar o sorteio de encontros. A caixa é coletada ao se aproximar, sem saltar e sem cobertura entre o jogador e o item. Ela repõe na reserva o equivalente a um carregador, limitado à capacidade da arma.

A prioridade é a arma equipada. Se ela usa reserva infinita ou está cheia, outra arma possuída que precise recebe a munição. Se nenhuma precisar, a caixa fica disponível. Há limite de 12 caixas visíveis e 32 registros por sala. Sair e retornar ao mesmo andar mantém caixas não coletadas, sem duplicar as coletadas; mudar de andar ou iniciar outra incursão limpa os registros anteriores.

## Organização

`Core/WeaponAmmo.cs` define carregadores, reservas e durações. `RunState` controla consumo, transferência atômica, cancelamento e inventário. `Runtime/HauntGame.Ammunition.cs` trata R/Triângulo, chance de drop, sprites, coleta e persistência por sala. `HauntGame.Weapons.cs` cobra o disparo aceito antes de emitir a salva, reservando espaço para todos os seus projéteis. Sons curtos de início/fim, gatilho vazio e coleta são sintetizados no módulo de áudio. `HauntPixelArt.Equipment.cs` detalha as armas, os itens e o sprite `ammo-cache` em 32 pixels por unidade, mantendo seus tamanhos no mundo.

As verificações do núcleo e os helpers externos conferem lógica, conservação da munição e contratos de código. A aparência em movimento, mixagem de áudio, importação, testes Editor e controle físico exigem Unity; veja [VALIDATION.md](VALIDATION.md).
