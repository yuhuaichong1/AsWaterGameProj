@echo off
chcp 65001 >nul
setlocal EnableExtensions

rem 切换到 bat 所在目录（CreateSln），工程根目录为其上一级
cd /d "%~dp0"

echo ========================================
echo   Unity 解决方案一键生成工具
echo ========================================
echo.

if not exist "..\Assets\" (
    echo [错误] 未找到 ..\Assets\
    echo 请将 CreateSln 文件夹放在 Unity 工程根目录，与 Assets 同级。
    echo.
    pause
    exit /b 1
)

if not exist "..\ProjectSettings\" (
    echo [错误] 未找到 ..\ProjectSettings\
    echo 请确认 CreateSln 位于 Unity 工程根目录。
    echo.
    pause
    exit /b 1
)

set "PY="
where python >nul 2>&1 && set "PY=python"
if not defined PY where py >nul 2>&1 && set "PY=py -3"
if not defined PY where python3 >nul 2>&1 && set "PY=python3"

if not defined PY (
    echo [错误] 未找到 Python 3。
    echo 请安装 Python 3 并勾选 "Add to PATH"，然后重试。
    echo 下载: https://www.python.org/downloads/
    echo.
    pause
    exit /b 1
)

echo 使用 Python: %PY%
echo 工程目录: %~dp0..
echo.

%PY% "%~dp0generate_unity_solution.py"
set "ERR=%ERRORLEVEL%"

echo.
if "%ERR%"=="0" (
    echo [成功] 解决方案已生成在工程根目录。
) else (
    echo [失败] 生成过程中出现错误，请查看上方提示。
)
echo.
pause
exit /b %ERR%
