setout Library2.dll
setentry no
compile Library\Library.znn
setref Library2.dll
setout UserLibrary2.dll
compile UserLibrary\UserLibrary.znn
setref Library2.dll UserLibrary2.dll
setentry Main
setout Use.exe
compile Use\Use.znn
testrun Use.exe

