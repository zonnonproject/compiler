if exist test.txt del test.txt
call "C:\Program Files (x86)\Mono-2.10.9\bin\setmonopath.bat" 
IF exist ..\..\temp ( del /Q ..\..\temp\*.* ) ELSE ( mkdir ..\..\temp)
mono bin\ROTORrelease\Tester.exe /testdir:..\..\testsuite /binarydir:..\..\temp
copy ..\..\temp\Log*.xml Log*.xml
del /Q ..\..\temp\*.*
if exist test.txt del test.txt
if exist 1.txt del 1.txt