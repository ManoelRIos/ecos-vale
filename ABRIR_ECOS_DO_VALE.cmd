@echo off
rem Compatibilidade com o atalho anterior; abre o roguelike atual.
call "%~dp0ABRIR_ECOS_DA_MEIA_NOITE.cmd" %*
exit /b %errorlevel%
