@echo off
echo VcareTodoを再起動します...
taskkill /F /IM VcareTodo.exe 2>nul
timeout /t 1 /nobreak >nul
dotnet run
