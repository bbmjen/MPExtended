@ECHO OFF

REM Requires 64-bit Windows

pushd %1
cd 
"C:\Program Files (x86)\Microsoft\ILMerge\ILMerge.exe" /target:library /v2 /out:MPExtended.PowerScheduler.dll MPExtended.Applications.PowerSchedulerPlugin.dll MPExtended.Services.MetaService.Interfaces.dll MPExtended.Services.Common.Interfaces.dll
popd