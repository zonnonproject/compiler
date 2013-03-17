setout Library.dll
setentry no
compile Library\data.znn Library\queues.znn 
setref Library.dll
setentry Main 
setout testBuffers.exe
compile Use\testBuffers.znn
testrun testbuffers.exe