@echo off
setlocal EnableDelayedExpansion

echo %date% %time%


echo Compare GPP performance with original GOLDbuild.exe
echo Tested on '%PROCESSOR_IDENTIFIER%, %NUMBER_OF_PROCESSORS% cores'
echo Using for grammar file with 1000 LR states


echo ---------------------------------------
set "tm_begin=%time: =0%"
echo Starting original GOLDbuild.exe. please wait........
echo time begin: %tm_begin%
for /F "tokens=1-4 delims=:.," %%a in ("%tm_begin%") do (
   set /A "tm_begin_val=(((%%a*60)+1%%b %% 100)*60+1%%c %% 100)*100+1%%d %% 100"
)

test\gp_original\GOLDbuild.exe test\1C8-Query.grm test\1C8-Query.egt_original.egt test\1C8-Query.log_original -verbose -details

set "tm_final=%time: =0%"
echo GOLDbuild.exe Done.
echo time final: %tm_final%
for /F "tokens=1-4 delims=:.," %%a in ("%tm_final%") do (
   set /A "tm_final_val=(((%%a*60)+1%%b %% 100)*60+1%%c %% 100)*100+1%%d %% 100"
)
set /A gpo_elapsed="%tm_final_val% - %tm_begin_val%"
echo Original GOLDbuild.exe elapsed ms: %gpo_elapsed%
echo ---------------------------------------

echo ---------------------------------------
set "tm_begin=%time: =0%"
echo Starting GPP goldbuild.exe.
echo time begin: %tm_begin%
for /F "tokens=1-4 delims=:.," %%a in ("%tm_begin%") do (
   set /A "tm_begin_val=(((%%a*60)+1%%b %% 100)*60+1%%c %% 100)*100+1%%d %% 100"
)

bin\Release\net8.0\win-x64\native\goldbuild.exe test\1C8-Query.grm test\1C8-Query.egt_gpp.egt -dat:"test\gp_original" -log:"test\1C8-Query.egt_gpp.log" -g:"2025-04-04 19:17" -w

set "tm_final=%time: =0%"
echo time final: %tm_final%
for /F "tokens=1-4 delims=:.," %%a in ("%tm_final%") do (
   set /A "tm_final_val=(((%%a*60)+1%%b %% 100)*60+1%%c %% 100)*100+1%%d %% 100"
)
set /A gpp_elapsed="%tm_final_val% - %tm_begin_val%"
echo GPP Builder elapsed ms: %gpp_elapsed%
echo ---------------------------------------


FC /B test\1C8-Query.egt_original_gui.egt test\1C8-Query.egt_gpp.egt


echo ---------------------------------------
set /A "tm_ratio"=gpo_elapsed / gpp_elapsed
echo GPP Builder faster than the original GOLDbuild in: %tm_ratio% times
echo ---------------------------------------
echo ---------------------------------------

pause