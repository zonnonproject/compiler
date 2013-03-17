if exist test.txt del test.txt
IF exist ..\..\temp ( del /Q ..\..\temp\*.* ) ELSE ( mkdir ..\..\temp)
bin\2008Release\Tester.exe /testdir:..\..\testsuite /binarydir:..\..\temp
copy ..\..\temp\Log*.xml Log*.xml
del /Q ..\..\temp\*.*
if exist test.txt del test.txt
if exist 1.txt del 1.txt