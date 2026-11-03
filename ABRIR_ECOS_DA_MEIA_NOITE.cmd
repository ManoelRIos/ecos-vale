@echo off
setlocal
cd /d "%~dp0"
title Ecos da Meia-Noite - verificar pasta e abrir

echo ECOS DA MEIA-NOITE
echo Pasta deste projeto: "%CD%"
echo.
set "ECOS_PROJECT_DIR=%CD%"
rem Limite preventivo para os caminhos internos dos pacotes no Windows.
if not "%ECOS_PROJECT_DIR:~60,1%"=="" goto PATH_TOO_LONG
if not exist "Packages\manifest.json" goto MISSING_PROJECT
if not exist "ProjectSettings\ProjectVersion.txt" goto MISSING_PROJECT
if not exist "Assets\_Game\Scenes\Haunt.unity" goto MISSING_ROGUELIKE
if not exist "Assets\_Game\Haunt\Runtime\HauntGame.cs" goto MISSING_ROGUELIKE
if not exist "Assets\_Game\Haunt\Editor\HauntProject.cs" goto MISSING_ROGUELIKE
findstr /B /L /C:"m_EditorVersion: 6000.0.85f1" "ProjectSettings\ProjectVersion.txt" >nul
if errorlevel 1 goto WRONG_VERSION
echo [OK] Roguelike encontrado. Projeto preparado para Unity 6000.0.85f1.
if /I "%~1"=="/check" exit /b 0

set "ECOS_EDITOR=%ProgramFiles%\Unity\Hub\Editor\6000.0.85f1\Editor\Unity.exe"
if not exist "%ECOS_EDITOR%" goto MISSING_EDITOR
if not exist "Logs" mkdir "Logs"
echo Abrindo o roguelike desta pasta no Unity. Aguarde a resolucao e a compilacao.
echo Log desta abertura: "%CD%\Logs\MeiaNoite-Editor.log"
start "" "%ECOS_EDITOR%" -projectPath "%CD%" -executeMethod Ecos.Haunt.Editor.HauntProject.OpenFromCommandLine -logFile "%CD%\Logs\MeiaNoite-Editor.log"
if errorlevel 1 goto START_FAILED
exit /b 0

:PATH_TOO_LONG
echo [ERRO] O caminho desta pasta tem mais de 60 caracteres.
echo O Windows pode falhar ao extrair DLLs e shaders dos pacotes em caminhos longos.
echo Feche o Unity e coloque o projeto em C:\Dev\EcosDaMeiaNoite.
echo Se este projeto ja falhou na importacao, renomeie Library para Library_backup
echo antes de reabrir, preservando o cache anterior em vez de apaga-lo.
goto FAILED

:MISSING_PROJECT
echo [ERRO] Pasta incompleta. Extraia TODO o ZIP antes de executar este arquivo.
goto FAILED

:MISSING_ROGUELIKE
echo [ERRO] Esta pasta nao contem a versao completa de Ecos da Meia-Noite.
echo Baixe o ZIP atualizado e extraia tudo em uma pasta nova.
echo Preserve sua copia antiga e alteracoes proprias.
goto FAILED

:WRONG_VERSION
echo [ERRO] Esta copia nao corresponde ao projeto Unity 6000.0.85f1 atualizado.
goto FAILED

:MISSING_EDITOR
echo [ERRO] Unity 6000.0.85f1 nao foi encontrado no caminho padrao do Hub.
echo Instale essa versao no Hub. Se ela estiver em outra pasta, use Add project from disk
echo e selecione a pasta mostrada acima usando o Editor 6000.0.85f1.
goto FAILED

:START_FAILED
echo [ERRO] Nao foi possivel iniciar o Editor. Confira a instalacao no Unity Hub.
goto FAILED

:FAILED
echo.
echo A verificacao nao apaga caches nem modifica os arquivos do projeto.
pause
exit /b 1
