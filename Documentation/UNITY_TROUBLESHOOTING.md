# Abrir e diagnosticar o projeto

Use **Unity 6000.0.85f1**, pelo Hub, e uma pasta curta como `C:\Dev\EcosDaMeiaNoite`. A pasta selecionada deve conter `Assets`, `Packages`, `ProjectSettings` e `ABRIR_ECOS_DA_MEIA_NOITE.cmd`.

## Atualização por ZIP

Feche o Editor e extraia todo o ZIP em uma pasta nova, preservando alterações próprias na cópia anterior. Sobrepor arquivos pode manter scripts e assets que foram removidos do projeto. Em um clone Git, a atualização da branch também aplica as remoções rastreadas.

Execute o atalho da pasta nova. Ele verifica a versão e os arquivos necessários, espera a importação e solicita a abertura da cena `HauntFps.unity`. Se o Editor estiver instalado fora do caminho padrão do Hub, adicione essa pasta pelo Hub e use **Ecos → Abrir protótipo FPS**, com o Play parado. Pressione Play depois da compilação. **Ecos → Abrir roguelike** abre a cena 2D preservada, `Haunt.unity`.

## Compilação bloqueada

A primeira mensagem vermelha do Console identifica o bloqueio mais útil. Registre o código do erro, o arquivo e a linha. Não edite arquivos em `Library/PackageCache` nem substitua DLLs para ocultar erros.

Um erro `CS0246` sobre um atributo de testes pode impedir o Play mesmo que o runtime compile. Os testes deste projeto usam o NUnit personalizado do Unity; mantenha a cópia atualizada dos testes e as dependências declaradas no manifest.

## Renderização

Se faltar a configuração do pipeline, pare o Play e use **Ecos → Preparar projeto**. A preparação cria `FpsPipeline` e `FpsRenderer` para a cena FPS e `HauntURP` e `HauntRenderer` para a cena 2D, dentro de `Assets/_Game/Resources`. Aguarde o fim da importação antes de abrir a cena ou voltar ao Play. **Ecos → Reparar renderização URP** também prepara os recursos e reinicializa o par usado pela versão 2D.

O FPS usa `FpsPalette` e o shader `Ecos/HauntFpsLit`; sua arma tem uma câmera de sobreposição no renderer URP 3D. Os sprites 2D usam o material `HauntSprites` e o shader `Haunt/Pixel Sprites`. Erros de shader ou recursos ausentes devem ser conferidos no Console após a resolução dos pacotes. Os checks estáticos não comprovam que os shaders foram compilados pela Unity.

## Cursor e entrada no FPS

Inicie o protótipo e clique na janela Game para capturar o mouse. **Esc / Options** pausa e libera o cursor para os menus; retome pelo botão ou pela ação de pausa. Solte o gatilho ao retomar antes de voltar a atirar. **M** alterna o áudio. Os controles completos estão em [PROTOTIPO_FPS.md](PROTOTIPO_FPS.md).

O módulo `com.unity.modules.physics` 1.0.0 deve estar no manifest desta revisão. Erros de referência a `CharacterController`, `Collider` ou `Physics` pedem conferir essa dependência e a resolução do UPM. O suporte de Gamepad usa Input System; reconhecimento de um controle no sistema e teste no hardware continuam necessários.

## Caminhos e cache

O atalho recusa caminhos de projeto maiores que 60 caracteres como prevenção contra falhas de extração de pacotes no Windows. Caso uma importação tenha falhado por arquivos ausentes em caminhos longos, feche o Editor, coloque o projeto no caminho curto e preserve o cache anterior renomeando `Library` para `Library_backup` antes de reabrir. O UPM reconstruirá o cache; não apague arquivos de fonte.

## Logs

O atalho grava `Logs/MeiaNoite-Editor.log`. A abertura pelo Hub normalmente grava `%LOCALAPPDATA%\Unity\Editor\Editor.log`. Use o log da tentativa mais recente, sem confundi-lo com uma abertura de outra pasta. Logs podem conter dados locais ou de autenticação; não os inclua no ZIP ou em commits.

O build Windows também exige Windows Build Support. Um aviso sobre esse módulo não identifica, por si só, um erro de compilação C# do jogo. Consulte [VALIDATION.md](VALIDATION.md) para distinguir importação, testes, Play Mode e build.
