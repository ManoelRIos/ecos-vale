@echo off
setlocal
cd /d "%~dp0"
title Ecos do Vale - verificar pasta e abrir

echo ECOS DO VALE
echo Pasta deste projeto: "%CD%"
echo.
if not exist "Packages\manifest.json" goto MISSING_PROJECT
if not exist "ProjectSettings\ProjectVersion.txt" goto MISSING_PROJECT
if not exist "Assets\_Game\Scenes\Valley.unity" goto MISSING_PROJECT
findstr /L /C:"com.unity.modules.physics2d" "Packages\manifest.json" >nul
if errorlevel 1 goto MISSING_PHYSICS2D
findstr /B /L /C:"m_EditorVersion: 6000.0.85f1" "ProjectSettings\ProjectVersion.txt" >nul
if errorlevel 1 goto WRONG_VERSION
echo [OK] Physics 2D declarado no manifest desta pasta.
echo [OK] Projeto preparado para Unity 6000.0.85f1.
if /I "%~1"=="/check" exit /b 0

set "ECOS_EDITOR=%ProgramFiles%\Unity\Hub\Editor\6000.0.85f1\Editor\Unity.exe"
if not exist "%ECOS_EDITOR%" goto MISSING_EDITOR
if not exist "Logs" mkdir "Logs"
echo Abrindo esta pasta no Unity. Aguarde a resolucao e a compilacao.
echo Log desta abertura: "%CD%\Logs\Ecos-Editor.log"
start "" "%ECOS_EDITOR%" -projectPath "%CD%" -logFile "%CD%\Logs\Ecos-Editor.log"
if errorlevel 1 goto START_FAILED
exit /b 0

:MISSING_PROJECT
echo [ERRO] Pasta incompleta. Extraia TODO o ZIP antes de executar este arquivo.
goto FAILED

:MISSING_PHYSICS2D
echo [ERRO] Esta copia ainda nao declara Physics 2D.
echo Use a pasta nova do ZIP atualizado, ou habilite Physics 2D no Package Manager.
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
echo Nenhum arquivo do seu projeto foi alterado por esta verificacao.
pause
exit /b 1
